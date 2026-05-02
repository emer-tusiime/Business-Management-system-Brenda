"""
Low-level database configuration.

We use SQLite with:
- WAL mode (better concurrency)
- foreign_keys ON (we use FKs for integrity)
- synchronous=NORMAL (good speed/safety trade-off)
"""
from __future__ import annotations

from sqlalchemy import event
from sqlalchemy.engine import Engine


def configure_sqlite_pragmas(engine: Engine) -> None:
    """Attach an event listener that enables required SQLite pragmas."""

    @event.listens_for(engine, "connect")
    def _set_sqlite_pragma(dbapi_connection, connection_record):  # noqa: D401, ANN001
        cursor = dbapi_connection.cursor()
        try:
            cursor.execute("PRAGMA foreign_keys=ON")
            cursor.execute("PRAGMA journal_mode=WAL")
            cursor.execute("PRAGMA synchronous=NORMAL")
            cursor.execute("PRAGMA temp_store=MEMORY")
        finally:
            cursor.close()
