"""
Helpers for QTableWidget configuration.
"""
from __future__ import annotations

from typing import Sequence

from PySide6.QtCore import Qt
from PySide6.QtGui import QFont
from PySide6.QtWidgets import (
    QAbstractItemView,
    QHeaderView,
    QTableWidget,
    QTableWidgetItem,
)


def configure_table(table: QTableWidget, headers: Sequence[str],
                    stretch_last: bool = True,
                    resize_modes: dict | None = None) -> None:
    """Apply standard styling and behaviour to a QTableWidget."""
    table.setColumnCount(len(headers))
    table.setHorizontalHeaderLabels(list(headers))
    table.verticalHeader().setVisible(False)
    table.setEditTriggers(QAbstractItemView.NoEditTriggers)
    table.setSelectionBehavior(QAbstractItemView.SelectRows)
    table.setSelectionMode(QAbstractItemView.SingleSelection)
    table.setAlternatingRowColors(True)
    table.setShowGrid(False)
    table.setSortingEnabled(False)
    table.setWordWrap(False)
    table.horizontalHeader().setHighlightSections(False)

    header = table.horizontalHeader()
    header.setSectionResizeMode(QHeaderView.Interactive)
    header.setStretchLastSection(stretch_last)
    if resize_modes:
        for col, mode in resize_modes.items():
            header.setSectionResizeMode(col, mode)

    table.setMinimumHeight(220)


def make_item(text: str, *, align: int = Qt.AlignLeft | Qt.AlignVCenter,
              bold: bool = False, color: str | None = None,
              data=None) -> QTableWidgetItem:
    item = QTableWidgetItem(text if text is not None else "")
    item.setTextAlignment(align)
    if bold:
        font = item.font()
        font.setBold(True)
        item.setFont(font)
    if color:
        from PySide6.QtGui import QColor
        item.setForeground(QColor(color))
    if data is not None:
        item.setData(Qt.UserRole, data)
    return item


def make_money_item(amount, *, bold: bool = False) -> QTableWidgetItem:
    from app.core.utils import format_money
    item = make_item(
        format_money(amount),
        align=Qt.AlignRight | Qt.AlignVCenter,
        bold=bold,
    )
    item.setData(Qt.UserRole, float(amount or 0))
    return item
