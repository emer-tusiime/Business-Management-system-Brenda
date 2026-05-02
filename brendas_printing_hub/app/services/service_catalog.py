"""
CRUD for the computer-services catalog (typing, printing, ...).
Named `service_catalog` to avoid colliding with `app.services` package.
"""
from __future__ import annotations

from typing import List, Optional

from sqlalchemy import select

from app.database.models import Service, ServiceCategory
from app.database.session import session_scope


class ServiceError(Exception):
    pass


def list_services(active_only: bool = True, search: str = "") -> List[Service]:
    with session_scope() as session:
        stmt = select(Service).order_by(Service.name)
        if active_only:
            stmt = stmt.where(Service.is_active.is_(True))
        if search:
            stmt = stmt.where(Service.name.ilike(f"%{search}%"))
        items = list(session.execute(stmt).scalars())
        for s in items:
            session.expunge(s)
        return items


def list_categories() -> List[ServiceCategory]:
    with session_scope() as session:
        items = list(session.execute(select(ServiceCategory).order_by(ServiceCategory.name)).scalars())
        for c in items:
            session.expunge(c)
        return items


def get_service(service_id: int) -> Optional[Service]:
    with session_scope() as session:
        s = session.get(Service, service_id)
        if s is not None:
            session.expunge(s)
        return s


def create_service(*, name: str, category_name: str = "", unit_type: str = "per item",
                   default_price: float = 0.0) -> int:
    if not name.strip():
        raise ServiceError("Service name is required.")
    if default_price < 0:
        raise ServiceError("Price cannot be negative.")
    with session_scope() as session:
        cat_id = None
        if category_name.strip():
            cat = session.execute(
                select(ServiceCategory).where(ServiceCategory.name == category_name.strip())
            ).scalar_one_or_none()
            if cat is None:
                cat = ServiceCategory(name=category_name.strip())
                session.add(cat)
                session.flush()
            cat_id = cat.id
        service = Service(
            name=name.strip(),
            category_id=cat_id,
            unit_type=unit_type,
            default_price=float(default_price),
            is_active=True,
        )
        session.add(service)
        session.flush()
        return service.id


def update_service(service_id: int, **fields) -> None:
    allowed = {"name", "unit_type", "default_price", "is_active"}
    with session_scope() as session:
        service = session.get(Service, service_id)
        if service is None:
            raise ServiceError("Service not found.")
        if "category_name" in fields and fields["category_name"]:
            name = fields["category_name"].strip()
            cat = session.execute(
                select(ServiceCategory).where(ServiceCategory.name == name)
            ).scalar_one_or_none()
            if cat is None:
                cat = ServiceCategory(name=name)
                session.add(cat)
                session.flush()
            service.category_id = cat.id
        for key, value in fields.items():
            if key in allowed and value is not None:
                setattr(service, key, value)
