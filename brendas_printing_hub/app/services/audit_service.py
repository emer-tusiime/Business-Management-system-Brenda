"""
Audit log helpers. These are intentionally synchronous - logging a tiny row
during a user action won't be a bottleneck and we want to be sure the entry
lands before the action returns.
"""
from __future__ import annotations

from typing import Optional

from app.core.auth import current_user
from app.database.models import AuditLog
from app.database.session import session_scope


def log_action(
    action: str,
    module: str,
    description: str = "",
    user_id: Optional[int] = None,
    username: Optional[str] = None,
) -> None:
    """Record an audit row. Falls back to the currently logged-in user if
    explicit identifiers are not supplied."""
    if user_id is None or username is None:
        cu = current_user()
        if cu is not None:
            user_id = user_id or cu.id
            username = username or cu.username

    with session_scope() as session:
        session.add(
            AuditLog(
                user_id=user_id,
                username=username,
                action=action,
                module=module,
                description=description or None,
            )
        )
