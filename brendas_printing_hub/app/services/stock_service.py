"""
Stock / product business logic.
"""
from __future__ import annotations

from datetime import datetime
from typing import List, Optional

from sqlalchemy import select
from sqlalchemy.orm import Session

from app.core.constants import (
    STOCK_MOVEMENT_ADJUSTMENT,
    STOCK_MOVEMENT_PURCHASE,
    STOCK_MOVEMENT_SALE,
)
from app.database.models import Product, StockMovement
from app.database.session import session_scope


class StockError(Exception):
    """Raised for invalid stock operations (e.g. selling more than on hand)."""


# ---------------------------------------------------------------------------
# Queries
# ---------------------------------------------------------------------------

def list_products(active_only: bool = True, search: str = "") -> List[Product]:
    with session_scope() as session:
        stmt = select(Product).order_by(Product.name)
        if active_only:
            stmt = stmt.where(Product.is_active.is_(True))
        if search:
            like = f"%{search.lower()}%"
            stmt = stmt.where(Product.name.ilike(like))
        items = list(session.execute(stmt).scalars())
        # detach from session
        for p in items:
            session.expunge(p)
        return items


def get_product(product_id: int) -> Optional[Product]:
    with session_scope() as session:
        product = session.get(Product, product_id)
        if product is not None:
            session.expunge(product)
        return product


def low_stock_products(threshold_override: Optional[int] = None) -> List[Product]:
    with session_scope() as session:
        stmt = select(Product).where(Product.is_active.is_(True))
        items = list(session.execute(stmt).scalars())
        result: List[Product] = []
        for p in items:
            limit = threshold_override if threshold_override is not None else p.low_stock_threshold
            if p.current_stock <= limit:
                result.append(p)
        for p in result:
            session.expunge(p)
        return result


# ---------------------------------------------------------------------------
# Mutations
# ---------------------------------------------------------------------------

def create_product(
    *,
    name: str,
    brand: str = "",
    category: str = "Other",
    buying_price: float = 0.0,
    selling_price: float = 0.0,
    opening_stock: int = 0,
    low_stock_threshold: int = 5,
    user_id: Optional[int] = None,
) -> int:
    if not name.strip():
        raise StockError("Product name is required.")
    if buying_price < 0 or selling_price < 0:
        raise StockError("Prices cannot be negative.")
    if opening_stock < 0:
        raise StockError("Opening stock cannot be negative.")

    with session_scope() as session:
        product = Product(
            name=name.strip(),
            brand=(brand or "").strip() or None,
            category=category or "Other",
            buying_price=float(buying_price),
            selling_price=float(selling_price),
            current_stock=int(opening_stock),
            low_stock_threshold=int(low_stock_threshold),
            is_active=True,
        )
        session.add(product)
        session.flush()

        if opening_stock > 0:
            session.add(
                StockMovement(
                    product_id=product.id,
                    movement_type="Opening",
                    quantity=int(opening_stock),
                    unit_cost=float(buying_price),
                    notes="Opening stock",
                    created_by=user_id,
                )
            )
        return product.id


def update_product(product_id: int, **fields) -> None:
    allowed = {
        "name", "brand", "category", "buying_price", "selling_price",
        "low_stock_threshold", "is_active",
    }
    with session_scope() as session:
        product = session.get(Product, product_id)
        if product is None:
            raise StockError("Product not found.")
        for key, value in fields.items():
            if key in allowed and value is not None:
                setattr(product, key, value)


def adjust_stock(
    product_id: int,
    *,
    delta: int,
    movement_type: str,
    notes: str = "",
    sale_id: Optional[int] = None,
    user_id: Optional[int] = None,
    session: Optional[Session] = None,
) -> None:
    """Apply a stock change. If a session is supplied, work inside it; else
    open a new transactional scope."""

    def _do(s: Session) -> None:
        product = s.get(Product, product_id)
        if product is None:
            raise StockError("Product not found.")
        new_qty = product.current_stock + int(delta)
        if new_qty < 0:
            raise StockError(
                f"Insufficient stock for '{product.name}'. "
                f"Available: {product.current_stock}, requested: {-delta}."
            )
        product.current_stock = new_qty
        s.add(
            StockMovement(
                product_id=product_id,
                movement_type=movement_type,
                quantity=int(delta),
                unit_cost=float(product.buying_price),
                notes=notes or None,
                sale_id=sale_id,
                created_by=user_id,
                created_at=datetime.utcnow(),
            )
        )

    if session is not None:
        _do(session)
    else:
        with session_scope() as s:
            _do(s)


def record_purchase(product_id: int, quantity: int, unit_cost: float,
                    notes: str = "", user_id: Optional[int] = None) -> None:
    if quantity <= 0:
        raise StockError("Purchase quantity must be positive.")
    with session_scope() as session:
        product = session.get(Product, product_id)
        if product is None:
            raise StockError("Product not found.")
        product.current_stock += int(quantity)
        if unit_cost > 0:
            product.buying_price = float(unit_cost)  # update to most recent cost
        session.add(
            StockMovement(
                product_id=product_id,
                movement_type=STOCK_MOVEMENT_PURCHASE,
                quantity=int(quantity),
                unit_cost=float(unit_cost),
                notes=notes or None,
                created_by=user_id,
            )
        )


def list_movements(product_id: Optional[int] = None, limit: int = 200) -> List[StockMovement]:
    with session_scope() as session:
        stmt = select(StockMovement).order_by(StockMovement.created_at.desc()).limit(limit)
        if product_id is not None:
            stmt = stmt.where(StockMovement.product_id == product_id)
        items = list(session.execute(stmt).scalars())
        for m in items:
            session.expunge(m)
        return items
