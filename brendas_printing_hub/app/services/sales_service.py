"""
Sales business logic.

A Sale is a header row (date, category, payment method, total, recorded_by)
with one or more SaleItem children. For fridge sales, creating the sale also
decrements product stock and writes a StockMovement of type "Sale" inside the
same transaction so totals never drift.
"""
from __future__ import annotations

from dataclasses import dataclass, field
from datetime import date, datetime
from typing import List, Optional

from sqlalchemy import and_, func, select
from sqlalchemy.orm import joinedload

from app.core.constants import (
    PAYMENT_METHODS,
    SALE_CATEGORIES,
    SALE_CATEGORY_FRIDGE,
    STOCK_MOVEMENT_SALE,
)
from app.core.utils import day_bounds, month_bounds
from app.database.models import Product, Sale, SaleItem
from app.database.session import session_scope
from app.services.stock_service import StockError, adjust_stock


class SalesError(Exception):
    pass


# ---------------------------------------------------------------------------
# Input DTOs
# ---------------------------------------------------------------------------

@dataclass
class SaleItemInput:
    description: str
    quantity: float
    unit_price: float
    product_id: Optional[int] = None
    service_id: Optional[int] = None

    @property
    def line_total(self) -> float:
        return float(self.quantity) * float(self.unit_price)


@dataclass
class SaleInput:
    category: str
    payment_method: str
    items: List[SaleItemInput] = field(default_factory=list)
    sale_date: Optional[datetime] = None
    notes: str = ""
    recorded_by: Optional[int] = None
    allow_stock_override: bool = False


@dataclass
class SaleSummary:
    """Detached lightweight DTO for table display."""
    id: int
    sale_date: datetime
    category: str
    payment_method: str
    total_amount: float
    notes: str
    recorded_by: Optional[int]
    item_summary: str


# ---------------------------------------------------------------------------
# Mutations
# ---------------------------------------------------------------------------

def create_sale(data: SaleInput) -> int:
    if data.category not in SALE_CATEGORIES:
        raise SalesError(f"Unknown sale category '{data.category}'.")
    if data.payment_method not in PAYMENT_METHODS:
        raise SalesError(f"Unknown payment method '{data.payment_method}'.")
    if not data.items:
        raise SalesError("A sale must have at least one item.")
    for item in data.items:
        if item.quantity <= 0:
            raise SalesError(f"Quantity for '{item.description}' must be greater than 0.")
        if item.unit_price < 0:
            raise SalesError(f"Unit price for '{item.description}' cannot be negative.")

    total = sum(it.line_total for it in data.items)

    with session_scope() as session:
        # For fridge sales, validate stock BEFORE creating the row so we
        # don't have to roll back. (We still rely on the session_scope rollback
        # if anything else fails.)
        if data.category == SALE_CATEGORY_FRIDGE and not data.allow_stock_override:
            for item in data.items:
                if item.product_id is None:
                    continue
                product = session.get(Product, item.product_id)
                if product is None:
                    raise SalesError(f"Product not found for line '{item.description}'.")
                if product.current_stock < item.quantity:
                    raise StockError(
                        f"Not enough stock for '{product.name}'. "
                        f"Available: {product.current_stock}, requested: {int(item.quantity)}."
                    )

        sale = Sale(
            sale_date=data.sale_date or datetime.utcnow(),
            category=data.category,
            payment_method=data.payment_method,
            total_amount=float(total),
            notes=data.notes or None,
            recorded_by=data.recorded_by,
        )
        session.add(sale)
        session.flush()  # need sale.id for movements

        for item in data.items:
            session.add(
                SaleItem(
                    sale_id=sale.id,
                    product_id=item.product_id,
                    service_id=item.service_id,
                    description=item.description,
                    quantity=float(item.quantity),
                    unit_price=float(item.unit_price),
                    line_total=float(item.line_total),
                )
            )

        # Decrement stock for fridge items inside the same transaction
        if data.category == SALE_CATEGORY_FRIDGE:
            for item in data.items:
                if item.product_id is None:
                    continue
                adjust_stock(
                    product_id=item.product_id,
                    delta=-int(item.quantity),
                    movement_type=STOCK_MOVEMENT_SALE,
                    notes=f"Sale #{sale.id}",
                    sale_id=sale.id,
                    user_id=data.recorded_by,
                    session=session,
                )

        return sale.id


def delete_sale(sale_id: int, *, restore_stock: bool = True) -> None:
    """Delete a sale (Admin-only at the UI layer). Restores fridge stock by
    reading the line items and reversing them via stock movements."""
    with session_scope() as session:
        sale = session.get(Sale, sale_id)
        if sale is None:
            raise SalesError("Sale not found.")

        if restore_stock and sale.category == SALE_CATEGORY_FRIDGE:
            items = list(session.execute(
                select(SaleItem).where(SaleItem.sale_id == sale_id)
            ).scalars())
            for item in items:
                if item.product_id is None:
                    continue
                adjust_stock(
                    product_id=item.product_id,
                    delta=int(item.quantity),
                    movement_type="Adjustment",
                    notes=f"Reversed deletion of sale #{sale_id}",
                    session=session,
                )

        session.delete(sale)


def update_sale_meta(sale_id: int, *, notes: Optional[str] = None,
                     payment_method: Optional[str] = None) -> None:
    """Light edit - changing item lines is not supported; users should delete
    and recreate the sale to keep stock movements consistent."""
    with session_scope() as session:
        sale = session.get(Sale, sale_id)
        if sale is None:
            raise SalesError("Sale not found.")
        if notes is not None:
            sale.notes = notes or None
        if payment_method is not None:
            if payment_method not in PAYMENT_METHODS:
                raise SalesError(f"Unknown payment method '{payment_method}'.")
            sale.payment_method = payment_method


# ---------------------------------------------------------------------------
# Queries
# ---------------------------------------------------------------------------

def list_sales(
    *,
    start: Optional[datetime] = None,
    end: Optional[datetime] = None,
    category: Optional[str] = None,
    payment_method: Optional[str] = None,
    search: str = "",
    limit: int = 500,
) -> List[SaleSummary]:
    with session_scope() as session:
        stmt = (
            select(Sale)
            .options(joinedload(Sale.items))
            .order_by(Sale.sale_date.desc(), Sale.id.desc())
        )
        if start:
            stmt = stmt.where(Sale.sale_date >= start)
        if end:
            stmt = stmt.where(Sale.sale_date < end)
        if category:
            stmt = stmt.where(Sale.category == category)
        if payment_method:
            stmt = stmt.where(Sale.payment_method == payment_method)

        sales = list(session.execute(stmt).unique().scalars())

        if search:
            term = search.lower()
            sales = [
                s for s in sales
                if term in (s.notes or "").lower()
                or any(term in (it.description or "").lower() for it in s.items)
            ]

        sales = sales[:limit]

        return [
            SaleSummary(
                id=s.id,
                sale_date=s.sale_date,
                category=s.category,
                payment_method=s.payment_method,
                total_amount=float(s.total_amount or 0),
                notes=s.notes or "",
                recorded_by=s.recorded_by,
                item_summary=", ".join(
                    f"{int(it.quantity) if it.quantity == int(it.quantity) else it.quantity}\u00d7 {it.description}"
                    for it in s.items
                ) or "-",
            )
            for s in sales
        ]


def get_sale_detail(sale_id: int) -> Optional[Sale]:
    with session_scope() as session:
        sale = session.execute(
            select(Sale).options(joinedload(Sale.items)).where(Sale.id == sale_id)
        ).unique().scalar_one_or_none()
        if sale is None:
            return None
        # Force load and detach
        _ = list(sale.items)
        session.expunge_all()
        return sale


# ---------------------------------------------------------------------------
# Aggregates used by Dashboard / Reports
# ---------------------------------------------------------------------------

def totals_by_category(start: datetime, end: datetime) -> dict:
    """Return {category: total} for sales in [start, end)."""
    result = {cat: 0.0 for cat in SALE_CATEGORIES}
    with session_scope() as session:
        rows = session.execute(
            select(Sale.category, func.sum(Sale.total_amount))
            .where(and_(Sale.sale_date >= start, Sale.sale_date < end))
            .group_by(Sale.category)
        ).all()
        for cat, total in rows:
            result[cat] = float(total or 0)
    return result


def totals_by_payment(start: datetime, end: datetime) -> dict:
    """Return {payment_method: total}."""
    result = {pm: 0.0 for pm in PAYMENT_METHODS}
    with session_scope() as session:
        rows = session.execute(
            select(Sale.payment_method, func.sum(Sale.total_amount))
            .where(and_(Sale.sale_date >= start, Sale.sale_date < end))
            .group_by(Sale.payment_method)
        ).all()
        for pm, total in rows:
            result[pm] = float(total or 0)
    return result


def total_sales(start: datetime, end: datetime) -> float:
    with session_scope() as session:
        value = session.execute(
            select(func.sum(Sale.total_amount))
            .where(and_(Sale.sale_date >= start, Sale.sale_date < end))
        ).scalar()
        return float(value or 0)


def today_totals() -> dict:
    return totals_by_category(*day_bounds(date.today()))


def month_totals() -> dict:
    return totals_by_category(*month_bounds(date.today()))


def top_items(start: datetime, end: datetime, limit: int = 5) -> List[tuple]:
    """Top selling items (description, total_qty, total_revenue)."""
    with session_scope() as session:
        rows = session.execute(
            select(
                SaleItem.description,
                func.sum(SaleItem.quantity),
                func.sum(SaleItem.line_total),
            )
            .join(Sale, Sale.id == SaleItem.sale_id)
            .where(and_(Sale.sale_date >= start, Sale.sale_date < end))
            .group_by(SaleItem.description)
            .order_by(func.sum(SaleItem.line_total).desc())
            .limit(limit)
        ).all()
        return [(desc, float(qty or 0), float(rev or 0)) for desc, qty, rev in rows]


def monthly_revenue_trend(months: int = 6) -> List[tuple]:
    """Return [(YYYY-MM label, total)] for the last `months` months."""
    today = date.today()
    points: List[tuple] = []
    # walk back month by month
    year, month = today.year, today.month
    bounds: List[tuple] = []
    for _ in range(months):
        start_d = date(year, month, 1)
        if month == 12:
            end_d = date(year + 1, 1, 1)
        else:
            end_d = date(year, month + 1, 1)
        bounds.append((start_d, end_d))
        # step back
        if month == 1:
            year -= 1
            month = 12
        else:
            month -= 1
    bounds.reverse()

    with session_scope() as session:
        for start_d, end_d in bounds:
            value = session.execute(
                select(func.sum(Sale.total_amount))
                .where(and_(
                    Sale.sale_date >= datetime.combine(start_d, datetime.min.time()),
                    Sale.sale_date < datetime.combine(end_d, datetime.min.time()),
                ))
            ).scalar()
            points.append((start_d.strftime("%b %y"), float(value or 0)))
    return points
