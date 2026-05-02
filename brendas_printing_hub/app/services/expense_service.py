"""
Business expense business logic.

Business expenses reduce reported profit. They are distinct from owner
withdrawals (which do NOT reduce profit but DO reduce available cash).

Service functions are module-level and return detached DTOs / ORM objects so
the UI never has to manage SQLAlchemy sessions.
"""
from __future__ import annotations

from dataclasses import dataclass
from datetime import date, datetime
from typing import List, Optional

from sqlalchemy import and_, func, select

from app.core.constants import EXPENSE_CATEGORIES, PAYMENT_METHODS
from app.core.utils import day_bounds, month_bounds
from app.database.models import BusinessExpense
from app.database.session import session_scope


class ExpenseError(Exception):
    """Raised for invalid expense input (e.g. unknown category)."""


# ---------------------------------------------------------------------------
# DTOs
# ---------------------------------------------------------------------------

@dataclass
class ExpenseSummary:
    """Detached lightweight DTO for table display."""
    id: int
    expense_date: datetime
    category: str
    description: str
    amount: float
    payment_method: str
    notes: str
    recorded_by: Optional[int]


# ---------------------------------------------------------------------------
# Mutations
# ---------------------------------------------------------------------------

def create_expense(
    *,
    category: str,
    amount: float,
    expense_date: Optional[datetime] = None,
    description: str = "",
    payment_method: str = "Cash",
    notes: str = "",
    recorded_by: Optional[int] = None,
) -> int:
    if category not in EXPENSE_CATEGORIES:
        raise ExpenseError(f"Unknown expense category '{category}'.")
    if payment_method not in PAYMENT_METHODS:
        raise ExpenseError(f"Unknown payment method '{payment_method}'.")
    if amount is None or float(amount) <= 0:
        raise ExpenseError("Amount must be greater than 0.")

    with session_scope() as session:
        row = BusinessExpense(
            expense_date=expense_date or datetime.utcnow(),
            category=category,
            description=description or None,
            amount=float(amount),
            payment_method=payment_method,
            notes=notes or None,
            recorded_by=recorded_by,
        )
        session.add(row)
        session.flush()
        return row.id


def update_expense(
    expense_id: int,
    *,
    category: Optional[str] = None,
    amount: Optional[float] = None,
    expense_date: Optional[datetime] = None,
    description: Optional[str] = None,
    payment_method: Optional[str] = None,
    notes: Optional[str] = None,
) -> None:
    with session_scope() as session:
        row = session.get(BusinessExpense, expense_id)
        if row is None:
            raise ExpenseError("Expense not found.")
        if category is not None:
            if category not in EXPENSE_CATEGORIES:
                raise ExpenseError(f"Unknown expense category '{category}'.")
            row.category = category
        if amount is not None:
            if float(amount) <= 0:
                raise ExpenseError("Amount must be greater than 0.")
            row.amount = float(amount)
        if expense_date is not None:
            row.expense_date = expense_date
        if description is not None:
            row.description = description or None
        if payment_method is not None:
            if payment_method not in PAYMENT_METHODS:
                raise ExpenseError(f"Unknown payment method '{payment_method}'.")
            row.payment_method = payment_method
        if notes is not None:
            row.notes = notes or None


def delete_expense(expense_id: int) -> None:
    with session_scope() as session:
        row = session.get(BusinessExpense, expense_id)
        if row is None:
            raise ExpenseError("Expense not found.")
        session.delete(row)


# ---------------------------------------------------------------------------
# Queries
# ---------------------------------------------------------------------------

def list_expenses(
    *,
    start: Optional[datetime] = None,
    end: Optional[datetime] = None,
    category: Optional[str] = None,
    search: str = "",
    limit: int = 500,
) -> List[ExpenseSummary]:
    with session_scope() as session:
        stmt = (
            select(BusinessExpense)
            .order_by(BusinessExpense.expense_date.desc(), BusinessExpense.id.desc())
        )
        if start:
            stmt = stmt.where(BusinessExpense.expense_date >= start)
        if end:
            stmt = stmt.where(BusinessExpense.expense_date < end)
        if category:
            stmt = stmt.where(BusinessExpense.category == category)
        rows = list(session.execute(stmt).scalars())

        if search:
            term = search.lower()
            rows = [
                r for r in rows
                if term in (r.description or "").lower()
                or term in (r.notes or "").lower()
            ]

        rows = rows[:limit]

        return [
            ExpenseSummary(
                id=r.id,
                expense_date=r.expense_date,
                category=r.category,
                description=r.description or "",
                amount=float(r.amount or 0),
                payment_method=r.payment_method,
                notes=r.notes or "",
                recorded_by=r.recorded_by,
            )
            for r in rows
        ]


def get_expense(expense_id: int) -> Optional[BusinessExpense]:
    with session_scope() as session:
        row = session.get(BusinessExpense, expense_id)
        if row is None:
            return None
        session.expunge(row)
        return row


# ---------------------------------------------------------------------------
# Aggregates
# ---------------------------------------------------------------------------

def total_in_range(start: datetime, end: datetime) -> float:
    with session_scope() as session:
        value = session.execute(
            select(func.sum(BusinessExpense.amount))
            .where(and_(
                BusinessExpense.expense_date >= start,
                BusinessExpense.expense_date < end,
            ))
        ).scalar()
        return float(value or 0)


def totals_by_category(start: datetime, end: datetime) -> dict:
    """Return {category: total} for the given window."""
    result = {cat: 0.0 for cat in EXPENSE_CATEGORIES}
    with session_scope() as session:
        rows = session.execute(
            select(BusinessExpense.category, func.sum(BusinessExpense.amount))
            .where(and_(
                BusinessExpense.expense_date >= start,
                BusinessExpense.expense_date < end,
            ))
            .group_by(BusinessExpense.category)
        ).all()
        for cat, total in rows:
            result[cat] = float(total or 0)
    return result


def today_total() -> float:
    return total_in_range(*day_bounds(date.today()))


def month_total() -> float:
    return total_in_range(*month_bounds(date.today()))


def total_cash_in_range(start: datetime, end: datetime) -> float:
    """Only cash expenses - used by daily-closing reconciliation."""
    with session_scope() as session:
        value = session.execute(
            select(func.sum(BusinessExpense.amount))
            .where(and_(
                BusinessExpense.expense_date >= start,
                BusinessExpense.expense_date < end,
                BusinessExpense.payment_method == "Cash",
            ))
        ).scalar()
        return float(value or 0)
