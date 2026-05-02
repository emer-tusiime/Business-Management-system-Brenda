"""
Common reusable dialogs and toast notifications.
"""
from __future__ import annotations

from typing import Optional

from PySide6.QtCore import Qt, QTimer
from PySide6.QtWidgets import (
    QFrame,
    QHBoxLayout,
    QLabel,
    QMessageBox,
    QVBoxLayout,
    QWidget,
)


def confirm(parent: Optional[QWidget], title: str, message: str,
            destructive: bool = False) -> bool:
    """Show a Yes/No confirmation. Returns True if the user confirmed."""
    box = QMessageBox(parent)
    box.setWindowTitle(title)
    box.setText(message)
    box.setIcon(QMessageBox.Warning if destructive else QMessageBox.Question)
    box.setStandardButtons(QMessageBox.Yes | QMessageBox.No)
    box.setDefaultButton(QMessageBox.No if destructive else QMessageBox.Yes)
    return box.exec() == QMessageBox.Yes


def info(parent: Optional[QWidget], title: str, message: str) -> None:
    QMessageBox.information(parent, title, message)


def warn(parent: Optional[QWidget], title: str, message: str) -> None:
    QMessageBox.warning(parent, title, message)


def error(parent: Optional[QWidget], title: str, message: str) -> None:
    QMessageBox.critical(parent, title, message)


# ---------------------------------------------------------------------------
# Toast - lightweight non-blocking notifications anchored to a parent widget.
# ---------------------------------------------------------------------------

class Toast(QFrame):
    """Self-dismissing notification overlay."""

    def __init__(self, parent: QWidget, message: str, level: str = "success",
                 duration_ms: int = 2500) -> None:
        super().__init__(parent)
        self.setAttribute(Qt.WA_TransparentForMouseEvents)
        self.setFrameShape(QFrame.NoFrame)

        bg = {
            "success": "#16A34A",
            "info": "#2563EB",
            "warning": "#D97706",
            "danger": "#DC2626",
        }.get(level, "#0F172A")

        self.setStyleSheet(
            f"background-color:{bg}; color:#FFFFFF; border-radius:10px;"
        )

        layout = QHBoxLayout(self)
        layout.setContentsMargins(16, 10, 16, 10)
        label = QLabel(message)
        label.setStyleSheet("color:#FFFFFF; font-weight:600;")
        layout.addWidget(label)

        self.adjustSize()
        self._reposition()
        parent.installEventFilter(self)

        self.show()
        QTimer.singleShot(duration_ms, self.close)

    def _reposition(self) -> None:
        parent = self.parentWidget()
        if parent is None:
            return
        margin = 24
        x = parent.width() - self.width() - margin
        y = parent.height() - self.height() - margin
        self.move(max(margin, x), max(margin, y))

    def eventFilter(self, obj, event):  # noqa: N802
        if obj is self.parentWidget() and event.type() in (
            event.Type.Resize, event.Type.Move
        ):
            self._reposition()
        return super().eventFilter(obj, event)


def toast(parent: QWidget, message: str, level: str = "success",
          duration_ms: int = 2500) -> Toast:
    return Toast(parent, message, level, duration_ms)
