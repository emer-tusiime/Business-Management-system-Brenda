"""
Add Sale dialog. Supports all three categories: Computer Work, Fridge,
Labelling. The line-item picker swaps automatically based on the selected
category.
"""
from __future__ import annotations

from datetime import datetime
from typing import List, Optional

from PySide6.QtCore import Qt, QDateTime
from PySide6.QtWidgets import (
    QComboBox,
    QDateTimeEdit,
    QDialog,
    QDoubleSpinBox,
    QHBoxLayout,
    QHeaderView,
    QLabel,
    QLineEdit,
    QMessageBox,
    QPushButton,
    QSpinBox,
    QTableWidget,
    QTableWidgetItem,
    QTextEdit,
    QVBoxLayout,
    QWidget,
)

from app.core.auth import current_user
from app.core.constants import (
    PAYMENT_CASH,
    PAYMENT_METHODS,
    SALE_CATEGORIES,
    SALE_CATEGORY_COMPUTER,
    SALE_CATEGORY_FRIDGE,
    SALE_CATEGORY_LABELLING,
)
from app.core.utils import format_money
from app.services import sales_service
from app.services.service_catalog import list_services
from app.services.stock_service import list_products
from app.ui.components.tables import configure_table, make_item, make_money_item


class SaleDialog(QDialog):
    """Dialog for creating a new sale."""

    def __init__(self, parent: QWidget | None = None) -> None:
        super().__init__(parent)
        self.setWindowTitle("Record Sale")
        self.setModal(True)
        self.resize(720, 600)
        self.created_sale_id: Optional[int] = None

        self._items: List[sales_service.SaleItemInput] = []
        self._build_ui()
        self._reload_picker()

    # ------------------------------------------------------------------
    # UI
    # ------------------------------------------------------------------

    def _build_ui(self) -> None:
        layout = QVBoxLayout(self)
        layout.setContentsMargins(20, 18, 20, 18)
        layout.setSpacing(10)

        title = QLabel("Record a new sale")
        title.setStyleSheet("font-size:16px; font-weight:700; color:#0F172A;")
        subtitle = QLabel("Choose a category, add line items, then save.")
        subtitle.setStyleSheet("color:#64748B; font-size:12px;")
        layout.addWidget(title)
        layout.addWidget(subtitle)

        # Top form: category, payment, date
        top = QHBoxLayout()
        top.setSpacing(10)

        self.category_combo = QComboBox()
        for cat in SALE_CATEGORIES:
            self.category_combo.addItem(cat, cat)
        self.category_combo.currentIndexChanged.connect(self._on_category_changed)

        self.payment_combo = QComboBox()
        for pm in PAYMENT_METHODS:
            self.payment_combo.addItem(pm, pm)
        self.payment_combo.setCurrentText(PAYMENT_CASH)

        self.datetime_edit = QDateTimeEdit()
        self.datetime_edit.setCalendarPopup(True)
        self.datetime_edit.setDateTime(QDateTime.currentDateTime())
        self.datetime_edit.setDisplayFormat("dd MMM yyyy HH:mm")

        for label, widget in [
            ("Category", self.category_combo),
            ("Payment", self.payment_combo),
            ("Date / Time", self.datetime_edit),
        ]:
            col = QVBoxLayout()
            col.setSpacing(4)
            lbl = QLabel(label)
            lbl.setStyleSheet("color:#64748B; font-size:11px; font-weight:600;")
            col.addWidget(lbl)
            col.addWidget(widget)
            top.addLayout(col)
        layout.addLayout(top)

        # Item picker row
        picker_box = QHBoxLayout()
        picker_box.setSpacing(8)

        self.picker_label = QLabel("Item")
        self.picker_label.setStyleSheet("color:#64748B; font-size:11px; font-weight:600;")

        self.item_combo = QComboBox()
        self.item_combo.setMinimumWidth(220)
        self.item_combo.currentIndexChanged.connect(self._on_picker_item_changed)

        self.description_input = QLineEdit()
        self.description_input.setPlaceholderText("Description (auto-filled)")

        self.qty_spin = QDoubleSpinBox()
        self.qty_spin.setDecimals(2)
        self.qty_spin.setMinimum(0.01)
        self.qty_spin.setMaximum(100000)
        self.qty_spin.setValue(1)

        self.price_spin = QDoubleSpinBox()
        self.price_spin.setDecimals(0)
        self.price_spin.setMinimum(0)
        self.price_spin.setMaximum(100000000)
        self.price_spin.setSingleStep(100)

        self.add_line_btn = QPushButton("Add line")
        self.add_line_btn.setObjectName("PrimaryButton")
        self.add_line_btn.clicked.connect(self._on_add_line)

        for label, widget in [
            ("Item", self.item_combo),
            ("Description", self.description_input),
            ("Qty", self.qty_spin),
            ("Unit price (UGX)", self.price_spin),
        ]:
            col = QVBoxLayout()
            col.setSpacing(4)
            lbl = QLabel(label)
            lbl.setStyleSheet("color:#64748B; font-size:11px; font-weight:600;")
            col.addWidget(lbl)
            col.addWidget(widget)
            picker_box.addLayout(col, stretch=1 if label == "Description" else 0)
        picker_box.addWidget(self.add_line_btn)
        layout.addLayout(picker_box)

        # Items table
        self.items_table = QTableWidget(0, 5)
        configure_table(
            self.items_table,
            ["Description", "Qty", "Unit price", "Line total", ""],
            stretch_last=False,
        )
        self.items_table.horizontalHeader().setSectionResizeMode(0, QHeaderView.Stretch)
        self.items_table.setMinimumHeight(160)
        layout.addWidget(self.items_table, stretch=1)

        # Notes
        notes_label = QLabel("Notes (optional)")
        notes_label.setStyleSheet("color:#64748B; font-size:11px; font-weight:600;")
        self.notes_input = QTextEdit()
        self.notes_input.setMaximumHeight(70)
        layout.addWidget(notes_label)
        layout.addWidget(self.notes_input)

        # Footer with total + save
        footer = QHBoxLayout()
        self.total_label = QLabel("Total: UGX 0")
        self.total_label.setStyleSheet("font-size:16px; font-weight:700; color:#0F172A;")
        footer.addWidget(self.total_label)
        footer.addStretch(1)

        cancel_btn = QPushButton("Cancel")
        cancel_btn.clicked.connect(self.reject)
        save_btn = QPushButton("Save sale")
        save_btn.setObjectName("PrimaryButton")
        save_btn.clicked.connect(self._on_save)
        footer.addWidget(cancel_btn)
        footer.addWidget(save_btn)
        layout.addLayout(footer)

    # ------------------------------------------------------------------
    # Picker (services / products / labelling)
    # ------------------------------------------------------------------

    def _on_category_changed(self) -> None:
        self._reload_picker()

    def _reload_picker(self) -> None:
        cat = self.category_combo.currentData()
        self.item_combo.blockSignals(True)
        self.item_combo.clear()

        if cat == SALE_CATEGORY_COMPUTER:
            self.item_combo.addItem("- Choose a service -", None)
            for svc in list_services(active_only=True):
                payload = {
                    "type": "service",
                    "id": svc.id,
                    "description": svc.name,
                    "price": float(svc.default_price or 0),
                }
                self.item_combo.addItem(f"{svc.name}  ({format_money(svc.default_price)})", payload)
            self.picker_label.setText("Service")

        elif cat == SALE_CATEGORY_FRIDGE:
            self.item_combo.addItem("- Choose a product -", None)
            for p in list_products(active_only=True):
                stock_label = f"in stock: {p.current_stock}" if p.current_stock else "OUT OF STOCK"
                payload = {
                    "type": "product",
                    "id": p.id,
                    "description": f"{p.name}" + (f" - {p.brand}" if p.brand else ""),
                    "price": float(p.selling_price or 0),
                    "available_stock": int(p.current_stock),
                }
                label = f"{p.name}" + (f" ({p.brand})" if p.brand else "")
                self.item_combo.addItem(
                    f"{label}  -  {format_money(p.selling_price)}  [{stock_label}]",
                    payload,
                )
            self.picker_label.setText("Product")

        else:  # Labelling - free-form line items
            self.item_combo.addItem("Custom labelling line", {
                "type": "free", "id": None, "description": "", "price": 0.0,
            })
            self.picker_label.setText("Item")

        self.item_combo.blockSignals(False)
        self._on_picker_item_changed()

    def _on_picker_item_changed(self) -> None:
        payload = self.item_combo.currentData()
        if not payload:
            self.description_input.setText("")
            self.price_spin.setValue(0)
            return
        self.description_input.setText(payload.get("description") or "")
        self.price_spin.setValue(float(payload.get("price") or 0))

    # ------------------------------------------------------------------
    # Item lines
    # ------------------------------------------------------------------

    def _on_add_line(self) -> None:
        payload = self.item_combo.currentData() or {}
        description = self.description_input.text().strip() or payload.get("description") or ""
        if not description:
            QMessageBox.warning(self, "Description required", "Please enter a description for this line.")
            return
        qty = float(self.qty_spin.value())
        price = float(self.price_spin.value())
        if qty <= 0 or price < 0:
            QMessageBox.warning(self, "Invalid amounts", "Quantity must be > 0 and price >= 0.")
            return

        # Stock guard for fridge
        if (
            self.category_combo.currentData() == SALE_CATEGORY_FRIDGE
            and payload.get("type") == "product"
        ):
            available = int(payload.get("available_stock") or 0)
            existing_for_product = sum(
                it.quantity for it in self._items if it.product_id == payload.get("id")
            )
            if existing_for_product + qty > available:
                resp = QMessageBox.question(
                    self,
                    "Stock alert",
                    f"Only {available} unit(s) of '{description}' in stock. "
                    f"Continue anyway? (Admin override)",
                    QMessageBox.Yes | QMessageBox.No,
                )
                if resp != QMessageBox.Yes:
                    return

        item = sales_service.SaleItemInput(
            description=description,
            quantity=qty,
            unit_price=price,
            product_id=payload.get("id") if payload.get("type") == "product" else None,
            service_id=payload.get("id") if payload.get("type") == "service" else None,
        )
        self._items.append(item)
        self._refresh_items_table()

        # Reset for next entry
        self.qty_spin.setValue(1)

    def _refresh_items_table(self) -> None:
        self.items_table.setRowCount(0)
        total = 0.0
        for idx, it in enumerate(self._items):
            row = self.items_table.rowCount()
            self.items_table.insertRow(row)
            self.items_table.setItem(row, 0, make_item(it.description))
            qty_text = str(int(it.quantity)) if it.quantity == int(it.quantity) else f"{it.quantity:.2f}"
            self.items_table.setItem(row, 1, make_item(qty_text, align=Qt.AlignRight | Qt.AlignVCenter))
            self.items_table.setItem(row, 2, make_money_item(it.unit_price))
            self.items_table.setItem(row, 3, make_money_item(it.line_total, bold=True))
            remove_btn = QPushButton("Remove")
            remove_btn.setObjectName("GhostButton")
            remove_btn.setStyleSheet("color:#DC2626; font-weight:600;")
            remove_btn.clicked.connect(lambda _=False, i=idx: self._on_remove_line(i))
            self.items_table.setCellWidget(row, 4, remove_btn)
            total += it.line_total
        self.total_label.setText(f"Total: {format_money(total)}")

    def _on_remove_line(self, index: int) -> None:
        if 0 <= index < len(self._items):
            del self._items[index]
            self._refresh_items_table()

    # ------------------------------------------------------------------
    # Save
    # ------------------------------------------------------------------

    def _on_save(self) -> None:
        if not self._items:
            QMessageBox.warning(self, "No items", "Add at least one item to the sale.")
            return

        user = current_user()
        is_admin = user is not None and user.is_admin

        qd = self.datetime_edit.dateTime()
        sale_dt = qd.toPython() if hasattr(qd, "toPython") else datetime(
            qd.date().year(), qd.date().month(), qd.date().day(),
            qd.time().hour(), qd.time().minute(),
        )

        payload = sales_service.SaleInput(
            category=self.category_combo.currentData(),
            payment_method=self.payment_combo.currentData(),
            items=list(self._items),
            sale_date=sale_dt,
            notes=self.notes_input.toPlainText().strip(),
            recorded_by=user.id if user else None,
            allow_stock_override=is_admin,
        )
        try:
            sale_id = sales_service.create_sale(payload)
        except Exception as exc:
            QMessageBox.critical(self, "Could not save sale", str(exc))
            return

        self.created_sale_id = sale_id
        self.accept()
