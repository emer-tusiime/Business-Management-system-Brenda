"""
Login window.

Two-column layout:
  Left  - dark "brand" panel with the business name + tagline.
  Right - white card with username, password, sign-in button.

After a successful login, opens the main window. If the user is flagged with
must_change_password, prompts a password change before the main window opens.
"""
from __future__ import annotations

from PySide6.QtCore import Qt
from PySide6.QtGui import QIcon
from PySide6.QtWidgets import (
    QDialog,
    QFrame,
    QHBoxLayout,
    QLabel,
    QLineEdit,
    QMessageBox,
    QPushButton,
    QSizePolicy,
    QVBoxLayout,
    QWidget,
)

from app.core.auth import AuthError, authenticate, set_current_user
from app.core.constants import AUDIT_LOGIN
from app.core.security import hash_password
from app.database.models import User
from app.database.session import session_scope
from app.services.audit_service import log_action


# ---------------------------------------------------------------------------
# Change password dialog (used on first login)
# ---------------------------------------------------------------------------

class ChangePasswordDialog(QDialog):
    def __init__(self, username: str, parent: QWidget | None = None) -> None:
        super().__init__(parent)
        self.setWindowTitle("Change Password")
        self.setModal(True)
        self.setMinimumWidth(380)
        self._username = username

        layout = QVBoxLayout(self)
        layout.setContentsMargins(24, 24, 24, 24)
        layout.setSpacing(12)

        title = QLabel("Set a new password")
        title.setObjectName("LoginTitle")
        subtitle = QLabel("For security, please change the default password before continuing.")
        subtitle.setObjectName("LoginSubtitle")
        subtitle.setWordWrap(True)

        layout.addWidget(title)
        layout.addWidget(subtitle)
        layout.addSpacing(6)

        self.new_password = QLineEdit()
        self.new_password.setEchoMode(QLineEdit.Password)
        self.new_password.setPlaceholderText("New password (at least 6 characters)")
        self.confirm_password = QLineEdit()
        self.confirm_password.setEchoMode(QLineEdit.Password)
        self.confirm_password.setPlaceholderText("Confirm new password")

        layout.addWidget(QLabel("New password"))
        layout.addWidget(self.new_password)
        layout.addWidget(QLabel("Confirm password"))
        layout.addWidget(self.confirm_password)

        self.error_label = QLabel("")
        self.error_label.setObjectName("LoginError")
        layout.addWidget(self.error_label)

        button_row = QHBoxLayout()
        button_row.addStretch(1)
        cancel_btn = QPushButton("Cancel")
        cancel_btn.clicked.connect(self.reject)
        save_btn = QPushButton("Update Password")
        save_btn.setObjectName("PrimaryButton")
        save_btn.setDefault(True)
        save_btn.clicked.connect(self._on_save)
        button_row.addWidget(cancel_btn)
        button_row.addWidget(save_btn)
        layout.addLayout(button_row)

    def _on_save(self) -> None:
        pw = self.new_password.text()
        confirm = self.confirm_password.text()
        if len(pw) < 6:
            self.error_label.setText("Password must be at least 6 characters.")
            return
        if pw != confirm:
            self.error_label.setText("Passwords do not match.")
            return

        try:
            with session_scope() as session:
                user = session.query(User).filter(User.username == self._username).one_or_none()
                if user is None:
                    self.error_label.setText("User no longer exists.")
                    return
                user.password_hash = hash_password(pw)
                user.must_change_password = False
        except Exception as exc:  # pragma: no cover - defensive
            self.error_label.setText(f"Could not update password: {exc}")
            return

        self.accept()


# ---------------------------------------------------------------------------
# Login window
# ---------------------------------------------------------------------------

class LoginWindow(QWidget):
    def __init__(self) -> None:
        super().__init__()
        self.setObjectName("LoginRoot")
        self.setWindowTitle("Sign in - Brenda's Printing Hub")
        self.resize(900, 560)
        self._main_window = None  # keep a reference so it isn't garbage collected

        self._build_ui()

    # -- UI ----------------------------------------------------------------

    def _build_ui(self) -> None:
        root = QHBoxLayout(self)
        root.setContentsMargins(0, 0, 0, 0)
        root.setSpacing(0)

        root.addWidget(self._build_brand_panel(), stretch=5)
        root.addWidget(self._build_form_panel(), stretch=4)

    def _build_brand_panel(self) -> QWidget:
        panel = QFrame()
        panel.setStyleSheet("background-color: #0F172A;")
        panel.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)

        layout = QVBoxLayout(panel)
        layout.setContentsMargins(48, 48, 48, 48)
        layout.setSpacing(16)

        small = QLabel("BUSINESS MANAGEMENT SYSTEM")
        small.setStyleSheet("color:#60A5FA; font-size:11px; font-weight:700; letter-spacing:2px;")

        brand = QLabel("Brenda's\nPrinting Hub")
        brand.setObjectName("LoginBrand")
        brand.setWordWrap(True)

        tagline = QLabel(
            "Track sales, stock, expenses and profit for typing, printing, "
            "photocopying, fridge drinks and labelling jobs - all offline."
        )
        tagline.setObjectName("LoginTagline")
        tagline.setWordWrap(True)

        layout.addWidget(small)
        layout.addWidget(brand)
        layout.addWidget(tagline)
        layout.addStretch(1)

        # Three quick feature lines
        for line in (
            "Daily and monthly profit reports",
            "Separate business expenses from owner withdrawals",
            "Works fully offline. Backups to flash disk.",
        ):
            row = QHBoxLayout()
            dot = QLabel("\u2022")
            dot.setStyleSheet("color:#2563EB; font-size:18px; font-weight:700;")
            text = QLabel(line)
            text.setStyleSheet("color:#CBD5E1; font-size:12px;")
            row.addWidget(dot)
            row.addWidget(text, stretch=1)
            layout.addLayout(row)

        layout.addStretch(2)
        version = QLabel("v1.0.0")
        version.setStyleSheet("color:#475569; font-size:11px;")
        layout.addWidget(version)
        return panel

    def _build_form_panel(self) -> QWidget:
        wrapper = QFrame()
        wrapper.setStyleSheet("background-color: #F8FAFC;")
        outer = QVBoxLayout(wrapper)
        outer.setContentsMargins(40, 40, 40, 40)
        outer.addStretch(1)

        card = QFrame()
        card.setObjectName("LoginCard")
        card.setMaximumWidth(380)
        card_layout = QVBoxLayout(card)
        card_layout.setContentsMargins(32, 32, 32, 32)
        card_layout.setSpacing(10)

        title = QLabel("Welcome back")
        title.setObjectName("LoginTitle")
        subtitle = QLabel("Sign in to continue to your dashboard.")
        subtitle.setObjectName("LoginSubtitle")
        subtitle.setWordWrap(True)
        card_layout.addWidget(title)
        card_layout.addWidget(subtitle)
        card_layout.addSpacing(10)

        username_label = QLabel("Username")
        username_label.setStyleSheet("font-weight:600; color:#0F172A;")
        self.username_input = QLineEdit()
        self.username_input.setPlaceholderText("e.g. admin")

        password_label = QLabel("Password")
        password_label.setStyleSheet("font-weight:600; color:#0F172A;")
        self.password_input = QLineEdit()
        self.password_input.setEchoMode(QLineEdit.Password)
        self.password_input.setPlaceholderText("Enter your password")

        card_layout.addWidget(username_label)
        card_layout.addWidget(self.username_input)
        card_layout.addSpacing(4)
        card_layout.addWidget(password_label)
        card_layout.addWidget(self.password_input)

        self.error_label = QLabel("")
        self.error_label.setObjectName("LoginError")
        self.error_label.setWordWrap(True)
        card_layout.addWidget(self.error_label)

        self.sign_in_btn = QPushButton("Sign In")
        self.sign_in_btn.setObjectName("PrimaryButton")
        self.sign_in_btn.setMinimumHeight(40)
        self.sign_in_btn.setDefault(True)
        self.sign_in_btn.clicked.connect(self._on_sign_in)
        card_layout.addSpacing(6)
        card_layout.addWidget(self.sign_in_btn)

        hint = QLabel("Default credentials: admin / admin123 (change on first login)")
        hint.setObjectName("LoginHint")
        hint.setWordWrap(True)
        hint.setAlignment(Qt.AlignCenter)
        card_layout.addSpacing(8)
        card_layout.addWidget(hint)

        # Center the card horizontally
        center_row = QHBoxLayout()
        center_row.addStretch(1)
        center_row.addWidget(card)
        center_row.addStretch(1)
        outer.addLayout(center_row)
        outer.addStretch(2)

        # Submit on Enter from the password field
        self.password_input.returnPressed.connect(self._on_sign_in)
        self.username_input.returnPressed.connect(lambda: self.password_input.setFocus())

        return wrapper

    # -- Behaviour ---------------------------------------------------------

    def _on_sign_in(self) -> None:
        self.error_label.setText("")
        self.sign_in_btn.setEnabled(False)
        self.sign_in_btn.setText("Signing in...")
        try:
            user = authenticate(self.username_input.text(), self.password_input.text())
        except AuthError as exc:
            self.error_label.setText(str(exc))
            self.sign_in_btn.setEnabled(True)
            self.sign_in_btn.setText("Sign In")
            return
        except Exception as exc:  # pragma: no cover - defensive
            self.error_label.setText("Unexpected error. Please try again.")
            print(f"[v0] login error: {exc}")
            self.sign_in_btn.setEnabled(True)
            self.sign_in_btn.setText("Sign In")
            return

        # Force password change on first login
        if user.must_change_password:
            dlg = ChangePasswordDialog(user.username, self)
            if dlg.exec() != QDialog.Accepted:
                self.sign_in_btn.setEnabled(True)
                self.sign_in_btn.setText("Sign In")
                return
            user.must_change_password = False

        set_current_user(user)
        try:
            log_action(
                AUDIT_LOGIN,
                module="auth",
                description=f"User '{user.username}' signed in.",
                user_id=user.id,
                username=user.username,
            )
        except Exception as exc:  # pragma: no cover
            print(f"[v0] audit log failed: {exc}")

        self._open_main_window()

    def _open_main_window(self) -> None:
        # Lazy import - the main window doesn't exist until Turn 2. Until
        # then we show a confirmation dialog so the login flow is testable.
        try:
            from app.ui.main_window import MainWindow  # type: ignore
        except ImportError:
            QMessageBox.information(
                self,
                "Signed in",
                "Login successful. The main dashboard will open here once it is built.",
            )
            self.close()
            return

        self._main_window = MainWindow()
        self._main_window.show()
        self.close()
