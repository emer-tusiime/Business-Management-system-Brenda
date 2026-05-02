"""
Cash Management page.

Sections
--------
1. Today panel - shows the live tally and either an "Open day" form (if not
   open yet) or a "Close day" form (if open).
2. History table of past daily closings, with the running cash difference for
   the month.
"""
from __future__ import annotations

from datetime import date, datetime, timedelta
from typing import List, Optional

from PySide6.QtCore import Qt
from PySide6.QtGui import QColor
from PySide6.QtWidgets import (
    QDialog,
    QDialogButtonBox,
    QFormLayout,
    QFrame,
    QHBoxLayout,
    QHeaderView,
    QLabel,
    QLineEdit,
    QPushButton,
    QTableWidget,
    QTableWidgetItem,
    QTextEdit,
    QVBoxLayout,
    QWidget,
)

from app.core.auth import current_user
from app.core.constants import AUDIT_CLOSE_DAY, AUDIT_CREATE
from app.core.utils import format_date, format_money, parse_money
from app.services import cash_service
from app.services.audit_service import log_action
from app.ui.components.cards import Panel, StatCard, StatusBadge
from app.ui.components.dialogs import confirm, error, info, toast
from app.ui.components.tables import configure_table, make_item, make_money_item


# ---------------------------------------------------------------------------
# Helper: simple labelled rows used inside the today panel
# ---------------------------------------------------------------------------

def _row(label_text: str, value_text: str = "--", *,
         emphasise: bool = False, negative: bool = False) -> tuple[QHBoxLayout, QLabel]:
    row = QHBoxLayout()
    row.setSpacing(8)
    label = QLabel(label_text)
    label.setStyleSheet("color:#475569;")
    value = QLabel(value_text)
    if emphasise:
        value.setStyleSheet(
            "font-size:15px; font-weight:700; "
            f"color:{'#DC2626' if negative else '#0F172A'};"
        )
    else:
        value.setStyleSheet(f"color:{'#DC2626' if negative else '#0F172A'};")
    value.setAlignment(Qt.AlignRight | Qt.AlignVCenter)
    row.addWidget(label)
    row.addStretch(1)
    row.addWidget(value)
    return row, value


# ---------------------------------------------------------------------------
# Open Day dialog
# ---------------------------------------------------------------------------

class OpenDayDialog(QDialog):
    def __init__(self, parent: QWidget | None = None) -> None:
        super().__init__(parent)
        self.setWindowTitle("Open the Day")
        self.setModal(True)
        self.resize(380, 0)

        layout = QVBoxLayout(self)
        layout.setContentsMargins(20, 18, 20, 18)
        layout.setSpacing(10)

        title = QLabel("Open today's cash session")
        title.setStyleSheet("font-size:16px; font-weight:700; color:#0F172A;")
        sub = QLabel(
            "Count the cash float you are starting with and enter it below."
        )
        sub.setStyleSheet("color:#64748B; font-size:12px;")
        sub.setWordWrap(True)
        layout.addWidget(title)
        layout.addWidget(sub)

        form = QFormLayout()
        form.setSpacing(8)

        self.opening_input = QLineEdit()
        self.opening_input.setPlaceholderText("e.g. 100,000")

        self.notes_input = QLineEdit()
        self.notes_input.setPlaceholderText("Notes (optional)")

        form.addRow("Opening cash (UGX)", self.opening_input)
        form.addRow("Notes", self.notes_input)
        layout.addLayout(form)

        buttons = QDialogButtonBox(
            QDialogButtonBox.Ok | QDialogButtonBox.Cancel
        )
        buttons.button(QDialogButtonBox.Ok).setText("Open day")
        buttons.accepted.connect(self._accept)
        buttons.rejected.connect(self.reject)
        layout.addWidget(buttons)

    def _accept(self) -> None:
        amount = parse_money(self.opening_input.text())
        if amount < 0:
            error(self, "Invalid amount", "Opening cash cannot be negative.")
            return
        cu = current_user()
        try:
            cash_service.open_day(
                opening_cash=float(amount),
                opened_by=cu.id if cu else None,
                notes=self.notes_input.text().strip(),
            )
        except cash_service.CashError as exc:
            error(self, "Could not open day", str(exc))
            return
        log_action(
            AUDIT_CREATE,
            module="cash",
            description=f"Opened day with float {format_money(amount)}.",
        )
        self.accept()


# ---------------------------------------------------------------------------
# Close Day dialog
# ---------------------------------------------------------------------------

class CloseDayDialog(QDialog):
    def __init__(self, parent: QWidget | None = None,
                 tally: cash_service.DailyTally | None = None) -> None:
        super().__init__(parent)
        self.setWindowTitle("Close the Day")
        self.setModal(True)
        self.resize(440, 0)
        self._tally = tally

        layout = QVBoxLayout(self)
        layout.setContentsMargins(20, 18, 20, 18)
        layout.setSpacing(10)

        title = QLabel("Close today's cash session")
        title.setStyleSheet("font-size:16px; font-weight:700; color:#0F172A;")
        sub = QLabel(
            "Count the cash drawer and enter the actual amount. We will compare "
            "it with the expected cash and record the difference."
        )
        sub.setStyleSheet("color:#64748B; font-size:12px;")
        sub.setWordWrap(True)
        layout.addWidget(title)
        layout.addWidget(sub)

        # Recap of expected
        recap = QFrame()
        recap.setObjectName("Panel")
        recap_layout = QVBoxLayout(recap)
        recap_layout.setContentsMargins(14, 12, 14, 12)
        recap_layout.setSpacing(4)

        if tally is not None:
            for label, value in [
                ("Opening cash", format_money(tally.opening_cash)),
                ("+ Cash sales", format_money(tally.cash_sales)),
                ("- Cash expenses", format_money(tally.business_expenses)),
                ("- Owner withdrawals", format_money(tally.owner_withdrawals)),
            ]:
                row_layout, _ = _row(label, value)
                recap_layout.addLayout(row_layout)
            line = QFrame()
            line.setFixedHeight(1)
            line.setStyleSheet("background:#E2E8F0;")
            recap_layout.addWidget(line)
            row_layout, _ = _row(
                "Expected cash",
                format_money(tally.expected_cash),
                emphasise=True,
            )
            recap_layout.addLayout(row_layout)
        layout.addWidget(recap)

        form = QFormLayout()
        form.setSpacing(8)
        self.actual_input = QLineEdit()
        self.actual_input.setPlaceholderText("e.g. 250,000")
        self.notes_input = QTextEdit()
        self.notes_input.setMaximumHeight(80)
        self.notes_input.setPlaceholderText("Closing notes (e.g. why there is a difference)")

        form.addRow("Actual cash counted (UGX)", self.actual_input)
        form.addRow("Notes", self.notes_input)
        layout.addLayout(form)

        self.diff_label = QLabel("")
        self.diff_label.setStyleSheet(
            "padding:8px 10px; background:#F1F5F9; border-radius:6px; color:#0F172A;"
        )
        self.diff_label.setVisible(False)
        layout.addWidget(self.diff_label)
        self.actual_input.textChanged.connect(self._update_diff)

        buttons = QDialogButtonBox(
            QDialogButtonBox.Ok | QDialogButtonBox.Cancel
        )
        buttons.button(QDialogButtonBox.Ok).setText("Close day")
        buttons.accepted.connect(self._accept)
        buttons.rejected.connect(self.reject)
        layout.addWidget(buttons)

    def _update_diff(self, _text: str) -> None:
        if self._tally is None:
            return
        amount = parse_money(self.actual_input.text())
        diff = float(amount) - self._tally.expected_cash
        if amount == 0 and not self.actual_input.text().strip():
            self.diff_label.setVisible(False)
            return
        if abs(diff) < 1:
            text = "Cash matches the expected amount exactly."
            color = "#16A34A"
        elif diff > 0:
            text = f"Surplus of {format_money(diff)} (more than expected)."
            color = "#0EA5E9"
        else:
            text = f"Shortfall of {format_money(abs(diff))} (less than expected)."
            color = "#DC2626"
        self.diff_label.setText(text)
        self.diff_label.setStyleSheet(
            f"padding:8px 10px; background:#F1F5F9; border-radius:6px; "
            f"color:{color}; font-weight:600;"
        )
        self.diff_label.setVisible(True)

    def _accept(self) -> None:
        amount = parse_money(self.actual_input.text())
        if amount < 0:
            error(self, "Invalid amount", "Actual cash cannot be negative.")
            return
        cu = current_user()
        try:
            cash_service.close_day(
                actual_cash=float(amount),
                closed_by=cu.id if cu else None,
                closing_notes=self.notes_input.toPlainText().strip(),
            )
        except cash_service.CashError as exc:
            error(self, "Could not close day", str(exc))
            return
        if self._tally is not None:
            diff = float(amount) - self._tally.expected_cash
            log_action(
                AUDIT_CLOSE_DAY,
                module="cash",
                description=(
                    f"Closed day. Expected {format_money(self._tally.expected_cash)}, "
                    f"actual {format_money(amount)}, "
                    f"difference {format_money(diff)}."
                ),
            )
        self.accept()


# ---------------------------------------------------------------------------
# Page
# ---------------------------------------------------------------------------

class CashPage(QWidget):
    def __init__(self, parent: QWidget | None = None) -> None:
        super().__init__(parent)
        self._build_ui()
        self.refresh()

    def _build_ui(self) -> None:
        layout = QVBoxLayout(self)
        layout.setContentsMargins(28, 24, 28, 28)
        layout.setSpacing(14)

        # Header
        head = QHBoxLayout()
        col = QVBoxLayout()
        col.setSpacing(2)
        title = QLabel("Cash Management")
        title.setStyleSheet("font-size:22px; font-weight:700; color:#0F172A;")
        sub = QLabel("Open the day with a float, watch the live tally, close the day with a count.")
        sub.setStyleSheet("color:#64748B;")
        col.addWidget(title)
        col.addWidget(sub)
        head.addLayout(col)
        head.addStretch(1)

        self._refresh_btn = QPushButton("Refresh")
        self._refresh_btn.clicked.connect(self.refresh)
        head.addWidget(self._refresh_btn)
        layout.addLayout(head)

        # KPI strip
        cards_row = QHBoxLayout()
        cards_row.setSpacing(12)
        self._card_status = StatCard("Today's session", "--", accent="#2563EB")
        self._card_expected = StatCard("Expected cash now", accent="#0EA5E9")
        self._card_total_sales = StatCard("Total sales today", accent="#16A34A")
        self._card_mtd_diff = StatCard("MTD reconciliation", "--", accent="#94A3B8")
        cards_row.addWidget(self._card_status)
        cards_row.addWidget(self._card_expected)
        cards_row.addWidget(self._card_total_sales)
        cards_row.addWidget(self._card_mtd_diff)
        layout.addLayout(cards_row)

        # Today panel
        today_panel = Panel(
            title=f"Today - {format_date(date.today(), '%A, %d %B %Y')}",
            subtitle="Live tally of the current cash session",
        )

        body = QHBoxLayout()
        body.setSpacing(16)

        # Left column: tally rows
        left = QVBoxLayout()
        left.setSpacing(4)

        row, self._lbl_opening = _row("Opening cash float")
        left.addLayout(row)
        row, self._lbl_cash_sales = _row("Cash sales")
        left.addLayout(row)
        row, self._lbl_mm_sales = _row("Mobile money sales")
        left.addLayout(row)
        row, self._lbl_bank_sales = _row("Bank sales")
        left.addLayout(row)

        sep1 = QFrame()
        sep1.setFixedHeight(1)
        sep1.setStyleSheet("background:#E2E8F0;")
        left.addWidget(sep1)

        row, self._lbl_cash_exp = _row("Cash expenses (out)")
        left.addLayout(row)
        row, self._lbl_withdrawals = _row("Owner withdrawals (out)")
        left.addLayout(row)

        sep2 = QFrame()
        sep2.setFixedHeight(1)
        sep2.setStyleSheet("background:#E2E8F0;")
        left.addWidget(sep2)

        row, self._lbl_expected = _row("Expected cash on hand", emphasise=True)
        left.addLayout(row)
        body.addLayout(left, stretch=2)

        # Right column: action box
        right = QVBoxLayout()
        right.setSpacing(8)

        self._status_badge = StatusBadge("Not opened", "muted")
        self._status_badge.setMinimumHeight(28)
        right.addWidget(self._status_badge)

        self._action_help = QLabel("")
        self._action_help.setStyleSheet("color:#64748B;")
        self._action_help.setWordWrap(True)
        right.addWidget(self._action_help)
        right.addStretch(1)

        self._open_btn = QPushButton("Open the day")
        self._open_btn.setObjectName("PrimaryButton")
        self._open_btn.clicked.connect(self._on_open_day)
        self._close_btn = QPushButton("Close the day")
        self._close_btn.setObjectName("PrimaryButton")
        self._close_btn.clicked.connect(self._on_close_day)
        right.addWidget(self._open_btn)
        right.addWidget(self._close_btn)

        body.addLayout(right, stretch=1)
        today_panel.add_layout(body)
        layout.addWidget(today_panel)

        # Closing history
        history = Panel(
            title="Closing history",
            subtitle="Last 30 days of daily closings",
        )
        self.table = QTableWidget()
        configure_table(
            self.table,
            [
                "Date",
                "Opening",
                "Cash sales",
                "MoMo sales",
                "Bank sales",
                "Expenses",
                "Withdrawals",
                "Expected",
                "Actual",
                "Difference",
                "Notes",
            ],
            stretch_last=True,
            resize_modes={i: QHeaderView.ResizeToContents for i in range(10)},
        )
        history.add_widget(self.table)
        layout.addWidget(history, stretch=1)

    # ------------------------------------------------------------------
    # Refresh
    # ------------------------------------------------------------------

    def refresh(self) -> None:
        try:
            tally = cash_service.get_daily_tally(date.today())
            closings = cash_service.list_closings(
                start=date.today() - timedelta(days=29),
                end=date.today(),
                limit=200,
            )
            mtd_diff = cash_service.month_to_date_difference()
        except Exception as exc:  # pragma: no cover
            error(self, "Could not load cash data", str(exc))
            return

        self._render_tally(tally)
        self._render_closings(closings)
        self._update_kpis(tally, mtd_diff)

    def _update_kpis(self, tally: cash_service.DailyTally, mtd_diff: float) -> None:
        if tally.is_closed:
            status = "Closed"
        elif tally.is_open:
            status = "Open"
        else:
            status = "Not opened"
        self._card_status.set_value(status)
        self._card_status.set_sublabel(format_date(tally.session_date, "%d %b %Y"))

        self._card_expected.set_money(tally.expected_cash)
        self._card_expected.set_sublabel("Cash drawer should hold this much.")

        self._card_total_sales.set_money(tally.total_sales)
        self._card_total_sales.set_sublabel(
            f"Cash {format_money(tally.cash_sales)} \u2022 "
            f"MoMo {format_money(tally.mobile_money_sales)} \u2022 "
            f"Bank {format_money(tally.bank_sales)}"
        )

        if mtd_diff > 0:
            self._card_mtd_diff.set_value(f"+{format_money(mtd_diff)}")
            self._card_mtd_diff.set_sublabel("Surplus this month")
        elif mtd_diff < 0:
            self._card_mtd_diff.set_value(f"-{format_money(abs(mtd_diff))}")
            self._card_mtd_diff.set_sublabel("Shortfall this month", negative=True)
        else:
            self._card_mtd_diff.set_value(format_money(0))
            self._card_mtd_diff.set_sublabel("Perfectly balanced")

    def _render_tally(self, tally: cash_service.DailyTally) -> None:
        self._lbl_opening.setText(format_money(tally.opening_cash))
        self._lbl_cash_sales.setText(format_money(tally.cash_sales))
        self._lbl_mm_sales.setText(format_money(tally.mobile_money_sales))
        self._lbl_bank_sales.setText(format_money(tally.bank_sales))
        self._lbl_cash_exp.setText(format_money(tally.business_expenses))
        self._lbl_withdrawals.setText(format_money(tally.owner_withdrawals))
        self._lbl_expected.setText(format_money(tally.expected_cash))

        if tally.is_closed:
            self._status_badge.setText("Closed")
            self._status_badge.set_state("muted")
            self._action_help.setText(
                "Today has already been closed. The closing is in the history below."
            )
            self._open_btn.setVisible(False)
            self._close_btn.setVisible(False)
        elif tally.is_open:
            self._status_badge.setText("Open")
            self._status_badge.set_state("success")
            self._action_help.setText(
                "Day is open. When you are ready, count the cash drawer and click "
                "Close the day."
            )
            self._open_btn.setVisible(False)
            self._close_btn.setVisible(True)
        else:
            self._status_badge.setText("Not opened")
            self._status_badge.set_state("warning")
            self._action_help.setText(
                "Click Open the day to start tracking today's cash. Sales recorded "
                "before the day is opened will still be counted, but you will not "
                "have an opening float baseline."
            )
            self._open_btn.setVisible(True)
            self._close_btn.setVisible(False)

    def _render_closings(self, rows: List[cash_service.ClosingSummary]) -> None:
        self.table.setRowCount(len(rows))
        for r, row in enumerate(rows):
            self.table.setItem(r, 0, make_item(format_date(row.session_date)))
            self.table.setItem(r, 1, make_money_item(row.opening_cash))
            self.table.setItem(r, 2, make_money_item(row.cash_sales))
            self.table.setItem(r, 3, make_money_item(row.mobile_money_sales))
            self.table.setItem(r, 4, make_money_item(row.bank_sales))
            self.table.setItem(r, 5, make_money_item(row.business_expenses))
            self.table.setItem(r, 6, make_money_item(row.owner_withdrawals))
            self.table.setItem(r, 7, make_money_item(row.expected_cash))
            self.table.setItem(r, 8, make_money_item(row.actual_cash, bold=True))

            diff_text = format_money(row.difference) if row.difference != 0 else format_money(0)
            if row.difference > 0:
                color = "#0EA5E9"
            elif row.difference < 0:
                color = "#DC2626"
            else:
                color = "#16A34A"
            self.table.setItem(
                r, 9, make_item(diff_text, align=Qt.AlignRight | Qt.AlignVCenter,
                                bold=True, color=color)
            )
            self.table.setItem(r, 10, make_item(row.closing_notes or ""))

    # ------------------------------------------------------------------
    # Actions
    # ------------------------------------------------------------------

    def _on_open_day(self) -> None:
        if cash_service.is_day_open():
            info(self, "Already open", "The day is already open.")
            return
        dlg = OpenDayDialog(self)
        if dlg.exec() == QDialog.Accepted:
            toast(self, "Day opened", "success")
            self.refresh()

    def _on_close_day(self) -> None:
        tally = cash_service.get_daily_tally(date.today())
        if tally.is_closed:
            info(self, "Already closed", "Today has already been closed.")
            return
        if not tally.is_open:
            info(
                self, "Day not open",
                "The day has not been opened yet. Open the day first to record an "
                "opening float, then close it at the end.",
            )
            return
        if not confirm(
            self,
            "Close the day?",
            (
                "Closing the day will write an immutable record. "
                "Make sure all sales, expenses, and withdrawals for today have "
                "been entered. Continue?"
            ),
        ):
            return
        dlg = CloseDayDialog(self, tally=tally)
        if dlg.exec() == QDialog.Accepted:
            toast(self, "Day closed", "success")
            self.refresh()
