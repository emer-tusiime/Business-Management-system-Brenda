"""Labelling jobs with installment payments."""
from __future__ import annotations
from datetime import datetime, date
from decimal import Decimal
from typing import Optional
from sqlalchemy import select, func, and_
from sqlalchemy.orm import Session, selectinload

from app.database.models import (
    LabellingJob, LabellingPayment, JobStatus, PaymentMethod, AuditLog
)
from app.core.auth import CurrentUser
from app.core.utils import quantize_money, today_start, today_end


class LabellingError(Exception):
    pass


class LabellingService:
    def __init__(self, session: Session):
        self.session = session

    # ------------- Jobs -------------
    def create_job(
        self,
        customer_name: str,
        product_description: str,
        total_amount: Decimal,
        quantity: int = 1,
        notes: str = "",
        deposit: Decimal = Decimal("0"),
        deposit_method: str = PaymentMethod.CASH.value,
    ) -> LabellingJob:
        if not customer_name.strip():
            raise LabellingError("Customer name is required")
        if not product_description.strip():
            raise LabellingError("Product description is required")
        total_amount = quantize_money(total_amount)
        if total_amount <= 0:
            raise LabellingError("Total amount must be greater than zero")
        deposit = quantize_money(deposit or Decimal("0"))
        if deposit < 0 or deposit > total_amount:
            raise LabellingError("Invalid deposit amount")

        user = CurrentUser.require()

        job = LabellingJob(
            customer_name=customer_name.strip(),
            product_description=product_description.strip(),
            quantity=int(quantity or 1),
            total_amount=total_amount,
            paid_amount=Decimal("0"),
            balance_amount=total_amount,
            status=JobStatus.PENDING.value,
            notes=notes.strip(),
            created_by=user.id,
        )
        self.session.add(job)
        self.session.flush()

        if deposit > 0:
            self._add_payment_internal(job, deposit, deposit_method, "Initial deposit", user.id)

        self._refresh_status(job)
        self.session.add(AuditLog(
            user_id=user.id, action="CREATE", entity_type="LabellingJob",
            entity_id=job.id,
            description=f"Created job for {customer_name}: {product_description} (Total ${total_amount})"
        ))
        self.session.commit()
        return job

    def update_job(
        self,
        job_id: int,
        customer_name: str,
        product_description: str,
        total_amount: Decimal,
        quantity: int,
        notes: str,
    ) -> LabellingJob:
        user = CurrentUser.require()
        job = self.session.get(LabellingJob, job_id)
        if not job:
            raise LabellingError("Job not found")
        if job.status == JobStatus.CANCELLED.value:
            raise LabellingError("Cannot edit a cancelled job")

        total_amount = quantize_money(total_amount)
        if total_amount <= 0:
            raise LabellingError("Total amount must be greater than zero")
        if total_amount < job.paid_amount:
            raise LabellingError(
                f"Total cannot be less than already paid (${job.paid_amount})"
            )

        job.customer_name = customer_name.strip()
        job.product_description = product_description.strip()
        job.quantity = int(quantity or 1)
        job.total_amount = total_amount
        job.notes = notes.strip()
        job.balance_amount = quantize_money(total_amount - job.paid_amount)
        self._refresh_status(job)
        self.session.add(AuditLog(
            user_id=user.id, action="UPDATE", entity_type="LabellingJob",
            entity_id=job.id, description=f"Updated job #{job.id}"
        ))
        self.session.commit()
        return job

    def cancel_job(self, job_id: int, reason: str = "") -> None:
        user = CurrentUser.require()
        job = self.session.get(LabellingJob, job_id)
        if not job:
            raise LabellingError("Job not found")
        if job.status == JobStatus.COMPLETED.value:
            raise LabellingError("Cannot cancel a completed job")
        job.status = JobStatus.CANCELLED.value
        self.session.add(AuditLog(
            user_id=user.id, action="CANCEL", entity_type="LabellingJob",
            entity_id=job.id, description=f"Cancelled job #{job.id}. Reason: {reason}"
        ))
        self.session.commit()

    # ------------- Payments -------------
    def add_payment(
        self,
        job_id: int,
        amount: Decimal,
        method: str,
        notes: str = "",
    ) -> LabellingPayment:
        user = CurrentUser.require()
        job = self.session.get(LabellingJob, job_id)
        if not job:
            raise LabellingError("Job not found")
        if job.status == JobStatus.CANCELLED.value:
            raise LabellingError("Cannot add payment to a cancelled job")
        if job.status == JobStatus.COMPLETED.value:
            raise LabellingError("Job is already fully paid")

        amount = quantize_money(amount)
        if amount <= 0:
            raise LabellingError("Payment amount must be greater than zero")
        if amount > job.balance_amount:
            raise LabellingError(
                f"Payment exceeds remaining balance (${job.balance_amount})"
            )

        payment = self._add_payment_internal(job, amount, method, notes, user.id)
        self._refresh_status(job)
        self.session.add(AuditLog(
            user_id=user.id, action="PAYMENT", entity_type="LabellingJob",
            entity_id=job.id,
            description=f"Payment ${amount} ({method}) for job #{job.id}"
        ))
        self.session.commit()
        return payment

    def _add_payment_internal(
        self, job: LabellingJob, amount: Decimal, method: str,
        notes: str, user_id: int
    ) -> LabellingPayment:
        payment = LabellingPayment(
            job_id=job.id,
            amount=amount,
            payment_method=method,
            notes=notes,
            created_by=user_id,
        )
        self.session.add(payment)
        job.paid_amount = quantize_money(job.paid_amount + amount)
        job.balance_amount = quantize_money(job.total_amount - job.paid_amount)
        return payment

    def _refresh_status(self, job: LabellingJob) -> None:
        if job.status == JobStatus.CANCELLED.value:
            return
        if job.balance_amount <= 0:
            job.status = JobStatus.COMPLETED.value
            if not job.completed_at:
                job.completed_at = datetime.utcnow()
        elif job.paid_amount > 0:
            job.status = JobStatus.IN_PROGRESS.value
        else:
            job.status = JobStatus.PENDING.value

    # ------------- Queries -------------
    def list_jobs(
        self,
        status: Optional[str] = None,
        search: str = "",
        limit: int = 200,
    ) -> list[LabellingJob]:
        stmt = (
            select(LabellingJob)
            .options(selectinload(LabellingJob.payments))
            .order_by(LabellingJob.created_at.desc())
            .limit(limit)
        )
        if status and status != "All":
            stmt = stmt.where(LabellingJob.status == status)
        if search:
            term = f"%{search.lower()}%"
            stmt = stmt.where(
                func.lower(LabellingJob.customer_name).like(term)
                | func.lower(LabellingJob.product_description).like(term)
            )
        return list(self.session.scalars(stmt).all())

    def get_job(self, job_id: int) -> Optional[LabellingJob]:
        stmt = (
            select(LabellingJob)
            .where(LabellingJob.id == job_id)
            .options(selectinload(LabellingJob.payments))
        )
        return self.session.scalar(stmt)

    def outstanding_total(self) -> Decimal:
        total = self.session.scalar(
            select(func.coalesce(func.sum(LabellingJob.balance_amount), 0))
            .where(LabellingJob.status.in_([
                JobStatus.PENDING.value, JobStatus.IN_PROGRESS.value
            ]))
        ) or Decimal("0")
        return quantize_money(Decimal(str(total)))

    def today_payments_total(self) -> Decimal:
        start, end = today_start(), today_end()
        total = self.session.scalar(
            select(func.coalesce(func.sum(LabellingPayment.amount), 0))
            .where(and_(
                LabellingPayment.created_at >= start,
                LabellingPayment.created_at <= end,
            ))
        ) or Decimal("0")
        return quantize_money(Decimal(str(total)))
