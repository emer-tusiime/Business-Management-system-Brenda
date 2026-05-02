"""Fridge stock management with movement history."""
from __future__ import annotations
from decimal import Decimal
from PySide6.QtCore import Qt
from PySide6.QtGui import QColor
from PySide6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QLabel, QPushButton, QLineEdit,
    QComboBox, QTableWidgetItem, QMessageBox, QFrame, QTabWidget,
    QDialog, QDialogButtonBox, QFormLayout, QDoubleSpinBox, QTextEdit,
    QCheckBox
)

from app.core.auth import has_permission, AuthError
from app.core.utils import format_money, format_datetime
from app.database.models import StockMovementType
from app.database.session import session_scope
from app.services.stock_service import StockService, StockError
from app.ui.components.tables import StyledTable, set_column_widths
from app.ui.components.dialogs import ConfirmDialog


class ProductDialog(QDialog):
    """Add or edit a fridge product."""

    def __init__(self, parent=None, product=None):
        super().__init__(parent)
        self.product = product
        self.setWindowTitle("Edit Product" if product else "Add Product")
        self.setMinimumWidth(420)
        self.setObjectName("DialogWindow")

        layout = QVBoxLayout(self)
        layout.setContentsMargins(24, 24, 24, 24)
        layout.setSpacing(16)

        title = QLabel("Edit Product" if product else "Add New Product")
        title.setObjectName("DialogTitle")
        layout.addWidget(title)

        form = QFormLayout()
        form.setSpacing(10)

        self.name_input = QLineEdit(product.name if product else "")
        form.addRow("Name *", self.name_input)

        self.unit_input = QLineEdit(product.unit if product else "piece")
        form.addRow("Unit", self.unit_input)

        self.price_input = QDoubleSpinBox()
        self.price_input.setMaximum(1_000_000)
        self.price_input.setDecimals(2)
        self.price_input.setPrefix("$ ")
        self.price_input.setValue(float(product.unit_price) if product else 0)
        form.addRow("Selling Price *", self.price_input)

        self.cost_input = QDoubleSpinBox()
        self.cost_input.setMaximum(1_000_000)
        self.cost_input.setDecimals(2)
        self.cost_input.setPrefix("$ ")
        self.cost_input.setValue(float(product.cost_price) if product else 0)
        form.addRow("Cost Price", self.cost_input)

        self.threshold_input = QDoubleSpinBox()
        self.threshold_input.setMaximum(100_000)
        self.threshold_input.setDecimals(2)
        self.threshold_input.setValue(float(product.low_stock_threshold) if product else 5)
        form.addRow("Low-Stock Alert", self.threshold_input)

        self.active_check = QCheckBox("Active (visible in sales)")
        self.active_check.setChecked(product.is_active if product else True)
        form.addRow("", self.active_check)

        layout.addLayout(form)

        if not product:
            self.opening_input = QDoubleSpinBox()
            self.opening_input.setMaximum(1_000_000)
            self.opening_input.setDecimals(2)
            self.opening_input.setValue(0)
            form.addRow("Opening Stock", self.opening_input)
        else:
            self.opening_input = None

        buttons = QDialogButtonBox(QDialogButtonBox.Ok | QDialogButtonBox.Cancel)
        buttons.accepted.connect(self.accept)
        buttons.rejected.connect(self.reject)
        layout.addWidget(buttons)

    def get_data(self):
        return {
            "name": self.name_input.text().strip(),
            "unit": self.unit_input.text().strip() or "piece",
            "unit_price": Decimal(str(self.price_input.value())),
            "cost_price": Decimal(str(self.cost_input.value())),
            "low_stock_threshold": Decimal(str(self.threshold_input.value())),
            "is_active": self.active_check.isChecked(),
            "opening_stock": (
                Decimal(str(self.opening_input.value()))
                if self.opening_input is not None else Decimal("0")
            ),
        }


class StockAdjustmentDialog(QDialog):
    """Add stock (purchase/restock) or adjust stock."""

    def __init__(self, parent=None, products=None, mode="purchase"):
        super().__init__(parent)
        self.products = products or []
        self.mode = mode
        self.setWindowTitle("Add Stock" if mode == "purchase" else "Adjust Stock")
        self.setMinimumWidth(440)
        self.setObjectName("DialogWindow")

        layout = QVBoxLayout(self)
        layout.setContentsMargins(24, 24, 24, 24)
        layout.setSpacing(16)

        title = QLabel("Restock Product" if mode == "purchase" else "Manual Stock Adjustment")
        title.setObjectName("DialogTitle")
        subtitle = QLabel(
            "Record a purchase / new delivery into stock."
            if mode == "purchase"
            else "Use for spoilage, damage, or count corrections."
        )
        subtitle.setObjectName("DialogSubtitle")
        layout.addWidget(title)
        layout.addWidget(subtitle)

        form = QFormLayout()
        form.setSpacing(10)

        self.product_combo = QComboBox()
        for p in self.products:
            self.product_combo.addItem(
                f"{p.name} (current: {p.stock_quantity} {p.unit})", p.id
            )
        form.addRow("Product *", self.product_combo)

        self.qty_input = QDoubleSpinBox()
        self.qty_input.setMinimum(-100_000)
        self.qty_input.setMaximum(100_000)
        self.qty_input.setDecimals(2)
        self.qty_input.setValue(1)
        if mode == "purchase":
            self.qty_input.setMinimum(0.01)
            form.addRow("Quantity Added *", self.qty_input)
        else:
            form.addRow("Quantity (+/-)", self.qty_input)

        if mode == "purchase":
            self.cost_input = QDoubleSpinBox()
            self.cost_input.setMaximum(1_000_000)
            self.cost_input.setDecimals(2)
            self.cost_input.setPrefix("$ ")
            form.addRow("Total Cost (optional)", self.cost_input)
        else:
            self.cost_input = None

        self.notes_input = QTextEdit()
        self.notes_input.setMaximumHeight(80)
        form.addRow("Notes", self.notes_input)
        layout.addLayout(form)

        buttons = QDialogButtonBox(QDialogButtonBox.Ok | QDialogButtonBox.Cancel)
        buttons.accepted.connect(self.accept)
        buttons.rejected.connect(self.reject)
        layout.addWidget(buttons)

    def get_data(self):
        return {
            "product_id": self.product_combo.currentData(),
            "quantity": Decimal(str(self.qty_input.value())),
            "cost": Decimal(str(self.cost_input.value())) if self.cost_input else None,
            "notes": self.notes_input.toPlainText().strip(),
        }


class FridgePage(QWidget):
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
        title = QLabel("Fridge Stock")
        title.setObjectName("PageTitle")
        subtitle = QLabel("Manage drinks & snacks inventory")
        subtitle.setObjectName("PageSubtitle")
        title_box = QVBoxLayout()
        title_box.setSpacing(2)
        title_box.addWidget(title)
        title_box.addWidget(subtitle)
        header.addLayout(title_box)
        header.addStretch()

        if has_permission("stock.manage"):
            self.add_product_btn = QPushButton("+ Add Product")
            self.add_product_btn.clicked.connect(self._on_add_product)
            header.addWidget(self.add_product_btn)

            self.restock_btn = QPushButton("+ Add Stock")
            self.restock_btn.setObjectName("PrimaryButton")
            self.restock_btn.clicked.connect(lambda: self._on_movement("purchase"))
            header.addWidget(self.restock_btn)

            self.adjust_btn = QPushButton("Adjust Stock")
            self.adjust_btn.clicked.connect(lambda: self._on_movement("adjustment"))
            header.addWidget(self.adjust_btn)
        root.addLayout(header)

        # Tabs
        self.tabs = QTabWidget()
        self.tabs.setObjectName("Tabs")
        root.addWidget(self.tabs, 1)

        # Products tab
        products_widget = QWidget()
        p_layout = QVBoxLayout(products_widget)
        p_layout.setContentsMargins(0, 8, 0, 0)

        search_row = QHBoxLayout()
        search_row.addWidget(QLabel("Search:"))
        self.search_input = QLineEdit()
        self.search_input.setPlaceholderText("Product name...")
        self.search_input.setMaximumWidth(280)
        self.search_input.textChanged.connect(self.refresh)
        search_row.addWidget(self.search_input)
        self.show_inactive = QCheckBox("Show inactive")
        self.show_inactive.toggled.connect(self.refresh)
        search_row.addWidget(self.show_inactive)
        search_row.addStretch()
        p_layout.addLayout(search_row)

        self.products_table = StyledTable([
            "Name", "Unit", "Stock", "Selling", "Cost", "Margin", "Threshold", "Status"
        ])
        self.products_table.cellDoubleClicked.connect(self._on_edit_product)
        p_layout.addWidget(self.products_table)
        self.tabs.addTab(products_widget, "Products")

        # Movements tab
        movements_widget = QWidget()
        m_layout = QVBoxLayout(movements_widget)
        m_layout.setContentsMargins(0, 8, 0, 0)
        self.movements_table = StyledTable([
            "Date", "Product", "Type", "Quantity", "Reason", "User"
        ])
        m_layout.addWidget(self.movements_table)
        self.tabs.addTab(movements_widget, "Movement History")

    def refresh(self):
        with session_scope() as session:
            stock_service = StockService(session)
            search = self.search_input.text().strip()
            include_inactive = self.show_inactive.isChecked()
            products = stock_service.list_products(
                search=search, include_inactive=include_inactive
            )
            self._populate_products(products)
            movements = stock_service.list_recent_movements(limit=200)
            self._populate_movements(movements)

    def _populate_products(self, products):
        self.products_table.setRowCount(0)
        for p in products:
            row = self.products_table.rowCount()
            self.products_table.insertRow(row)
            margin = Decimal("0")
            if p.unit_price > 0:
                margin = ((p.unit_price - p.cost_price) / p.unit_price) * 100
            cells = [
                p.name,
                p.unit,
                f"{p.stock_quantity:.2f}",
                format_money(p.unit_price),
                format_money(p.cost_price),
                f"{margin:.1f}%",
                f"{p.low_stock_threshold:.2f}",
                "Active" if p.is_active else "Inactive",
            ]
            low = p.stock_quantity <= p.low_stock_threshold
            for col, value in enumerate(cells):
                item = QTableWidgetItem(str(value))
                if col in (3, 4, 6):
                    item.setTextAlignment(Qt.AlignRight | Qt.AlignVCenter)
                if col == 2:
                    item.setTextAlignment(Qt.AlignRight | Qt.AlignVCenter)
                    if low and p.is_active:
                        item.setForeground(QColor("#dc2626"))
                if col == 7 and not p.is_active:
                    item.setForeground(QColor("#94a3b8"))
                item.setData(Qt.UserRole, p.id)
                self.products_table.setItem(row, col, item)
        set_column_widths(self.products_table, [None, 80, 100, 110, 110, 90, 110, 90])

    def _populate_movements(self, movements):
        self.movements_table.setRowCount(0)
        for m in movements:
            row = self.movements_table.rowCount()
            self.movements_table.insertRow(row)
            qty_str = f"{m.quantity_change:+.2f}"
            type_label = m.movement_type.replace("_", " ").title()
            cells = [
                format_datetime(m.created_at),
                m.product.name if m.product else "-",
                type_label,
                qty_str,
                m.reason or "-",
                m.user.full_name if m.user else "-",
            ]
            for col, value in enumerate(cells):
                item = QTableWidgetItem(str(value))
                if col == 3:
                    item.setTextAlignment(Qt.AlignRight | Qt.AlignVCenter)
                    if m.quantity_change > 0:
                        item.setForeground(QColor("#16a34a"))
                    elif m.quantity_change < 0:
                        item.setForeground(QColor("#dc2626"))
                self.movements_table.setItem(row, col, item)
        set_column_widths(self.movements_table, [140, None, 130, 100, None, 140])

    # ----- Actions -----
    def _on_add_product(self):
        if not has_permission("stock.manage"):
            QMessageBox.warning(self, "Access denied", "You cannot add products.")
            return
        dlg = ProductDialog(self)
        if dlg.exec():
            data = dlg.get_data()
            try:
                with session_scope() as session:
                    StockService(session).create_product(**data)
                self.refresh()
            except (StockError, AuthError) as e:
                QMessageBox.warning(self, "Cannot create product", str(e))

    def _on_edit_product(self, row, _col):
        item = self.products_table.item(row, 0)
        if not item:
            return
        product_id = item.data(Qt.UserRole)
        with session_scope() as session:
            stock_service = StockService(session)
            product = stock_service.get_product(product_id)
            if not product:
                return
            session.expunge(product)

        dlg = ProductDialog(self, product=product)
        if not dlg.exec():
            return
        data = dlg.get_data()
        try:
            with session_scope() as session:
                StockService(session).update_product(
                    product_id,
                    name=data["name"],
                    unit=data["unit"],
                    unit_price=data["unit_price"],
                    cost_price=data["cost_price"],
                    low_stock_threshold=data["low_stock_threshold"],
                    is_active=data["is_active"],
                )
            self.refresh()
        except (StockError, AuthError) as e:
            QMessageBox.warning(self, "Cannot update product", str(e))

    def _on_movement(self, mode: str):
        if not has_permission("stock.manage"):
            QMessageBox.warning(self, "Access denied", "You cannot adjust stock.")
            return
        with session_scope() as session:
            products = StockService(session).list_products(include_inactive=False)
            for p in products:
                session.expunge(p)
        if not products:
            QMessageBox.information(self, "No products", "Add a product first.")
            return

        dlg = StockAdjustmentDialog(self, products=products, mode=mode)
        if not dlg.exec():
            return
        data = dlg.get_data()
        if data["quantity"] == 0:
            QMessageBox.warning(self, "Invalid", "Quantity must be non-zero.")
            return
        try:
            with session_scope() as session:
                stock = StockService(session)
                if mode == "purchase":
                    stock.add_purchase(
                        data["product_id"],
                        quantity=data["quantity"],
                        total_cost=data["cost"],
                        notes=data["notes"],
                    )
                else:
                    if not data["notes"]:
                        raise StockError("A reason is required for manual adjustments.")
                    stock.adjust_stock(
                        data["product_id"],
                        quantity_change=data["quantity"],
                        reason=data["notes"],
                    )
            self.refresh()
        except (StockError, AuthError) as e:
            QMessageBox.warning(self, "Cannot record movement", str(e))
