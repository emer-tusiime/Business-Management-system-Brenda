"""
Owner withdrawal business logic.

Owner withdrawals represent personal money taken from the till (school fees,
home affairs, family support, etc). They DO reduce the cash on hand but they
DO NOT reduce reported profit - they are essentially owner equity drawings.
"""
from __future__ import annotations

from dataclasses import dataclass
from datetime import date, datetime
from typing import List, Optional

from sqlalchemy import and_, func, select

from app.core.constants import WITHDRAWAL_REASONS
from app.core.utils import day_bounds, month_bounds
from app.database.models import OwnerWithdrawal
from app.database.session import session_scope


class WithdrawalError(Exception):
    """Raised for invalid withdrawal input."""


# ---------------------------------------------------------------------------
# DTOs
# ---------------------------------------------------------------------------

@dataclass
class WithdrawalSummary:
    id: int
    withdrawal_date: datetime
    reason: str
    amount: float
    taken_by: str
    notes: str
    approved_by: Optional[int]


# ---------------------------------------------------------------------------
# Mutations
# ---------------------------------------------------------------------------

def create_withdrawal(
    *,
    reason: str,
    amount: float,
    withdrawal_date: Optional[datetime] = None,
    taken_by: str = "",
    notes: str = "",
    approved_by: Optional[int] = None,
) -> int:
    if reason not in WITHDRAWAL_REASONS:
        raise WithdrawalError(f"Unknown withdrawal reason '{reason}'.")
    if amount is None or float(amount) <= 0:
        raise WithdrawalError("Amount must be greater than 0.")

    with session_scope() as session:
        row = OwnerWithdrawal(
            withdrawal_date=withdrawal_date or datetime.utcnow(),
            reason=reason,
            amount=float(amount),
            taken_by=taken_by or None,
            notes=notes or None,
            approved_by=approved_by,
        )
        session.add(row)
        session.flush()
        return row.id


def update_withdrawal(
    withdrawal_id: int,
    *,
    reason: Optional[str] = None,
    amount: Optional[float] = None,
    withdrawal_date: Optional[datetime] = None,
    taken_by: Optional[str] = None,
    notes: Optional[str] = None,
) -> None:
    with session_scope() as session:
        row = session.get(OwnerWithdrawal, withdrawal_id)
        if row is None:
            raise WithdrawalError("Withdrawal not found.")
        if reason is not None:
            if reason not in WITHDRAWAL_REASONS:
                raise WithdrawalError(f"Unknown withdrawal reason '{reason}'.")
            row.reason = reason
        if amount is not None:
            if float(amount) <= 0:
                raise WithdrawalError("Amount must be greater than 0.")
            row.amount = float(amount)
        if withdrawal_date is not None:
            row.withdrawal_date = withdrawal_date
        if taken_by is not None:
            row.taken_by = taken_by or None
        if notes is not None:
            row.notes = notes or None


def delete_withdrawal(withdrawal_id: int) -> None:
    with session_scope() as session:
        row = session.get(OwnerWithdrawal, withdrawal_id)
        if row is None:
            raise WithdrawalError("Withdrawal not found.")
        session.delete(row)


# ---------------------------------------------------------------------------
# Queries
# ---------------------------------------------------------------------------

def list_withdrawals(
    *,
    start: Optional[datetime] = None,
    end: Optional[datetime] = None,
    reason: Optional[str] = None,
    search: str = "",
    limit: int = 500,
) -> List[WithdrawalSummary]:
    with session_scope() as session:
        stmt = (
            select(OwnerWithdrawal)
            .order_by(OwnerWithdrawal.withdrawal_date.desc(), OwnerWithdrawal.id.desc())
        )
        if start:
            stmt = stmt.where(OwnerWithdrawal.withdrawal_date >= start)
        if end:
            stmt = stmt.where(OwnerWithdrawal.withdrawal_date < end)
        if reason:
            stmt = stmt.where(OwnerWithdrawal.reason == reason)
        rows = list(session.execute(stmt).scalars())

        if search:
            term = search.lower()
            rows = [
                r for r in rows
                if term in (r.taken_by or "").lower()
                or term in (r.notes or "").lower()
            ]

        rows = rows[:limit]

        return [
            WithdrawalSummary(
                id=r.id,
                withdrawal_date=r.withdrawal_date,
                reason=r.reason,
                amount=float(r.amount or 0),
                taken_by=r.taken_by or "",
                notes=r.notes or "",
                approved_by=r.approved_by,
            )
            for r in rows
        ]


def get_withdrawal(withdrawal_id: int) -> Optional[OwnerWithdrawal]:
    with session_scope() as session:
        row = session.get(OwnerWithdrawal, withdrawal_id)
        if row is None:
            return None
        session.expunge(row)
        return row


# ---------------------------------------------------------------------------
# Aggregates
# ---------------------------------------------------------------------------

def total_in_range(start: datetime, end: datetime) -> float:
    with session_scope() as session:
        value = session.execute(
            select(func.sum(OwnerWithdrawal.amount))
            .where(and_(
                OwnerWithdrawal.withdrawal_date >= start,
                OwnerWithdrawal.withdrawal_date < end,
            ))
        ).scalar()
        return float(value or 0)


def totals_by_reason(start: datetime, end: datetime) -> dict:
    result = {r: 0.0 for r in WITHDRAWAL_REASONS}
    with session_scope() as session:
        rows = session.execute(
            select(OwnerWithdrawal.reason, func.sum(OwnerWithdrawal.amount))
            .where(and_(
                OwnerWithdrawal.withdrawal_date >= start,
                OwnerWithdrawal.withdrawal_date < end,
            ))
            .group_by(OwnerWithdrawal.reason)
        ).all()
        for reason, total in rows:
            result[reason] = float(total or 0)
    return result


def today_total() -> float:
    return total_in_range(*day_bounds(date.today()))


def month_total() -> float:
    return total_in_range(*month_bounds(date.today()))
