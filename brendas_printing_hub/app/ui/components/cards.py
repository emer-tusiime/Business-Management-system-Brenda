"""
Reusable visual components: stat cards and section panels.
"""
from __future__ import annotations

from typing import Optional

from PySide6.QtCore import Qt
from PySide6.QtWidgets import (
    QFrame,
    QHBoxLayout,
    QLabel,
    QSizePolicy,
    QVBoxLayout,
    QWidget,
)

from app.core.utils import format_money


class StatCard(QFrame):
    """A KPI card with title, large value, and optional sub-label."""

    def __init__(
        self,
        title: str,
        value: str = "--",
        sublabel: str = "",
        accent: str = "#2563EB",
        parent: Optional[QWidget] = None,
    ) -> None:
        super().__init__(parent)
        self.setObjectName("Card")
        self.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Fixed)
        self.setMinimumHeight(110)

        layout = QVBoxLayout(self)
        layout.setContentsMargins(18, 16, 18, 16)
        layout.setSpacing(6)

        # Coloured accent dot + title row
        head_row = QHBoxLayout()
        head_row.setSpacing(8)
        dot = QLabel()
        dot.setFixedSize(8, 8)
        dot.setStyleSheet(f"background-color:{accent}; border-radius:4px;")
        self._title_label = QLabel(title)
        self._title_label.setObjectName("CardTitle")
        head_row.addWidget(dot)
        head_row.addWidget(self._title_label)
        head_row.addStretch(1)
        layout.addLayout(head_row)

        self._value_label = QLabel(value)
        self._value_label.setObjectName("CardValue")
        self._value_label.setTextInteractionFlags(Qt.TextSelectableByMouse)
        layout.addWidget(self._value_label)

        self._sub_label = QLabel(sublabel)
        self._sub_label.setObjectName("CardDelta")
        self._sub_label.setVisible(bool(sublabel))
        layout.addWidget(self._sub_label)
        layout.addStretch(1)

    def set_title(self, text: str) -> None:
        self._title_label.setText(text)

    def set_value(self, text: str) -> None:
        self._value_label.setText(text)

    def set_money(self, amount) -> None:
        self._value_label.setText(format_money(amount))

    def set_sublabel(self, text: str, negative: bool = False) -> None:
        self._sub_label.setText(text)
        self._sub_label.setObjectName("CardDeltaNegative" if negative else "CardDelta")
        # Force style refresh after object-name change
        self._sub_label.style().unpolish(self._sub_label)
        self._sub_label.style().polish(self._sub_label)
        self._sub_label.setVisible(bool(text))


class Panel(QFrame):
    """A bordered, rounded section card with an optional title and subtitle."""

    def __init__(
        self,
        title: str = "",
        subtitle: str = "",
        parent: Optional[QWidget] = None,
    ) -> None:
        super().__init__(parent)
        self.setObjectName("Panel")
        self._outer = QVBoxLayout(self)
        self._outer.setContentsMargins(18, 16, 18, 16)
        self._outer.setSpacing(10)

        if title or subtitle:
            head = QVBoxLayout()
            head.setSpacing(2)
            if title:
                t = QLabel(title)
                t.setObjectName("PanelTitle")
                head.addWidget(t)
            if subtitle:
                s = QLabel(subtitle)
                s.setObjectName("PanelSubtitle")
                head.addWidget(s)
            self._outer.addLayout(head)

        self._body = QVBoxLayout()
        self._body.setSpacing(10)
        self._outer.addLayout(self._body, stretch=1)

    def add_widget(self, widget: QWidget) -> None:
        self._body.addWidget(widget)

    def add_layout(self, layout) -> None:
        self._body.addLayout(layout)


class StatusBadge(QLabel):
    """Small pill label using the QSS [badge="..."] selectors."""

    _STATE_TO_BADGE = {
        "success": "success",
        "ok": "success",
        "paid": "success",
        "completed": "success",
        "collected": "success",
        "warning": "warning",
        "pending": "warning",
        "in progress": "info",
        "info": "info",
        "danger": "danger",
        "unpaid": "danger",
        "cancelled": "muted",
        "muted": "muted",
        "low": "danger",
        "ok_stock": "success",
    }

    def __init__(self, text: str = "", state: str = "muted", parent: Optional[QWidget] = None) -> None:
        super().__init__(text, parent)
        self.setAlignment(Qt.AlignCenter)
        self.set_state(state)

    def set_state(self, state: str) -> None:
        badge = self._STATE_TO_BADGE.get(state.lower(), "muted")
        self.setProperty("badge", badge)
        # Force style refresh
        self.style().unpolish(self)
        self.style().polish(self)
