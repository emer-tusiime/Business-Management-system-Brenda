"""
Sales page - list, filter, search, add, view, delete.
"""
from __future__ import annotations

import csv
from datetime import date, datetime, timedelta
from typing import List, Optional

from PySide6.QtCore import Qt, QDate
from PySide6.QtWidgets import (
    QComboBox,
    QDateEdit,
    QFileDialog,
    QHBoxLayout,
    QHeaderView,
    QLabel,
    QLineEdit,
    QPushButton,
    QTableWidget,
    QVBoxLayout,
    QWidget,
)

from app.core.auth import current_user
from app.core.constants import (
    AUDIT_CREATE,
    AUDIT_DELETE,
    PAYMENT_METHODS,
    SALE_CATEGORIES,
)
from app.core.utils import day_bounds, format_datetime, format_money
from app.services import sales_service
from app.services.audit_service import log_action
from app.ui.components.cards import Panel, StatCard, StatusBadge
from app.ui.components.dialogs import confirm, error, info, toast
from app.ui.components.tables import configure_table, make_item, make_money_item
from app.ui.sales_dialog import SaleDialog


class SalesPage(QWidget):
    def __init__(self, parent: QWidget | None = None) -> None:
        super().__init__(parent)
        self._sales: List[sales_service.SaleSummary] = []
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
        title_col = QVBoxLayout()
        title = QLabel("Sales")
        title.setStyleSheet("font-size:20px; font-weight:700; color:#0F172A;")
        subtitle = QLabel("Computer Work, Fridge & Labelling sales.")
        subtitle.setStyleSheet("color:#64748B; font-size:12px;")
        title_col.addWidget(title)
        title_col.addWidget(subtitle)
        head.addLayout(title_col)
        head.addStretch(1)

        self.export_btn = QPushButton("Export CSV")
        self.export_btn.clicked.connect(self._on_export)
        self.add_btn = QPushButton("Record Sale")
        self.add_btn.setObjectName("PrimaryButton")
        self.add_btn.clicked.connect(self._on_add_sale)
        head.addWidget(self.export_btn)
        head.addWidget(self.add_btn)
        layout.addLayout(head)

        # KPI strip
        kpi_row = QHBoxLayout()
        kpi_row.setSpacing(12)
        self.kpi_count = StatCard("Sales (filtered)", "0", accent="#2563EB")
        self.kpi_total = StatCard("Total revenue", "UGX 0", accent="#16A34A")
        self.kpi_avg = StatCard("Average sale", "UGX 0", accent="#0EA5E9")
        kpi_row.addWidget(self.kpi_count)
        kpi_row.addWidget(self.kpi_total)
        kpi_row.addWidget(self.kpi_avg)
        layout.addLayout(kpi_row)

        # Filters panel
        filter_panel = Panel("Filters", "")
        filter_row = QHBoxLayout()
        filter_row.setSpacing(10)

        self.search_input = QLineEdit()
        self.search_input.setPlaceholderText("Search description or notes...")
        self.search_input.textChanged.connect(self._on_filter_changed)

        self.start_date = QDateEdit()
        self.start_date.setCalendarPopup(True)
        self.start_date.setDate(QDate.currentDate().addDays(-7))
        self.start_date.dateChanged.connect(self._on_filter_changed)

        self.end_date = QDateEdit()
        self.end_date.setCalendarPopup(True)
        self.end_date.setDate(QDate.currentDate())
        self.end_date.dateChanged.connect(self._on_filter_changed)

        self.category_filter = QComboBox()
        self.category_filter.addItem("All categories", "")
        for cat in SALE_CATEGORIES:
            self.category_filter.addItem(cat, cat)
        self.category_filter.currentIndexChanged.connect(self._on_filter_changed)

        self.payment_filter = QComboBox()
        self.payment_filter.addItem("All payments", "")
        for pm in PAYMENT_METHODS:
            self.payment_filter.addItem(pm, pm)
        self.payment_filter.currentIndexChanged.connect(self._on_filter_changed)

        self.preset_combo = QComboBox()
        for label, days in [
            ("Last 7 days", 7),
            ("Today", 0),
            ("Last 30 days", 30),
            ("Last 90 days", 90),
        ]:
            self.preset_combo.addItem(label, days)
        self.preset_combo.currentIndexChanged.connect(self._on_preset_changed)

        for label, widget in [
            ("Search", self.search_input),
            ("From", self.start_date),
            ("To", self.end_date),
            ("Category", self.category_filter),
            ("Payment", self.payment_filter),
            ("Range", self.preset_combo),
        ]:
            col = QVBoxLayout()
            col.setSpacing(4)
            lbl = QLabel(label)
            lbl.setStyleSheet("color:#64748B; font-size:11px; font-weight:600; text-transform:uppercase;")
            col.addWidget(lbl)
            col.addWidget(widget)
            filter_row.addLayout(col)

        filter_panel.add_layout(filter_row)
        layout.addWidget(filter_panel)

        # Table
        table_panel = Panel("Recent sales", "")
        self.table = QTableWidget(0, 7)
        configure_table(
            self.table,
            ["Date / Time", "Category", "Items", "Payment", "Total", "Notes", "Actions"],
            stretch_last=False,
        )
        header = self.table.horizontalHeader()
        header.setSectionResizeMode(2, QHeaderView.Stretch)
        header.setSectionResizeMode(5, QHeaderView.Stretch)
        self.table.setMinimumHeight(360)
        table_panel.add_widget(self.table)
        layout.addWidget(table_panel, stretch=1)

    # ------------------------------------------------------------------
    # Filter / refresh
    # ------------------------------------------------------------------

    def _on_preset_changed(self) -> None:
        days = self.preset_combo.currentData()
        end = QDate.currentDate()
        start = end if days == 0 else end.addDays(-days)
        self.start_date.blockSignals(True)
        self.end_date.blockSignals(True)
        self.start_date.setDate(start)
        self.end_date.setDate(end)
        self.start_date.blockSignals(False)
        self.end_date.blockSignals(False)
        self.refresh()

    def _on_filter_changed(self) -> None:
        self.refresh()

    def refresh(self) -> None:
        start_qd: QDate = self.start_date.date()
        end_qd: QDate = self.end_date.date()
        start = datetime(start_qd.year(), start_qd.month(), start_qd.day(), 0, 0, 0)
        end = datetime(end_qd.year(), end_qd.month(), end_qd.day(), 0, 0, 0) + timedelta(days=1)

        self._sales = sales_service.list_sales(
            start=start,
            end=end,
            category=self.category_filter.currentData() or None,
            payment_method=self.payment_filter.currentData() or None,
            search=self.search_input.text().strip(),
        )

        # KPIs
        count = len(self._sales)
        total = sum(s.total_amount for s in self._sales)
        avg = (total / count) if count else 0
        self.kpi_count.set_value(str(count))
        self.kpi_total.set_money(total)
        self.kpi_avg.set_money(avg)

        # Table
        self.table.setRowCount(0)
        is_admin = current_user() is not None and current_user().is_admin
        for s in self._sales:
            row = self.table.rowCount()
            self.table.insertRow(row)
            self.table.setItem(row, 0, make_item(format_datetime(s.sale_date)))

            badge = StatusBadge(s.category, state=self._category_state(s.category))
            self.table.setCellWidget(row, 1, _wrap_center(badge))

            self.table.setItem(row, 2, make_item(s.item_summary))
            self.table.setItem(row, 3, make_item(s.payment_method))
            self.table.setItem(row, 4, make_money_item(s.total_amount, bold=True))
            self.table.setItem(row, 5, make_item(s.notes or "-"))

            self.table.setCellWidget(row, 6, self._build_action_cell(s.id, is_admin))

        self.table.resizeColumnsToContents()
        self.table.horizontalHeader().setSectionResizeMode(2, QHeaderView.Stretch)
        self.table.horizontalHeader().setSectionResizeMode(5, QHeaderView.Stretch)

    @staticmethod
    def _category_state(category: str) -> str:
        return {
            "Computer Work": "info",
            "Fridge": "success",
            "Labelling": "warning",
        }.get(category, "muted")

    def _build_action_cell(self, sale_id: int, is_admin: bool) -> QWidget:
        cell = QWidget()
        row = QHBoxLayout(cell)
        row.setContentsMargins(4, 0, 4, 0)
        row.setSpacing(4)

        view_btn = QPushButton("View")
        view_btn.setObjectName("GhostButton")
        view_btn.clicked.connect(lambda: self._on_view(sale_id))
        row.addWidget(view_btn)

        if is_admin:
            del_btn = QPushButton("Delete")
            del_btn.setObjectName("GhostButton")
            del_btn.setStyleSheet("color:#DC2626; font-weight:600;")
            del_btn.clicked.connect(lambda: self._on_delete(sale_id))
            row.addWidget(del_btn)
        row.addStretch(1)
        return cell

    # ------------------------------------------------------------------
    # Actions
    # ------------------------------------------------------------------

    def _on_add_sale(self) -> None:
        dlg = SaleDialog(parent=self)
        if dlg.exec() != SaleDialog.Accepted:
            return
        sale_id = dlg.created_sale_id
        if sale_id:
            log_action(AUDIT_CREATE, module="sales", description=f"Created sale #{sale_id}")
            toast(self.window(), "Sale recorded", level="success")
            self.refresh()

    def _on_view(self, sale_id: int) -> None:
        sale = sales_service.get_sale_detail(sale_id)
        if sale is None:
            error(self, "Not found", "This sale could not be loaded.")
            return
        lines = [
            f"Sale #{sale.id}",
            f"Date: {format_datetime(sale.sale_date)}",
            f"Category: {sale.category}",
            f"Payment: {sale.payment_method}",
            "",
            "Items:",
        ]
        for it in sale.items:
            qty_text = str(int(it.quantity)) if it.quantity == int(it.quantity) else f"{it.quantity:.2f}"
            lines.append(f"  {qty_text} \u00d7 {it.description}  -  {format_money(it.line_total)}")
        lines.append("")
        lines.append(f"Total: {format_money(sale.total_amount)}")
        if sale.notes:
            lines.append("")
            lines.append(f"Notes: {sale.notes}")
        info(self, f"Sale #{sale.id}", "\n".join(lines))

    def _on_delete(self, sale_id: int) -> None:
        if not confirm(
            self,
            "Delete sale",
            f"Delete sale #{sale_id}? Fridge stock for this sale will be restored.",
            destructive=True,
        ):
            return
        try:
            sales_service.delete_sale(sale_id)
        except Exception as exc:
            error(self, "Could not delete", str(exc))
            return
        log_action(AUDIT_DELETE, module="sales", description=f"Deleted sale #{sale_id}")
        toast(self.window(), "Sale deleted", level="info")
        self.refresh()

    def _on_export(self) -> None:
        if not self._sales:
            info(self, "Nothing to export", "There are no sales in the current view.")
            return
        path, _ = QFileDialog.getSaveFileName(
            self,
            "Export sales",
            f"sales_{date.today().isoformat()}.csv",
            "CSV files (*.csv)",
        )
        if not path:
            return
        try:
            with open(path, "w", newline="", encoding="utf-8") as fh:
                writer = csv.writer(fh)
                writer.writerow(["ID", "Date", "Category", "Items", "Payment", "Total (UGX)", "Notes"])
                for s in self._sales:
                    writer.writerow([
                        s.id,
                        format_datetime(s.sale_date),
                        s.category,
                        s.item_summary,
                        s.payment_method,
                        int(round(s.total_amount)),
                        s.notes or "",
                    ])
        except OSError as exc:
            error(self, "Export failed", str(exc))
            return
        toast(self.window(), "Sales exported", level="success")


def _wrap_center(widget: QWidget) -> QWidget:
    w = QWidget()
    row = QHBoxLayout(w)
    row.setContentsMargins(6, 0, 6, 0)
    row.addWidget(widget)
    row.addStretch(1)
    return w
