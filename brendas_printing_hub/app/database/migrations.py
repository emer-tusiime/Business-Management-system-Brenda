"""
Lightweight schema migrations.

We don't use Alembic to keep the packaged app small. Instead we track the
schema version in the `settings` table and apply incremental migration steps
when the stored version is older than the current code.

For v1.0.0 there are no migrations to apply; this module is a placeholder so
later releases can ship simple SQL alterations without breaking installations.
"""
from __future__ import annotations

from sqlalchemy import text
from sqlalchemy.engine import Engine

CURRENT_SCHEMA_VERSION = 1


def _read_schema_version(engine: Engine) -> int:
    with engine.connect() as conn:
        row = conn.execute(
            text("SELECT value FROM settings WHERE key = 'schema_version'")
        ).fetchone()
        if row and row[0] is not None:
            try:
                return int(row[0])
            except (TypeError, ValueError):
                return 0
        return 0


def _write_schema_version(engine: Engine, version: int) -> None:
    with engine.begin() as conn:
        conn.execute(
            text(
                "INSERT INTO settings(key, value) VALUES('schema_version', :v) "
                "ON CONFLICT(key) DO UPDATE SET value = excluded.value"
            ),
            {"v": str(version)},
        )


def run_migrations(engine: Engine) -> None:
    current = _read_schema_version(engine)
    if current >= CURRENT_SCHEMA_VERSION:
        return
    # Future: apply schema deltas here based on `current`.
    _write_schema_version(engine, CURRENT_SCHEMA_VERSION)
