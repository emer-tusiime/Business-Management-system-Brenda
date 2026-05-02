"""
SQLAlchemy engine + session factory.

Use `session_scope()` as a context manager for any unit of work:

    with session_scope() as session:
        session.add(...)

The context manager commits on success and rolls back on any exception.
"""
from __future__ import annotations

from contextlib import contextmanager
from typing import Iterator

from sqlalchemy import create_engine
from sqlalchemy.orm import Session, sessionmaker

from config.database import configure_sqlite_pragmas
from config.settings import DATABASE_URL, ensure_runtime_directories


# Engine is lazily created the first time it's needed so unit-test code or
# migration tools can override DATABASE_URL before init.
_engine = None
_SessionFactory: sessionmaker | None = None


def _build_engine():
    ensure_runtime_directories()
    engine = create_engine(
        DATABASE_URL,
        echo=False,
        future=True,
        connect_args={"check_same_thread": False},
    )
    configure_sqlite_pragmas(engine)
    return engine


def get_engine():
    global _engine
    if _engine is None:
        _engine = _build_engine()
    return _engine


def get_session_factory() -> sessionmaker:
    global _SessionFactory
    if _SessionFactory is None:
        _SessionFactory = sessionmaker(
            bind=get_engine(),
            autoflush=False,
            autocommit=False,
            expire_on_commit=False,
            future=True,
        )
    return _SessionFactory


@contextmanager
def session_scope() -> Iterator[Session]:
    """Provide a transactional scope around a series of operations."""
    factory = get_session_factory()
    session: Session = factory()
    try:
        yield session
        session.commit()
    except Exception:
        session.rollback()
        raise
    finally:
        session.close()


def init_database() -> None:
    """Create tables if they don't exist."""
    from app.database.models import Base  # local import avoids circular deps
    from app.database.migrations import run_migrations

    engine = get_engine()
    Base.metadata.create_all(engine)
    run_migrations(engine)
