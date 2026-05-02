"""
Dashboard page - the home screen after login.
"""
from __future__ import annotations

from PySide6.QtCore import Qt
from PySide6.QtWidgets import (
    QGridLayout,
    QHBoxLayout,
    QLabel,
    QPushButton,
    QScrollArea,
    QSizePolicy,
    QTableWidget,
    QTableWidgetItem,
    QVBoxLayout,
    QWidget,
)

from app.core.utils import format_money
from app.services.dashboard_service import get_dashboard_snapshot
from app.ui.components.cards import Panel, StatCard
from app.ui.components.charts import (
    make_bar_chart,
    make_grouped_bar_chart,
    make_line_chart,
    make_pie_chart,
)
from app.ui.components.tables import configure_table, make_item, make_money_item


class DashboardPage(QWidget):
    def __init__(self, parent: QWidget | None = None) -> None:
        super().__init__(parent)

        outer = QVBoxLayout(self)
        outer.setContentsMargins(0, 0, 0, 0)

        scroll = QScrollArea()
        scroll.setWidgetResizable(True)
        scroll.setFrameShape(QScrollArea.NoFrame)
        outer.addWidget(scroll)

        body = QWidget()
        scroll.setWidget(body)
        self._layout = QVBoxLayout(body)
        self._layout.setContentsMargins(28, 24, 28, 28)
        self._layout.setSpacing(18)

        self._build_header()
        self._build_kpi_grid()
        self._build_charts_row()
        self._build_secondary_row()

        self.refresh()

    # -- Header -----------------------------------------------------------

    def _build_header(self) -> None:
        head = QHBoxLayout()
        head.setSpacing(8)
        title = QLabel("Overview")
        title.setStyleSheet("font-size:20px; font-weight:700; color:#0F172A;")
        subtitle = QLabel("Snapshot of today\u2019s business performance.")
        subtitle.setStyleSheet("color:#64748B; font-size:12px;")
        col = QVBoxLayout()
        col.setSpacing(2)
        col.addWidget(title)
        col.addWidget(subtitle)
        head.addLayout(col)
        head.addStretch(1)

        self.refresh_btn = QPushButton("Refresh")
        self.refresh_btn.setObjectName("PrimaryButton")
        self.refresh_btn.clicked.connect(self.refresh)
        head.addWidget(self.refresh_btn)

        self._layout.addLayout(head)

    # -- KPI cards --------------------------------------------------------

    def _build_kpi_grid(self) -> None:
        grid = QGridLayout()
        grid.setSpacing(14)

        self.card_today = StatCard("Today's Sales", accent="#2563EB")
        self.card_computer = StatCard("Computer Work", accent="#0EA5E9")
        self.card_fridge = StatCard("Fridge / Drinks", accent="#16A34A")
        self.card_labelling = StatCard("Labelling", accent="#D97706")
        self.card_expenses = StatCard("Today's Expenses", accent="#DC2626")
        self.card_withdrawals = StatCard("Owner Withdrawals", accent="#7C3AED")
        self.card_profit = StatCard("Today's Net Profit", accent="#16A34A")
        self.card_cash = StatCard("Expected Cash", accent="#0F766E")
        self.card_month_sales = StatCard("Monthly Sales", accent="#2563EB")
        self.card_month_profit = StatCard("Monthly Net Profit", accent="#16A34A")
        self.card_low_stock = StatCard("Low Stock Items", accent="#DC2626")
        self.card_pending_jobs = StatCard("Pending Labelling Jobs", accent="#D97706")

        cards = [
            self.card_today, self.card_computer, self.card_fridge, self.card_labelling,
            self.card_expenses, self.card_withdrawals, self.card_profit, self.card_cash,
            self.card_month_sales, self.card_month_profit, self.card_low_stock,
            self.card_pending_jobs,
        ]
        for i, card in enumerate(cards):
            grid.addWidget(card, i // 4, i % 4)
        for c in range(4):
            grid.setColumnStretch(c, 1)

        self._layout.addLayout(grid)

    # -- Charts -----------------------------------------------------------

    def _build_charts_row(self) -> None:
        row = QHBoxLayout()
        row.setSpacing(14)

        self.panel_pie = Panel("Sales by category", "Today's split across the three streams")
        self.panel_pie.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        self._pie_view = None

        self.panel_trend = Panel("Monthly revenue trend", "Last 6 months")
        self.panel_trend.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        self._trend_view = None

        row.addWidget(self.panel_pie, stretch=2)
        row.addWidget(self.panel_trend, stretch=3)
        self._layout.addLayout(row)

    def _build_secondary_row(self) -> None:
        row = QHBoxLayout()
        row.setSpacing(14)

        # Top selling items table
        self.panel_top = Panel("Top selling items", "Best performers this month")
        self.top_table = QTableWidget(0, 3)
        configure_table(self.top_table, ["Item / Service", "Qty", "Revenue"])
        self.top_table.setMinimumHeight(220)
        self.panel_top.add_widget(self.top_table)

        # Expenses vs withdrawals bar chart
        self.panel_compare = Panel(
            "Expenses vs Withdrawals", "How money flows out this month"
        )
        self._compare_view = None

        row.addWidget(self.panel_top, stretch=3)
        row.addWidget(self.panel_compare, stretch=2)
        self._layout.addLayout(row)

    # -- Data refresh -----------------------------------------------------

    def refresh(self) -> None:
        try:
            snap = get_dashboard_snapshot()
        except Exception as exc:  # pragma: no cover
            print(f"[v0] dashboard load failed: {exc}")
            return

        self.card_today.set_money(snap.today_total)
        self.card_computer.set_money(snap.today_computer)
        self.card_fridge.set_money(snap.today_fridge)
        self.card_labelling.set_money(snap.today_labelling)
        self.card_expenses.set_money(snap.today_expenses)
        self.card_withdrawals.set_money(snap.today_withdrawals)
        self.card_profit.set_money(snap.today_net_profit)
        self.card_profit.set_sublabel(
            "Profit positive" if snap.today_net_profit >= 0 else "Operating at a loss",
            negative=snap.today_net_profit < 0,
        )
        self.card_cash.set_money(snap.expected_cash)
        self.card_month_sales.set_money(snap.month_total)
        self.card_month_profit.set_money(snap.month_net_profit)
        self.card_low_stock.set_value(str(snap.low_stock_count))
        self.card_low_stock.set_sublabel(
            "Restock needed" if snap.low_stock_count else "All stocked",
            negative=snap.low_stock_count > 0,
        )
        self.card_pending_jobs.set_value(str(snap.pending_jobs))
        self.card_pending_jobs.set_sublabel(
            "Awaiting work" if snap.pending_jobs else "Nothing pending",
            negative=False,
        )

        # Pie chart - replace previous instance
        slices = snap.sales_by_category
        # If everything is zero, force at least one slice so the chart isn't empty
        if sum(v for _, v in slices) == 0:
            slices = [("No sales today", 1)]
        new_pie = make_pie_chart("", slices)
        self._swap_chart(self.panel_pie, "_pie_view", new_pie)

        # Monthly trend
        trend_points = snap.monthly_trend
        new_trend = make_line_chart("", trend_points, series_label="Revenue (UGX)")
        self._swap_chart(self.panel_trend, "_trend_view", new_trend)

        # Top items table
        self.top_table.setRowCount(0)
        for desc, qty, rev in snap.top_items:
            row = self.top_table.rowCount()
            self.top_table.insertRow(row)
            self.top_table.setItem(row, 0, make_item(desc))
            qty_text = str(int(qty)) if qty == int(qty) else f"{qty:.1f}"
            self.top_table.setItem(row, 1, make_item(qty_text, align=Qt.AlignRight | Qt.AlignVCenter))
            self.top_table.setItem(row, 2, make_money_item(rev, bold=True))
        if self.top_table.rowCount() == 0:
            self.top_table.insertRow(0)
            empty = QTableWidgetItem("No sales recorded this month yet.")
            empty.setForeground(Qt.gray)
            self.top_table.setSpan(0, 0, 1, 3)
            self.top_table.setItem(0, 0, empty)
        self.top_table.resizeColumnsToContents()
        self.top_table.horizontalHeader().setStretchLastSection(True)

        # Expenses vs withdrawals
        exp, wd = snap.expenses_vs_withdrawals
        new_compare = make_grouped_bar_chart(
            "",
            ["This month"],
            [
                ("Business expenses", [exp]),
                ("Owner withdrawals", [wd]),
            ],
        )
        self._swap_chart(self.panel_compare, "_compare_view", new_compare)

    def _swap_chart(self, panel: Panel, attr: str, new_view) -> None:
        old = getattr(self, attr)
        if old is not None:
            old.setParent(None)
            old.deleteLater()
        panel.add_widget(new_view)
        setattr(self, attr, new_view)
