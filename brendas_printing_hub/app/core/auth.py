"""
Authentication helpers and a tiny in-process "current user" registry.

The application has a single logged-in user at any one time. Pages query
`current_user()` to decide what actions to expose (e.g. only Admins see the
Delete button on a sale row).
"""
from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime
from typing import Optional

from sqlalchemy import select

from app.core.constants import ROLE_ADMIN
from app.core.security import verify_password
from app.database.session import session_scope
from app.database.models import User


# ---------------------------------------------------------------------------
# Lightweight user identity used everywhere. Detached from the ORM session so
# we don't accidentally trigger lazy loads after the session is closed.
# ---------------------------------------------------------------------------

@dataclass
class CurrentUser:
    id: int
    username: str
    full_name: str
    role: str
    must_change_password: bool

    @property
    def is_admin(self) -> bool:
        return self.role == ROLE_ADMIN


_current_user: Optional[CurrentUser] = None


def current_user() -> Optional[CurrentUser]:
    return _current_user


def set_current_user(user: Optional[CurrentUser]) -> None:
    global _current_user
    _current_user = user


def can_access(module_key: str) -> bool:
    """Return True if the currently logged-in user can open the given module."""
    from app.core.constants import MODULE_ACCESS

    user = current_user()
    if user is None:
        return False
    allowed = MODULE_ACCESS.get(module_key, ())
    return user.role in allowed


# ---------------------------------------------------------------------------
# Login
# ---------------------------------------------------------------------------

class AuthError(Exception):
    """Raised when authentication fails for a known reason (bad password,
    inactive user, etc.). The message is safe to show in the UI."""


def authenticate(username: str, password: str) -> CurrentUser:
    """Verify credentials and return a detached CurrentUser. Raises AuthError."""
    username = (username or "").strip()
    if not username or not password:
        raise AuthError("Please enter both username and password.")

    with session_scope() as session:
        user = session.execute(
            select(User).where(User.username == username)
        ).scalar_one_or_none()

        if user is None:
            raise AuthError("Invalid username or password.")
        if not user.is_active:
            raise AuthError("This account has been disabled. Contact the administrator.")
        if not verify_password(password, user.password_hash):
            raise AuthError("Invalid username or password.")

        # Update last_login inside the same transaction
        user.last_login = datetime.utcnow()

        return CurrentUser(
            id=user.id,
            username=user.username,
            full_name=user.full_name or user.username,
            role=user.role,
            must_change_password=bool(user.must_change_password),
        )
