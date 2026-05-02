"""
Main application window with sidebar navigation, top bar, and a stacked
content area that swaps pages on demand.

Pages that haven't been built yet show a "Coming soon" placeholder so the
shell is fully clickable from day one.
"""
from __future__ import annotations

from datetime import datetime
from typing import Dict, Optional

from PySide6.QtCore import Qt, QTimer
from PySide6.QtGui import QIcon
from PySide6.QtWidgets import (
    QApplication,
    QButtonGroup,
    QFrame,
    QHBoxLayout,
    QLabel,
    QMainWindow,
    QPushButton,
    QSizePolicy,
    QStackedWidget,
    QVBoxLayout,
    QWidget,
)

from app.core.auth import can_access, current_user, set_current_user
from app.core.constants import AUDIT_LOGOUT
from app.services.audit_service import log_action


# ---------------------------------------------------------------------------
# Sidebar
# ---------------------------------------------------------------------------

NAV_ITEMS = [
    ("dashboard", "Dashboard"),
    ("sales", "Sales"),
    ("services", "Computer Services"),
    ("products", "Fridge & Stock"),
    ("labelling", "Labelling Jobs"),
    ("expenses", "Business Expenses"),
    ("withdrawals", "Owner Withdrawals"),
    ("cash", "Cash Management"),
    ("reports", "Reports"),
    ("settings", "Settings"),
    ("audit", "Audit Logs"),
]


# ---------------------------------------------------------------------------
# Placeholder used for pages that aren't built yet
# ---------------------------------------------------------------------------

class ComingSoonPage(QWidget):
    def __init__(self, title: str) -> None:
        super().__init__()
        layout = QVBoxLayout(self)
        layout.setContentsMargins(28, 24, 28, 28)
        layout.setSpacing(10)
        h = QLabel(title)
        h.setStyleSheet("font-size:20px; font-weight:700; color:#0F172A;")
        sub = QLabel("This module is being prepared and will be available soon.")
        sub.setStyleSheet("color:#64748B;")
        body = QFrame()
        body.setObjectName("Panel")
        body_layout = QVBoxLayout(body)
        body_layout.setContentsMargins(20, 24, 20, 24)
        msg = QLabel("Coming soon")
        msg.setAlignment(Qt.AlignCenter)
        msg.setStyleSheet("font-size:18px; font-weight:700; color:#94A3B8;")
        sub2 = QLabel("Continue using the other modules in the meantime.")
        sub2.setAlignment(Qt.AlignCenter)
        sub2.setStyleSheet("color:#94A3B8;")
        body_layout.addWidget(msg)
        body_layout.addWidget(sub2)
        layout.addWidget(h)
        layout.addWidget(sub)
        layout.addWidget(body, stretch=1)


# ---------------------------------------------------------------------------
# Main window
# ---------------------------------------------------------------------------

class MainWindow(QMainWindow):
    def __init__(self) -> None:
        super().__init__()
        user = current_user()
        self.setWindowTitle(
            f"Brenda's Printing Hub - {user.full_name if user else 'Guest'}"
        )
        self.resize(1280, 800)
        self.setMinimumSize(1100, 700)

        self._pages: Dict[str, QWidget] = {}
        self._nav_buttons: Dict[str, QPushButton] = {}
        self._current_key: Optional[str] = None

        self._build_ui()

        # Default page
        self._select_first_accessible_page()

        # Refresh top-bar clock once a minute
        self._clock_timer = QTimer(self)
        self._clock_timer.setInterval(60_000)
        self._clock_timer.timeout.connect(self._update_topbar_meta)
        self._clock_timer.start()

    # ------------------------------------------------------------------
    # Layout
    # ------------------------------------------------------------------

    def _build_ui(self) -> None:
        central = QWidget()
        self.setCentralWidget(central)
        root = QHBoxLayout(central)
        root.setContentsMargins(0, 0, 0, 0)
        root.setSpacing(0)

        root.addWidget(self._build_sidebar())

        right = QWidget()
        right_layout = QVBoxLayout(right)
        right_layout.setContentsMargins(0, 0, 0, 0)
        right_layout.setSpacing(0)
        right_layout.addWidget(self._build_topbar())

        self.stack = QStackedWidget()
        right_layout.addWidget(self.stack, stretch=1)

        root.addWidget(right, stretch=1)

    def _build_sidebar(self) -> QWidget:
        side = QFrame()
        side.setObjectName("Sidebar")
        side.setFixedWidth(240)

        layout = QVBoxLayout(side)
        layout.setContentsMargins(0, 0, 0, 0)
        layout.setSpacing(0)

        brand = QLabel("Brenda's Hub")
        brand.setObjectName("SidebarBrand")
        tagline = QLabel("BUSINESS MANAGEMENT")
        tagline.setObjectName("SidebarTagline")
        layout.addWidget(brand)
        layout.addWidget(tagline)

        # Buttons
        self._nav_group = QButtonGroup(self)
        self._nav_group.setExclusive(True)

        for key, label in NAV_ITEMS:
            btn = QPushButton(label)
            btn.setCheckable(True)
            btn.setCursor(Qt.PointingHandCursor)
            btn.setEnabled(can_access(key))
            btn.clicked.connect(lambda _=False, k=key: self.show_page(k))
            self._nav_buttons[key] = btn
            self._nav_group.addButton(btn)
            layout.addWidget(btn)

        layout.addStretch(1)

        user = current_user()
        if user is not None:
            user_label = QLabel(
                f"{user.full_name}\n{user.role} \u2022 @{user.username}"
            )
            user_label.setObjectName("SidebarUser")
            user_label.setWordWrap(True)
            layout.addWidget(user_label)

        logout_btn = QPushButton("  Sign out")
        logout_btn.setStyleSheet(
            "color:#FCA5A5; padding:14px 18px; text-align:left; "
            "border-top:1px solid #1E293B; background:transparent;"
        )
        logout_btn.setCursor(Qt.PointingHandCursor)
        logout_btn.clicked.connect(self._on_logout)
        layout.addWidget(logout_btn)

        return side

    def _build_topbar(self) -> QWidget:
        bar = QFrame()
        bar.setObjectName("TopBar")
        bar.setFixedHeight(64)

        layout = QHBoxLayout(bar)
        layout.setContentsMargins(28, 12, 28, 12)
        layout.setSpacing(12)

        self._page_title = QLabel("Dashboard")
        self._page_title.setObjectName("PageTitle")
        layout.addWidget(self._page_title)
        layout.addStretch(1)

        self._topbar_meta = QLabel("")
        self._topbar_meta.setObjectName("TopBarMeta")
        self._topbar_meta.setAlignment(Qt.AlignRight | Qt.AlignVCenter)
        layout.addWidget(self._topbar_meta)
        self._update_topbar_meta()

        return bar

    def _update_topbar_meta(self) -> None:
        user = current_user()
        when = datetime.now().strftime("%A, %d %B %Y  %H:%M")
        if user is not None:
            self._topbar_meta.setText(
                f"<span style='color:#64748B'>Signed in as</span> "
                f"<b style='color:#0F172A'>{user.full_name}</b> "
                f"<span style='color:#94A3B8'>({user.role})</span>"
                f"<span style='color:#CBD5E1'>  \u2022  </span>"
                f"<span style='color:#64748B'>{when}</span>"
            )
        else:
            self._topbar_meta.setText(when)

    # ------------------------------------------------------------------
    # Page swap
    # ------------------------------------------------------------------

    def _select_first_accessible_page(self) -> None:
        for key, _ in NAV_ITEMS:
            if can_access(key):
                self.show_page(key)
                return

    def show_page(self, key: str) -> None:
        if not can_access(key):
            return
        page = self._pages.get(key)
        if page is None:
            page = self._create_page(key)
            self._pages[key] = page
            self.stack.addWidget(page)

        # Refresh page if it has a refresh() method
        try:
            if hasattr(page, "refresh") and callable(page.refresh):
                page.refresh()
        except Exception as exc:  # pragma: no cover
            print(f"[v0] page refresh failed for '{key}': {exc}")

        self.stack.setCurrentWidget(page)
        self._current_key = key

        for k, btn in self._nav_buttons.items():
            btn.setChecked(k == key)
        title = next((label for k, label in NAV_ITEMS if k == key), key.title())
        self._page_title.setText(title)

    def _create_page(self, key: str) -> QWidget:
        if key == "dashboard":
            from app.ui.dashboard_page import DashboardPage
            return DashboardPage()
        if key == "sales":
            from app.ui.sales_page import SalesPage
            return SalesPage()
        # Other pages are added in later turns. Until then, show a placeholder
        # so the shell is fully clickable.
        title = next((label for k, label in NAV_ITEMS if k == key), key.title())
        return ComingSoonPage(title)

    # ------------------------------------------------------------------
    # Logout
    # ------------------------------------------------------------------

    def _on_logout(self) -> None:
        from app.ui.components.dialogs import confirm
        if not confirm(self, "Sign out", "Sign out of the application?"):
            return
        user = current_user()
        if user is not None:
            try:
                log_action(AUDIT_LOGOUT, module="auth",
                           description=f"User '{user.username}' signed out.")
            except Exception as exc:  # pragma: no cover
                print(f"[v0] logout audit failed: {exc}")
        set_current_user(None)
        self._clock_timer.stop()
        self.close()

        # Re-open the login window
        from app.ui.login_window import LoginWindow
        app = QApplication.instance()
        login = LoginWindow()
        login.show()
        # Keep a reference on the QApplication so it isn't GC'd
        if app is not None:
            app._login_window_ref = login  # type: ignore[attr-defined]
