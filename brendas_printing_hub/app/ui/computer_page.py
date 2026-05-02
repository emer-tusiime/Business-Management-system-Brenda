"""Computer Services page - filtered view of sales for computer service categories."""
from __future__ import annotations
from datetime import date, timedelta
from decimal import Decimal
from PySide6.QtCore import Qt
from PySide6.QtGui import QColor
from PySide6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QLabel, QPushButton, QLineEdit,
    QComboBox, QDateEdit, QTableWidgetItem, QHeaderView, QMessageBox, QFrame
)

from app.core.auth import CurrentUser, has_permission
from app.core.utils import format_money, format_datetime
from app.database.models import ServiceCategory, SaleStatus
from app.database.session import session_scope
from app.services.sales_service import SalesService
from app.services.dashboard_service import DashboardService
from app.ui.components.cards import KpiCard
from app.ui.components.tables import StyledTable, set_column_widths
from app.ui.sales_dialog import SaleDialog


COMPUTER_CATEGORIES = [
    ServiceCategory.PRINTING.value,
    ServiceCategory.PHOTOCOPYING.value,
    ServiceCategory.SCANNING.value,
    ServiceCategory.LAMINATION.value,
    ServiceCategory.TYPING.value,
    ServiceCategory.INTERNET.value,
    ServiceCategory.OTHER.value,
]


class ComputerPage(QWidget):
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
        title = QLabel("Computer Services")
        title.setObjectName("PageTitle")
        subtitle = QLabel("Printing, photocopying, scanning, lamination & more")
        subtitle.setObjectName("PageSubtitle")
        title_box = QVBoxLayout()
        title_box.setSpacing(2)
        title_box.addWidget(title)
        title_box.addWidget(subtitle)
        header.addLayout(title_box)
        header.addStretch()

        if has_permission("sales.create"):
            new_btn = QPushButton("+ New Service Sale")
            new_btn.setObjectName("PrimaryButton")
            new_btn.setMinimumHeight(38)
            new_btn.clicked.connect(self._on_new_sale)
            header.addWidget(new_btn)
        root.addLayout(header)

        # KPI cards
        cards = QHBoxLayout()
        cards.setSpacing(16)
        self.card_today = KpiCard("Today's Revenue", "$0.00", subtitle="Computer services")
        self.card_week = KpiCard("This Week", "$0.00", subtitle="Last 7 days")
        self.card_month = KpiCard("This Month", "$0.00", subtitle="Month to date")
        self.card_count = KpiCard("Transactions", "0", subtitle="Today")
        for c in (self.card_today, self.card_week, self.card_month, self.card_count):
            cards.addWidget(c, 1)
        root.addLayout(cards)

        # Filters
        filters_frame = QFrame()
        filters_frame.setObjectName("Card")
        f_layout = QHBoxLayout(filters_frame)
        f_layout.setContentsMargins(16, 12, 16, 12)
        f_layout.setSpacing(12)

        f_layout.addWidget(QLabel("Search:"))
        self.search_input = QLineEdit()
        self.search_input.setPlaceholderText("Customer or invoice...")
        self.search_input.setMinimumWidth(200)
        self.search_input.textChanged.connect(self.refresh)
        f_layout.addWidget(self.search_input)

        f_layout.addWidget(QLabel("Category:"))
        self.category_combo = QComboBox()
        self.category_combo.addItem("All Computer Services", None)
        for c in COMPUTER_CATEGORIES:
            self.category_combo.addItem(c, c)
        self.category_combo.currentIndexChanged.connect(self.refresh)
        f_layout.addWidget(self.category_combo)

        f_layout.addWidget(QLabel("From:"))
        self.from_date = QDateEdit()
        self.from_date.setCalendarPopup(True)
        self.from_date.setDate(date.today() - timedelta(days=30))
        self.from_date.dateChanged.connect(self.refresh)
        f_layout.addWidget(self.from_date)

        f_layout.addWidget(QLabel("To:"))
        self.to_date = QDateEdit()
        self.to_date.setCalendarPopup(True)
        self.to_date.setDate(date.today())
        self.to_date.dateChanged.connect(self.refresh)
        f_layout.addWidget(self.to_date)

        f_layout.addStretch()
        refresh_btn = QPushButton("Refresh")
        refresh_btn.clicked.connect(self.refresh)
        f_layout.addWidget(refresh_btn)
        root.addWidget(filters_frame)

        # Table
        self.table = StyledTable([
            "Date", "Invoice", "Customer", "Items", "Total", "Status", "Cashier"
        ])
        self.table.cellDoubleClicked.connect(self._on_row_double_click)
        root.addWidget(self.table, 1)

    def refresh(self):
        with session_scope() as session:
            sales_service = SalesService(session)
            dashboard = DashboardService(session)

            # KPIs (computer revenue today/week/month)
            today = date.today()
            week_start = today - timedelta(days=6)
            month_start = today.replace(day=1)

            self.card_today.set_value(format_money(
                self._revenue_for(session, today, today)
            ))
            self.card_week.set_value(format_money(
                self._revenue_for(session, week_start, today)
            ))
            self.card_month.set_value(format_money(
                self._revenue_for(session, month_start, today)
            ))

            # Filters
            search = self.search_input.text().strip()
            cat = self.category_combo.currentData()
            d_from = self.from_date.date().toPython()
            d_to = self.to_date.date().toPython()

            sales = sales_service.list_sales(
                search=search,
                date_from=d_from,
                date_to=d_to,
                category=cat,
                status=None,
                limit=500,
            )
            # Filter only computer category sales when "All Computer Services"
            if cat is None:
                sales = [s for s in sales if any(
                    i.service_category in COMPUTER_CATEGORIES for i in s.items
                )]
            self._populate_table(sales)
            self.card_count.set_value(str(len(sales)))

    def _revenue_for(self, session, d_from: date, d_to: date) -> Decimal:
        from sqlalchemy import select, func, and_
        from app.database.models import Sale, SaleItem
        from datetime import datetime, time as dtime
        start = datetime.combine(d_from, dtime.min)
        end = datetime.combine(d_to, dtime.max)
        total = session.scalar(
            select(func.coalesce(func.sum(SaleItem.subtotal), 0))
            .join(Sale, Sale.id == SaleItem.sale_id)
            .where(and_(
                Sale.status == SaleStatus.COMPLETED.value,
                Sale.sale_date >= start,
                Sale.sale_date <= end,
                SaleItem.service_category.in_(COMPUTER_CATEGORIES),
            ))
        ) or Decimal("0")
        return Decimal(str(total))

    def _populate_table(self, sales):
        self.table.setRowCount(0)
        for sale in sales:
            row = self.table.rowCount()
            self.table.insertRow(row)
            items_summary = ", ".join(
                f"{i.service_name} x{int(i.quantity) if i.quantity == int(i.quantity) else i.quantity}"
                for i in sale.items[:3]
            )
            if len(sale.items) > 3:
                items_summary += f" +{len(sale.items) - 3} more"
            cells = [
                format_datetime(sale.sale_date),
                sale.invoice_number,
                sale.customer_name or "-",
                items_summary,
                format_money(sale.total_amount),
                sale.status,
                sale.cashier.full_name if sale.cashier else "-",
            ]
            for col, value in enumerate(cells):
                item = QTableWidgetItem(str(value))
                if col == 4:
                    item.setTextAlignment(Qt.AlignRight | Qt.AlignVCenter)
                if col == 5 and sale.status == SaleStatus.VOIDED.value:
                    item.setForeground(QColor("#dc2626"))
                item.setData(Qt.UserRole, sale.id)
                self.table.setItem(row, col, item)
        set_column_widths(self.table, [140, 130, 180, None, 110, 100, 140])

    # Actions
    def _on_new_sale(self):
        dlg = SaleDialog(self, default_category_filter=COMPUTER_CATEGORIES)
        if dlg.exec():
            self.refresh()

    def _on_row_double_click(self, row, _col):
        item = self.table.item(row, 0)
        if not item:
            return
        sale_id = item.data(Qt.UserRole)
        # Show details dialog
        with session_scope() as session:
            sale = SalesService(session).get_sale(sale_id)
            if not sale:
                return
            lines = [
                f"Invoice: {sale.invoice_number}",
                f"Date: {format_datetime(sale.sale_date)}",
                f"Customer: {sale.customer_name or '-'}",
                f"Status: {sale.status}",
                f"Payment: {sale.payment_method}",
                "",
                "Items:",
            ]
            for it in sale.items:
                lines.append(
                    f"  - {it.service_name} ({it.service_category}) x"
                    f"{it.quantity} @ {format_money(it.unit_price)} = {format_money(it.subtotal)}"
                )
            lines.append("")
            lines.append(f"Total: {format_money(sale.total_amount)}")
            QMessageBox.information(self, f"Sale {sale.invoice_number}", "\n".join(lines))
