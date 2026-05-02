"""
Owner Withdrawals page - record personal money taken from the till.

These do NOT affect reported profit, but they DO reduce cash on hand and are
displayed prominently so the owner can see exactly how much has been drawn.
"""
from __future__ import annotations

from datetime import date, datetime, timedelta
from typing import List, Optional

from PySide6.QtCore import Qt, QDate, QDateTime
from PySide6.QtWidgets import (
    QComboBox,
    QDateEdit,
    QDateTimeEdit,
    QDialog,
    QDialogButtonBox,
    QFormLayout,
    QHBoxLayout,
    QHeaderView,
    QLabel,
    QLineEdit,
    QPushButton,
    QTableWidget,
    QTextEdit,
    QVBoxLayout,
    QWidget,
)

from app.core.auth import current_user
from app.core.constants import (
    AUDIT_CREATE,
    AUDIT_DELETE,
    AUDIT_UPDATE,
    ROLE_ADMIN,
    WITHDRAWAL_REASONS,
)
from app.core.utils import (
    day_bounds,
    format_datetime,
    format_money,
    month_bounds,
    parse_money,
    week_bounds,
)
from app.services import withdrawal_service
from app.services.audit_service import log_action
from app.ui.components.cards import Panel, StatCard
from app.ui.components.dialogs import confirm, error, toast
from app.ui.components.tables import configure_table, make_item, make_money_item


# ---------------------------------------------------------------------------
# Dialog
# ---------------------------------------------------------------------------

class WithdrawalDialog(QDialog):
    def __init__(self, parent: QWidget | None = None,
                 withdrawal: withdrawal_service.WithdrawalSummary | None = None) -> None:
        super().__init__(parent)
        self.setWindowTitle("Edit Withdrawal" if withdrawal else "Record Withdrawal")
        self.setModal(True)
        self.resize(440, 0)
        self._withdrawal = withdrawal

        layout = QVBoxLayout(self)
        layout.setContentsMargins(20, 18, 20, 18)
        layout.setSpacing(10)

        title = QLabel(
            "Edit owner withdrawal" if withdrawal else "Record owner withdrawal"
        )
        title.setStyleSheet("font-size:16px; font-weight:700; color:#0F172A;")
        sub = QLabel("Personal money taken from the till. Does not reduce profit.")
        sub.setStyleSheet("color:#64748B; font-size:12px;")
        layout.addWidget(title)
        layout.addWidget(sub)

        form = QFormLayout()
        form.setSpacing(8)

        self.reason_combo = QComboBox()
        self.reason_combo.addItems(WITHDRAWAL_REASONS)
        if withdrawal:
            idx = self.reason_combo.findText(withdrawal.reason)
            if idx >= 0:
                self.reason_combo.setCurrentIndex(idx)

        self.amount_input = QLineEdit()
        self.amount_input.setPlaceholderText("e.g. 50,000")
        if withdrawal:
            self.amount_input.setText(format_money(withdrawal.amount, with_symbol=False))

        self.date_input = QDateTimeEdit()
        self.date_input.setCalendarPopup(True)
        self.date_input.setDisplayFormat("dd MMM yyyy  HH:mm")
        self.date_input.setDateTime(
            QDateTime(withdrawal.withdrawal_date) if withdrawal else QDateTime.currentDateTime()
        )

        self.taken_by_input = QLineEdit()
        self.taken_by_input.setPlaceholderText("Person collecting (optional)")
        if withdrawal:
            self.taken_by_input.setText(withdrawal.taken_by)

        self.notes_input = QTextEdit()
        self.notes_input.setMaximumHeight(80)
        self.notes_input.setPlaceholderText("Notes (optional)")
        if withdrawal:
            self.notes_input.setPlainText(withdrawal.notes)

        form.addRow("Reason", self.reason_combo)
        form.addRow("Amount (UGX)", self.amount_input)
        form.addRow("Date / time", self.date_input)
        form.addRow("Taken by", self.taken_by_input)
        form.addRow("Notes", self.notes_input)
        layout.addLayout(form)

        buttons = QDialogButtonBox(
            QDialogButtonBox.Save | QDialogButtonBox.Cancel
        )
        buttons.accepted.connect(self._accept)
        buttons.rejected.connect(self.reject)
        layout.addWidget(buttons)

    def _accept(self) -> None:
        amount = parse_money(self.amount_input.text())
        if amount <= 0:
            error(self, "Invalid amount", "Please enter an amount greater than 0.")
            return

        reason = self.reason_combo.currentText()
        when = self.date_input.dateTime().toPython()
        taken_by = self.taken_by_input.text().strip()
        notes = self.notes_input.toPlainText().strip()
        cu = current_user()

        try:
            if self._withdrawal is None:
                new_id = withdrawal_service.create_withdrawal(
                    reason=reason,
                    amount=float(amount),
                    withdrawal_date=when,
                    taken_by=taken_by,
                    notes=notes,
                    approved_by=cu.id if cu else None,
                )
                log_action(
                    AUDIT_CREATE,
                    module="withdrawals",
                    description=(
                        f"Recorded withdrawal #{new_id}: {reason} - "
                        f"{format_money(amount)}"
                    ),
                )
            else:
                withdrawal_service.update_withdrawal(
                    self._withdrawal.id,
                    reason=reason,
                    amount=float(amount),
                    withdrawal_date=when,
                    taken_by=taken_by,
                    notes=notes,
                )
                log_action(
                    AUDIT_UPDATE,
                    module="withdrawals",
                    description=(
                        f"Updated withdrawal #{self._withdrawal.id}: "
                        f"{reason} - {format_money(amount)}"
                    ),
                )
        except withdrawal_service.WithdrawalError as exc:
            error(self, "Could not save withdrawal", str(exc))
            return

        self.accept()


# ---------------------------------------------------------------------------
# Page
# ---------------------------------------------------------------------------

PRESETS = ("Today", "This week", "This month", "Last 30 days", "All time")


class WithdrawalsPage(QWidget):
    def __init__(self, parent: QWidget | None = None) -> None:
        super().__init__(parent)
        self._rows: List[withdrawal_service.WithdrawalSummary] = []
        self._build_ui()
        self.refresh()

    def _build_ui(self) -> None:
        layout = QVBoxLayout(self)
        layout.setContentsMargins(28, 24, 28, 28)
        layout.setSpacing(14)

        head = QHBoxLayout()
        col = QVBoxLayout()
        col.setSpacing(2)
        title = QLabel("Owner Withdrawals")
        title.setStyleSheet("font-size:22px; font-weight:700; color:#0F172A;")
        sub = QLabel(
            "Personal money taken from the till. Reduces cash on hand but not profit."
        )
        sub.setStyleSheet("color:#64748B;")
        col.addWidget(title)
        col.addWidget(sub)
        head.addLayout(col)
        head.addStretch(1)

        self._add_btn = QPushButton("  + Record Withdrawal")
        self._add_btn.setObjectName("PrimaryButton")
        self._add_btn.setCursor(Qt.PointingHandCursor)
        self._add_btn.clicked.connect(self._on_add)
        head.addWidget(self._add_btn)
        layout.addLayout(head)

        # Cards
        cards_row = QHBoxLayout()
        cards_row.setSpacing(12)
        self._card_today = StatCard("Today", accent="#D97706")
        self._card_month = StatCard("This month", accent="#DC2626")
        self._card_count = StatCard("Records (this month)", accent="#94A3B8")
        cards_row.addWidget(self._card_today)
        cards_row.addWidget(self._card_month)
        cards_row.addWidget(self._card_count)
        layout.addLayout(cards_row)

        # Filters
        filters = Panel(title="Filters")
        bar = QHBoxLayout()
        bar.setSpacing(8)

        self._preset_combo = QComboBox()
        self._preset_combo.addItems(PRESETS)
        self._preset_combo.setCurrentText("This month")
        self._preset_combo.currentTextChanged.connect(self._on_preset_changed)

        self._from_input = QDateEdit()
        self._from_input.setCalendarPopup(True)
        self._from_input.setDisplayFormat("dd MMM yyyy")
        self._to_input = QDateEdit()
        self._to_input.setCalendarPopup(True)
        self._to_input.setDisplayFormat("dd MMM yyyy")

        self._reason_combo = QComboBox()
        self._reason_combo.addItem("All reasons", None)
        for r in WITHDRAWAL_REASONS:
            self._reason_combo.addItem(r, r)

        self._search_input = QLineEdit()
        self._search_input.setPlaceholderText("Search taken-by / notes")
        self._search_input.setClearButtonEnabled(True)

        apply_btn = QPushButton("Apply")
        apply_btn.clicked.connect(self.refresh)

        bar.addWidget(QLabel("Period:"))
        bar.addWidget(self._preset_combo)
        bar.addWidget(QLabel("From"))
        bar.addWidget(self._from_input)
        bar.addWidget(QLabel("To"))
        bar.addWidget(self._to_input)
        bar.addWidget(QLabel("Reason:"))
        bar.addWidget(self._reason_combo)
        bar.addWidget(self._search_input, stretch=1)
        bar.addWidget(apply_btn)
        filters.add_layout(bar)
        layout.addWidget(filters)

        self._apply_preset(self._preset_combo.currentText())

        # Table
        panel = Panel(title="Withdrawal records")
        self.table = QTableWidget()
        configure_table(
            self.table,
            ["Date", "Reason", "Taken by", "Amount", "Notes"],
            stretch_last=True,
            resize_modes={
                0: QHeaderView.ResizeToContents,
                1: QHeaderView.ResizeToContents,
                3: QHeaderView.ResizeToContents,
            },
        )
        self.table.itemSelectionChanged.connect(self._update_actions)
        panel.add_widget(self.table)

        actions = QHBoxLayout()
        actions.addStretch(1)
        self._edit_btn = QPushButton("Edit")
        self._edit_btn.setEnabled(False)
        self._edit_btn.clicked.connect(self._on_edit)
        self._delete_btn = QPushButton("Delete")
        self._delete_btn.setObjectName("DangerButton")
        self._delete_btn.setEnabled(False)
        self._delete_btn.clicked.connect(self._on_delete)
        actions.addWidget(self._edit_btn)
        actions.addWidget(self._delete_btn)
        panel.add_layout(actions)

        layout.addWidget(panel, stretch=1)

    # ------------------------------------------------------------------

    def _apply_preset(self, name: str) -> None:
        today = date.today()
        if name == "Today":
            d_from, d_to = today, today
        elif name == "This week":
            start, end = week_bounds(today)
            d_from, d_to = start.date(), end.date() - timedelta(days=1)
        elif name == "This month":
            start, end = month_bounds(today)
            d_from, d_to = start.date(), end.date() - timedelta(days=1)
        elif name == "Last 30 days":
            d_from, d_to = today - timedelta(days=29), today
        else:
            d_from, d_to = date(2020, 1, 1), today
        self._from_input.setDate(QDate(d_from.year, d_from.month, d_from.day))
        self._to_input.setDate(QDate(d_to.year, d_to.month, d_to.day))

    def _on_preset_changed(self, name: str) -> None:
        self._apply_preset(name)
        self.refresh()

    def _date_range(self) -> tuple[Optional[datetime], Optional[datetime]]:
        d_from = self._from_input.date().toPython()
        d_to = self._to_input.date().toPython()
        if d_from > d_to:
            d_from, d_to = d_to, d_from
        start = datetime.combine(d_from, datetime.min.time())
        end = datetime.combine(d_to + timedelta(days=1), datetime.min.time())
        return start, end

    # ------------------------------------------------------------------

    def refresh(self) -> None:
        start, end = self._date_range()
        reason = self._reason_combo.currentData()
        search = self._search_input.text().strip()

        try:
            rows = withdrawal_service.list_withdrawals(
                start=start, end=end, reason=reason, search=search, limit=1000,
            )
        except Exception as exc:  # pragma: no cover
            error(self, "Could not load withdrawals", str(exc))
            return

        self._rows = rows
        self._populate_table(rows)
        self._update_cards()
        self._update_actions()

    def _populate_table(self, rows: List[withdrawal_service.WithdrawalSummary]) -> None:
        self.table.setRowCount(len(rows))
        for r, row in enumerate(rows):
            self.table.setItem(
                r, 0, make_item(format_datetime(row.withdrawal_date), data=row.id)
            )
            self.table.setItem(r, 1, make_item(row.reason))
            self.table.setItem(r, 2, make_item(row.taken_by or "-"))
            self.table.setItem(r, 3, make_money_item(row.amount, bold=True))
            self.table.setItem(r, 4, make_item(row.notes or ""))

    def _update_cards(self) -> None:
        try:
            today_start, today_end = day_bounds(date.today())
            month_start, month_end = month_bounds(date.today())
            today_total = withdrawal_service.total_in_range(today_start, today_end)
            month_total = withdrawal_service.total_in_range(month_start, month_end)
            month_count = sum(
                1 for _ in withdrawal_service.list_withdrawals(
                    start=month_start, end=month_end, limit=10_000
                )
            )
        except Exception as exc:  # pragma: no cover
            print(f"[v0] withdrawals cards refresh failed: {exc}")
            return
        self._card_today.set_money(today_total)
        self._card_month.set_money(month_total)
        self._card_count.set_value(str(month_count))

    # ------------------------------------------------------------------

    def _selected_row(self) -> Optional[withdrawal_service.WithdrawalSummary]:
        row = self.table.currentRow()
        if row < 0:
            return None
        item = self.table.item(row, 0)
        if item is None:
            return None
        rid = item.data(Qt.UserRole)
        for r in self._rows:
            if r.id == rid:
                return r
        return None

    def _update_actions(self) -> None:
        sel = self._selected_row() is not None
        self._edit_btn.setEnabled(sel)
        cu = current_user()
        is_admin = cu is not None and cu.role == ROLE_ADMIN
        self._delete_btn.setEnabled(sel and is_admin)

    def _on_add(self) -> None:
        dlg = WithdrawalDialog(self)
        if dlg.exec() == QDialog.Accepted:
            toast(self, "Withdrawal saved", "success")
            self.refresh()

    def _on_edit(self) -> None:
        row = self._selected_row()
        if row is None:
            return
        dlg = WithdrawalDialog(self, withdrawal=row)
        if dlg.exec() == QDialog.Accepted:
            toast(self, "Withdrawal updated", "success")
            self.refresh()

    def _on_delete(self) -> None:
        row = self._selected_row()
        if row is None:
            return
        if not confirm(
            self,
            "Delete withdrawal",
            f"Delete this {row.reason} withdrawal of {format_money(row.amount)}?",
            destructive=True,
        ):
            return
        try:
            withdrawal_service.delete_withdrawal(row.id)
        except withdrawal_service.WithdrawalError as exc:
            error(self, "Could not delete", str(exc))
            return
        log_action(
            AUDIT_DELETE,
            module="withdrawals",
            description=(
                f"Deleted withdrawal #{row.id}: {row.reason} - "
                f"{format_money(row.amount)}"
            ),
        )
        toast(self, "Withdrawal deleted", "info")
        self.refresh()
