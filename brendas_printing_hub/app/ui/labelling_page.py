"""Labelling jobs page with installment payment tracking."""
from __future__ import annotations
from decimal import Decimal
from PySide6.QtCore import Qt
from PySide6.QtGui import QColor
from PySide6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QLabel, QPushButton, QLineEdit,
    QComboBox, QTableWidgetItem, QMessageBox, QFrame, QSpinBox,
    QDialog, QDialogButtonBox, QFormLayout, QDoubleSpinBox, QTextEdit
)

from app.core.auth import has_permission, AuthError
from app.core.utils import format_money, format_datetime
from app.database.models import JobStatus, PaymentMethod
from app.database.session import session_scope
from app.services.labelling_service import LabellingService, LabellingError
from app.ui.components.cards import KpiCard
from app.ui.components.tables import StyledTable, set_column_widths
from app.ui.components.dialogs import ConfirmDialog


PAYMENT_METHODS = [m.value for m in PaymentMethod]
JOB_STATUSES = ["All"] + [s.value for s in JobStatus]


class JobDialog(QDialog):
    """Create or edit a labelling job."""

    def __init__(self, parent=None, job=None):
        super().__init__(parent)
        self.job = job
        self.setWindowTitle("Edit Job" if job else "New Labelling Job")
        self.setMinimumWidth(480)
        self.setObjectName("DialogWindow")

        layout = QVBoxLayout(self)
        layout.setContentsMargins(24, 24, 24, 24)
        layout.setSpacing(16)

        title = QLabel("Edit Labelling Job" if job else "New Labelling Job")
        title.setObjectName("DialogTitle")
        layout.addWidget(title)

        form = QFormLayout()
        form.setSpacing(10)

        self.customer_input = QLineEdit(job.customer_name if job else "")
        form.addRow("Customer *", self.customer_input)

        self.product_input = QTextEdit(job.product_description if job else "")
        self.product_input.setMaximumHeight(70)
        form.addRow("Product / Description *", self.product_input)

        self.qty_input = QSpinBox()
        self.qty_input.setMinimum(1)
        self.qty_input.setMaximum(100_000)
        self.qty_input.setValue(job.quantity if job else 1)
        form.addRow("Quantity", self.qty_input)

        self.total_input = QDoubleSpinBox()
        self.total_input.setMaximum(10_000_000)
        self.total_input.setDecimals(2)
        self.total_input.setPrefix("$ ")
        self.total_input.setValue(float(job.total_amount) if job else 0)
        form.addRow("Total Amount *", self.total_input)

        if not job:
            self.deposit_input = QDoubleSpinBox()
            self.deposit_input.setMaximum(10_000_000)
            self.deposit_input.setDecimals(2)
            self.deposit_input.setPrefix("$ ")
            form.addRow("Deposit (optional)", self.deposit_input)

            self.deposit_method_combo = QComboBox()
            self.deposit_method_combo.addItems(PAYMENT_METHODS)
            form.addRow("Deposit Method", self.deposit_method_combo)
        else:
            self.deposit_input = None
            self.deposit_method_combo = None

        self.notes_input = QTextEdit(job.notes if job else "")
        self.notes_input.setMaximumHeight(80)
        form.addRow("Notes", self.notes_input)

        layout.addLayout(form)

        buttons = QDialogButtonBox(QDialogButtonBox.Ok | QDialogButtonBox.Cancel)
        buttons.accepted.connect(self.accept)
        buttons.rejected.connect(self.reject)
        layout.addWidget(buttons)

    def get_data(self):
        return {
            "customer_name": self.customer_input.text().strip(),
            "product_description": self.product_input.toPlainText().strip(),
            "quantity": self.qty_input.value(),
            "total_amount": Decimal(str(self.total_input.value())),
            "notes": self.notes_input.toPlainText().strip(),
            "deposit": (
                Decimal(str(self.deposit_input.value()))
                if self.deposit_input is not None else Decimal("0")
            ),
            "deposit_method": (
                self.deposit_method_combo.currentText()
                if self.deposit_method_combo is not None else PaymentMethod.CASH.value
            ),
        }


class PaymentDialog(QDialog):
    """Record an installment payment."""

    def __init__(self, parent=None, job=None):
        super().__init__(parent)
        self.job = job
        self.setWindowTitle("Add Payment")
        self.setMinimumWidth(420)
        self.setObjectName("DialogWindow")

        layout = QVBoxLayout(self)
        layout.setContentsMargins(24, 24, 24, 24)
        layout.setSpacing(16)

        title = QLabel("Record Payment")
        title.setObjectName("DialogTitle")
        layout.addWidget(title)

        info = QLabel(
            f"Customer: <b>{job.customer_name}</b><br>"
            f"Total: {format_money(job.total_amount)}<br>"
            f"Paid: {format_money(job.paid_amount)}<br>"
            f"Balance due: <b style='color:#dc2626'>{format_money(job.balance_amount)}</b>"
        )
        info.setObjectName("DialogSubtitle")
        layout.addWidget(info)

        form = QFormLayout()
        form.setSpacing(10)

        self.amount_input = QDoubleSpinBox()
        self.amount_input.setMaximum(float(job.balance_amount))
        self.amount_input.setDecimals(2)
        self.amount_input.setPrefix("$ ")
        self.amount_input.setValue(float(job.balance_amount))
        form.addRow("Payment Amount *", self.amount_input)

        self.method_combo = QComboBox()
        self.method_combo.addItems(PAYMENT_METHODS)
        form.addRow("Method", self.method_combo)

        self.notes_input = QLineEdit()
        form.addRow("Notes", self.notes_input)

        layout.addLayout(form)

        buttons = QDialogButtonBox(QDialogButtonBox.Ok | QDialogButtonBox.Cancel)
        buttons.accepted.connect(self.accept)
        buttons.rejected.connect(self.reject)
        layout.addWidget(buttons)

    def get_data(self):
        return {
            "amount": Decimal(str(self.amount_input.value())),
            "method": self.method_combo.currentText(),
            "notes": self.notes_input.text().strip(),
        }


class LabellingPage(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setObjectName("Page")
        self._build_ui()
        self.refresh()

    def _build_ui(self):
        root = QVBoxLayout(self)
        root.setContentsMargins(24, 24, 24, 24)
        root.setSpacing(20)

        # Header
        header = QHBoxLayout()
        title = QLabel("Labelling Jobs")
        title.setObjectName("PageTitle")
        subtitle = QLabel("Track product labelling orders & installment payments")
        subtitle.setObjectName("PageSubtitle")
        title_box = QVBoxLayout()
        title_box.setSpacing(2)
        title_box.addWidget(title)
        title_box.addWidget(subtitle)
        header.addLayout(title_box)
        header.addStretch()

        if has_permission("labelling.create"):
            new_btn = QPushButton("+ New Job")
            new_btn.setObjectName("PrimaryButton")
            new_btn.setMinimumHeight(38)
            new_btn.clicked.connect(self._on_new_job)
            header.addWidget(new_btn)
        root.addLayout(header)

        # KPIs
        cards = QHBoxLayout()
        cards.setSpacing(16)
        self.card_pending = KpiCard("Pending Jobs", "0", subtitle="Awaiting work")
        self.card_inprogress = KpiCard("In Progress", "0", subtitle="Partially paid")
        self.card_outstanding = KpiCard("Outstanding", "$0.00", subtitle="Total balance due")
        self.card_today_pay = KpiCard("Today's Payments", "$0.00", subtitle="All methods")
        for c in (self.card_pending, self.card_inprogress, self.card_outstanding, self.card_today_pay):
            cards.addWidget(c, 1)
        root.addLayout(cards)

        # Filters
        filters = QFrame()
        filters.setObjectName("Card")
        f_layout = QHBoxLayout(filters)
        f_layout.setContentsMargins(16, 12, 16, 12)
        f_layout.setSpacing(12)

        f_layout.addWidget(QLabel("Search:"))
        self.search_input = QLineEdit()
        self.search_input.setPlaceholderText("Customer or product...")
        self.search_input.setMinimumWidth(220)
        self.search_input.textChanged.connect(self.refresh)
        f_layout.addWidget(self.search_input)

        f_layout.addWidget(QLabel("Status:"))
        self.status_combo = QComboBox()
        self.status_combo.addItems(JOB_STATUSES)
        self.status_combo.currentIndexChanged.connect(self.refresh)
        f_layout.addWidget(self.status_combo)

        f_layout.addStretch()
        refresh_btn = QPushButton("Refresh")
        refresh_btn.clicked.connect(self.refresh)
        f_layout.addWidget(refresh_btn)
        root.addWidget(filters)

        # Table
        self.table = StyledTable([
            "Date", "Customer", "Product", "Qty", "Total", "Paid", "Balance", "Status"
        ])
        self.table.cellDoubleClicked.connect(self._on_view_job)

        # Action buttons row
        actions_row = QHBoxLayout()
        actions_row.addStretch()
        self.payment_btn = QPushButton("Add Payment")
        self.payment_btn.setObjectName("PrimaryButton")
        self.payment_btn.clicked.connect(self._on_add_payment)
        self.edit_btn = QPushButton("Edit Job")
        self.edit_btn.clicked.connect(self._on_edit_job)
        self.cancel_btn = QPushButton("Cancel Job")
        self.cancel_btn.setObjectName("DangerButton")
        self.cancel_btn.clicked.connect(self._on_cancel_job)
        for b in (self.edit_btn, self.cancel_btn, self.payment_btn):
            actions_row.addWidget(b)

        root.addWidget(self.table, 1)
        root.addLayout(actions_row)

    def refresh(self):
        with session_scope() as session:
            svc = LabellingService(session)
            search = self.search_input.text().strip()
            status = self.status_combo.currentText()
            jobs = svc.list_jobs(status=status, search=search, limit=300)
            self._populate_table(jobs)

            pending = sum(1 for j in jobs if j.status == JobStatus.PENDING.value)
            inprog = sum(1 for j in jobs if j.status == JobStatus.IN_PROGRESS.value)
            self.card_pending.set_value(str(pending))
            self.card_inprogress.set_value(str(inprog))
            self.card_outstanding.set_value(format_money(svc.outstanding_total()))
            self.card_today_pay.set_value(format_money(svc.today_payments_total()))

    def _populate_table(self, jobs):
        self.table.setRowCount(0)
        for j in jobs:
            row = self.table.rowCount()
            self.table.insertRow(row)
            cells = [
                format_datetime(j.created_at),
                j.customer_name,
                j.product_description[:60] + ("..." if len(j.product_description) > 60 else ""),
                str(j.quantity),
                format_money(j.total_amount),
                format_money(j.paid_amount),
                format_money(j.balance_amount),
                j.status,
            ]
            for col, value in enumerate(cells):
                item = QTableWidgetItem(str(value))
                if col in (3, 4, 5, 6):
                    item.setTextAlignment(Qt.AlignRight | Qt.AlignVCenter)
                if col == 6 and j.balance_amount > 0:
                    item.setForeground(QColor("#dc2626"))
                if col == 7:
                    if j.status == JobStatus.COMPLETED.value:
                        item.setForeground(QColor("#16a34a"))
                    elif j.status == JobStatus.CANCELLED.value:
                        item.setForeground(QColor("#94a3b8"))
                    elif j.status == JobStatus.IN_PROGRESS.value:
                        item.setForeground(QColor("#d97706"))
                item.setData(Qt.UserRole, j.id)
                self.table.setItem(row, col, item)
        set_column_widths(self.table, [140, 160, None, 70, 100, 100, 100, 110])

    # ----- Actions -----
    def _selected_job_id(self):
        row = self.table.currentRow()
        if row < 0:
            return None
        item = self.table.item(row, 0)
        return item.data(Qt.UserRole) if item else None

    def _on_new_job(self):
        if not has_permission("labelling.create"):
            QMessageBox.warning(self, "Access denied", "You cannot create jobs.")
            return
        dlg = JobDialog(self)
        if not dlg.exec():
            return
        data = dlg.get_data()
        try:
            with session_scope() as session:
                LabellingService(session).create_job(
                    customer_name=data["customer_name"],
                    product_description=data["product_description"],
                    total_amount=data["total_amount"],
                    quantity=data["quantity"],
                    notes=data["notes"],
                    deposit=data["deposit"],
                    deposit_method=data["deposit_method"],
                )
            self.refresh()
        except (LabellingError, AuthError) as e:
            QMessageBox.warning(self, "Cannot create job", str(e))

    def _on_edit_job(self):
        job_id = self._selected_job_id()
        if not job_id:
            QMessageBox.information(self, "Select job", "Please select a job first.")
            return
        with session_scope() as session:
            job = LabellingService(session).get_job(job_id)
            if not job:
                return
            session.expunge(job)
        dlg = JobDialog(self, job=job)
        if not dlg.exec():
            return
        data = dlg.get_data()
        try:
            with session_scope() as session:
                LabellingService(session).update_job(
                    job_id,
                    customer_name=data["customer_name"],
                    product_description=data["product_description"],
                    total_amount=data["total_amount"],
                    quantity=data["quantity"],
                    notes=data["notes"],
                )
            self.refresh()
        except (LabellingError, AuthError) as e:
            QMessageBox.warning(self, "Cannot update job", str(e))

    def _on_add_payment(self):
        job_id = self._selected_job_id()
        if not job_id:
            QMessageBox.information(self, "Select job", "Please select a job first.")
            return
        with session_scope() as session:
            job = LabellingService(session).get_job(job_id)
            if not job:
                return
            if job.status == JobStatus.COMPLETED.value:
                QMessageBox.information(self, "Fully paid", "This job is already fully paid.")
                return
            if job.status == JobStatus.CANCELLED.value:
                QMessageBox.warning(self, "Cancelled", "Cannot pay a cancelled job.")
                return
            session.expunge(job)
        dlg = PaymentDialog(self, job=job)
        if not dlg.exec():
            return
        data = dlg.get_data()
        try:
            with session_scope() as session:
                LabellingService(session).add_payment(
                    job_id,
                    amount=data["amount"],
                    method=data["method"],
                    notes=data["notes"],
                )
            self.refresh()
        except (LabellingError, AuthError) as e:
            QMessageBox.warning(self, "Cannot record payment", str(e))

    def _on_cancel_job(self):
        if not has_permission("labelling.cancel"):
            QMessageBox.warning(self, "Access denied", "Only managers/admins can cancel jobs.")
            return
        job_id = self._selected_job_id()
        if not job_id:
            QMessageBox.information(self, "Select job", "Please select a job first.")
            return
        if not ConfirmDialog.ask(
            self, "Cancel Job",
            "Are you sure you want to cancel this job? Payments already received are kept on record."
        ):
            return
        try:
            with session_scope() as session:
                LabellingService(session).cancel_job(job_id, reason="Cancelled by user")
            self.refresh()
        except (LabellingError, AuthError) as e:
            QMessageBox.warning(self, "Cannot cancel job", str(e))

    def _on_view_job(self, row, _col):
        item = self.table.item(row, 0)
        if not item:
            return
        job_id = item.data(Qt.UserRole)
        with session_scope() as session:
            job = LabellingService(session).get_job(job_id)
            if not job:
                return
            lines = [
                f"Customer: {job.customer_name}",
                f"Product: {job.product_description}",
                f"Quantity: {job.quantity}",
                f"Total: {format_money(job.total_amount)}",
                f"Paid: {format_money(job.paid_amount)}",
                f"Balance: {format_money(job.balance_amount)}",
                f"Status: {job.status}",
                "",
                "Payment History:",
            ]
            if not job.payments:
                lines.append("  (no payments yet)")
            else:
                for p in sorted(job.payments, key=lambda x: x.created_at):
                    lines.append(
                        f"  - {format_datetime(p.created_at)} : "
                        f"{format_money(p.amount)} ({p.payment_method})"
                        + (f" - {p.notes}" if p.notes else "")
                    )
            QMessageBox.information(self, f"Job #{job.id}", "\n".join(lines))
