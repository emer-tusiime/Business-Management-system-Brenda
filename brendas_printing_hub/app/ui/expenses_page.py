"""
Business Expenses page - record, filter, edit and delete operating costs.
"""
from __future__ import annotations

from datetime import date, datetime, timedelta
from typing import List, Optional

from PySide6.QtCore import Qt, QDate, QDateTime
from PySide6.QtGui import QColor
from PySide6.QtWidgets import (
    QComboBox,
    QDateEdit,
    QDateTimeEdit,
    QDialog,
    QDialogButtonBox,
    QDoubleSpinBox,
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
    EXPENSE_CATEGORIES,
    PAYMENT_CASH,
    PAYMENT_METHODS,
    ROLE_ADMIN,
)
from app.core.utils import (
    day_bounds,
    format_datetime,
    format_money,
    month_bounds,
    parse_money,
    week_bounds,
)
from app.services import expense_service
from app.services.audit_service import log_action
from app.ui.components.cards import Panel, StatCard, StatusBadge
from app.ui.components.dialogs import confirm, error, info, toast
from app.ui.components.tables import configure_table, make_item, make_money_item


# ---------------------------------------------------------------------------
# Add / Edit dialog
# ---------------------------------------------------------------------------

class ExpenseDialog(QDialog):
    """Dialog for creating or editing a business expense."""

    def __init__(self, parent: QWidget | None = None,
                 expense: expense_service.ExpenseSummary | None = None) -> None:
        super().__init__(parent)
        self.setWindowTitle("Edit Expense" if expense else "Record Expense")
        self.setModal(True)
        self.resize(460, 0)
        self._expense = expense

        layout = QVBoxLayout(self)
        layout.setContentsMargins(20, 18, 20, 18)
        layout.setSpacing(10)

        title = QLabel("Edit business expense" if expense else "Record a business expense")
        title.setStyleSheet("font-size:16px; font-weight:700; color:#0F172A;")
        layout.addWidget(title)
        sub = QLabel("Operating costs (paper, rent, electricity, ink, repairs, etc.).")
        sub.setStyleSheet("color:#64748B; font-size:12px;")
        layout.addWidget(sub)

        form = QFormLayout()
        form.setSpacing(8)

        self.category_combo = QComboBox()
        self.category_combo.addItems(EXPENSE_CATEGORIES)
        if expense:
            idx = self.category_combo.findText(expense.category)
            if idx >= 0:
                self.category_combo.setCurrentIndex(idx)

        self.amount_input = QLineEdit()
        self.amount_input.setPlaceholderText("e.g. 25,000")
        if expense:
            self.amount_input.setText(format_money(expense.amount, with_symbol=False))

        self.date_input = QDateTimeEdit()
        self.date_input.setCalendarPopup(True)
        self.date_input.setDisplayFormat("dd MMM yyyy  HH:mm")
        self.date_input.setDateTime(
            QDateTime(expense.expense_date) if expense else QDateTime.currentDateTime()
        )

        self.method_combo = QComboBox()
        self.method_combo.addItems(PAYMENT_METHODS)
        if expense:
            idx = self.method_combo.findText(expense.payment_method)
            if idx >= 0:
                self.method_combo.setCurrentIndex(idx)
        else:
            self.method_combo.setCurrentText(PAYMENT_CASH)

        self.description_input = QLineEdit()
        self.description_input.setPlaceholderText("Short description (optional)")
        if expense:
            self.description_input.setText(expense.description)

        self.notes_input = QTextEdit()
        self.notes_input.setPlaceholderText("Additional notes (optional)")
        self.notes_input.setMaximumHeight(80)
        if expense:
            self.notes_input.setPlainText(expense.notes)

        form.addRow("Category", self.category_combo)
        form.addRow("Amount (UGX)", self.amount_input)
        form.addRow("Date", self.date_input)
        form.addRow("Payment method", self.method_combo)
        form.addRow("Description", self.description_input)
        form.addRow("Notes", self.notes_input)
        layout.addLayout(form)

        buttons = QDialogButtonBox(
            QDialogButtonBox.Save | QDialogButtonBox.Cancel
        )
        buttons.accepted.connect(self._accept)
        buttons.rejected.connect(self.reject)
        layout.addWidget(buttons)

    # ------------------------------------------------------------------

    def _accept(self) -> None:
        amount = parse_money(self.amount_input.text())
        if amount <= 0:
            error(self, "Invalid amount", "Please enter an amount greater than 0.")
            return
        category = self.category_combo.currentText()
        method = self.method_combo.currentText()
        when = self.date_input.dateTime().toPython()
        description = self.description_input.text().strip()
        notes = self.notes_input.toPlainText().strip()

        cu = current_user()
        try:
            if self._expense is None:
                new_id = expense_service.create_expense(
                    category=category,
                    amount=float(amount),
                    expense_date=when,
                    description=description,
                    payment_method=method,
                    notes=notes,
                    recorded_by=cu.id if cu else None,
                )
                log_action(
                    AUDIT_CREATE,
                    module="expenses",
                    description=(
                        f"Recorded expense #{new_id}: {category} - "
                        f"{format_money(amount)}"
                    ),
                )
            else:
                expense_service.update_expense(
                    self._expense.id,
                    category=category,
                    amount=float(amount),
                    expense_date=when,
                    description=description,
                    payment_method=method,
                    notes=notes,
                )
                log_action(
                    AUDIT_UPDATE,
                    module="expenses",
                    description=(
                        f"Updated expense #{self._expense.id}: {category} - "
                        f"{format_money(amount)}"
                    ),
                )
        except expense_service.ExpenseError as exc:
            error(self, "Could not save expense", str(exc))
            return

        self.accept()


# ---------------------------------------------------------------------------
# Page
# ---------------------------------------------------------------------------

PRESETS = ("Today", "This week", "This month", "Last 30 days", "All time")


class ExpensesPage(QWidget):
    def __init__(self, parent: QWidget | None = None) -> None:
        super().__init__(parent)
        self._rows: List[expense_service.ExpenseSummary] = []
        self._build_ui()
        self.refresh()

    # ------------------------------------------------------------------
    # UI
    # ------------------------------------------------------------------

    def _build_ui(self) -> None:
        layout = QVBoxLayout(self)
        layout.setContentsMargins(28, 24, 28, 28)
        layout.setSpacing(14)

        # Header
        head = QHBoxLayout()
        col = QVBoxLayout()
        col.setSpacing(2)
        title = QLabel("Business Expenses")
        title.setStyleSheet("font-size:22px; font-weight:700; color:#0F172A;")
        sub = QLabel("Track operating costs that reduce profit.")
        sub.setStyleSheet("color:#64748B;")
        col.addWidget(title)
        col.addWidget(sub)
        head.addLayout(col)
        head.addStretch(1)

        self._add_btn = QPushButton("  + Record Expense")
        self._add_btn.setObjectName("PrimaryButton")
        self._add_btn.setCursor(Qt.PointingHandCursor)
        self._add_btn.clicked.connect(self._on_add)
        head.addWidget(self._add_btn)
        layout.addLayout(head)

        # Stat cards
        cards_row = QHBoxLayout()
        cards_row.setSpacing(12)
        self._card_today = StatCard("Today", accent="#2563EB")
        self._card_month = StatCard("This month", accent="#0EA5E9")
        self._card_count = StatCard("Records (this month)", accent="#94A3B8")
        cards_row.addWidget(self._card_today)
        cards_row.addWidget(self._card_month)
        cards_row.addWidget(self._card_count)
        layout.addLayout(cards_row)

        # Filter bar
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

        self._category_combo = QComboBox()
        self._category_combo.addItem("All categories", None)
        for cat in EXPENSE_CATEGORIES:
            self._category_combo.addItem(cat, cat)

        self._search_input = QLineEdit()
        self._search_input.setPlaceholderText("Search description / notes")
        self._search_input.setClearButtonEnabled(True)

        apply_btn = QPushButton("Apply")
        apply_btn.clicked.connect(self.refresh)

        bar.addWidget(QLabel("Period:"))
        bar.addWidget(self._preset_combo)
        bar.addWidget(QLabel("From"))
        bar.addWidget(self._from_input)
        bar.addWidget(QLabel("To"))
        bar.addWidget(self._to_input)
        bar.addWidget(QLabel("Category:"))
        bar.addWidget(self._category_combo)
        bar.addWidget(self._search_input, stretch=1)
        bar.addWidget(apply_btn)
        filters.add_layout(bar)
        layout.addWidget(filters)

        # Set initial date range from default preset
        self._apply_preset(self._preset_combo.currentText())

        # Table
        panel = Panel(title="Expense records")
        self.table = QTableWidget()
        configure_table(
            self.table,
            ["Date", "Category", "Description", "Amount", "Method", "Notes"],
            stretch_last=True,
            resize_modes={
                0: QHeaderView.ResizeToContents,
                1: QHeaderView.ResizeToContents,
                3: QHeaderView.ResizeToContents,
                4: QHeaderView.ResizeToContents,
            },
        )
        self.table.itemSelectionChanged.connect(self._update_action_buttons)
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
    # Filter helpers
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
        else:  # All time - use a wide range
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
    # Data loading
    # ------------------------------------------------------------------

    def refresh(self) -> None:
        start, end = self._date_range()
        category = self._category_combo.currentData()
        search = self._search_input.text().strip()

        try:
            rows = expense_service.list_expenses(
                start=start,
                end=end,
                category=category,
                search=search,
                limit=1000,
            )
        except Exception as exc:  # pragma: no cover
            error(self, "Could not load expenses", str(exc))
            return

        self._rows = rows
        self._populate_table(rows)
        self._update_cards()
        self._update_action_buttons()

    def _populate_table(self, rows: List[expense_service.ExpenseSummary]) -> None:
        self.table.setRowCount(len(rows))
        for r, row in enumerate(rows):
            self.table.setItem(
                r, 0, make_item(format_datetime(row.expense_date), data=row.id)
            )
            self.table.setItem(r, 1, make_item(row.category))
            self.table.setItem(r, 2, make_item(row.description or "-"))
            self.table.setItem(r, 3, make_money_item(row.amount, bold=True))
            self.table.setItem(r, 4, make_item(row.payment_method))
            self.table.setItem(r, 5, make_item(row.notes or ""))

    def _update_cards(self) -> None:
        try:
            today_start, today_end = day_bounds(date.today())
            month_start, month_end = month_bounds(date.today())
            today_total = expense_service.total_in_range(today_start, today_end)
            month_total = expense_service.total_in_range(month_start, month_end)
            month_count = sum(
                1 for r in expense_service.list_expenses(
                    start=month_start, end=month_end, limit=10_000
                )
            )
        except Exception as exc:  # pragma: no cover
            print(f"[v0] expenses cards refresh failed: {exc}")
            return

        self._card_today.set_money(today_total)
        self._card_month.set_money(month_total)
        self._card_count.set_value(str(month_count))

    # ------------------------------------------------------------------
    # Selection / actions
    # ------------------------------------------------------------------

    def _selected_row(self) -> Optional[expense_service.ExpenseSummary]:
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

    def _update_action_buttons(self) -> None:
        has_selection = self._selected_row() is not None
        self._edit_btn.setEnabled(has_selection)
        cu = current_user()
        is_admin = cu is not None and cu.role == ROLE_ADMIN
        self._delete_btn.setEnabled(has_selection and is_admin)

    def _on_add(self) -> None:
        dlg = ExpenseDialog(self)
        if dlg.exec() == QDialog.Accepted:
            toast(self, "Expense saved", "success")
            self.refresh()

    def _on_edit(self) -> None:
        row = self._selected_row()
        if row is None:
            return
        dlg = ExpenseDialog(self, expense=row)
        if dlg.exec() == QDialog.Accepted:
            toast(self, "Expense updated", "success")
            self.refresh()

    def _on_delete(self) -> None:
        row = self._selected_row()
        if row is None:
            return
        if not confirm(
            self,
            "Delete expense",
            (
                f"Delete this {row.category} expense of "
                f"{format_money(row.amount)}? This cannot be undone."
            ),
            destructive=True,
        ):
            return
        try:
            expense_service.delete_expense(row.id)
        except expense_service.ExpenseError as exc:
            error(self, "Could not delete", str(exc))
            return
        log_action(
            AUDIT_DELETE,
            module="expenses",
            description=(
                f"Deleted expense #{row.id}: {row.category} - "
                f"{format_money(row.amount)}"
            ),
        )
        toast(self, "Expense deleted", "info")
        self.refresh()
