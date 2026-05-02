"""
Cash management: opening balance, live tally during the day, end-of-day
closing reconciliation.

Daily flow
----------
1. Manager/admin opens the day with an opening cash float (CashSession row).
2. Throughout the day, sales / expenses / withdrawals accumulate.
3. At end of day, the user enters the actual counted cash. We compute:

     expected_cash = opening_cash
                   + cash_sales
                   - cash_expenses
                   - owner_withdrawals
     difference    = actual_cash - expected_cash

4. A DailyClosing row is written (immutable history) and the CashSession is
   marked closed.
"""
from __future__ import annotations

from dataclasses import dataclass
from datetime import date, datetime
from typing import List, Optional

from sqlalchemy import and_, func, select

from app.core.constants import (
    PAYMENT_BANK,
    PAYMENT_CASH,
    PAYMENT_MOBILE_MONEY,
)
from app.core.utils import day_bounds, month_bounds
from app.database.models import (
    BusinessExpense,
    CashSession,
    DailyClosing,
    OwnerWithdrawal,
    Sale,
)
from app.database.session import session_scope


class CashError(Exception):
    """Raised for invalid cash management operations."""


# ---------------------------------------------------------------------------
# DTOs
# ---------------------------------------------------------------------------

@dataclass
class DailyTally:
    """Live (or historical) snapshot for a given day."""
    session_date: date
    is_open: bool
    is_closed: bool
    opening_cash: float
    cash_sales: float
    mobile_money_sales: float
    bank_sales: float
    business_expenses: float       # cash only - reduces cash on hand
    business_expenses_all: float   # all methods - shown for reference
    owner_withdrawals: float
    expected_cash: float

    @property
    def total_sales(self) -> float:
        return self.cash_sales + self.mobile_money_sales + self.bank_sales

    @property
    def net_cash_flow(self) -> float:
        return self.cash_sales - self.business_expenses - self.owner_withdrawals


@dataclass
class ClosingSummary:
    """Detached row used to render the closing-history table."""
    id: int
    session_date: date
    opening_cash: float
    cash_sales: float
    mobile_money_sales: float
    bank_sales: float
    business_expenses: float
    owner_withdrawals: float
    expected_cash: float
    actual_cash: float
    difference: float
    closed_at: datetime
    closing_notes: str


# ---------------------------------------------------------------------------
# Session helpers
# ---------------------------------------------------------------------------

def get_session_for_date(d: date) -> Optional[CashSession]:
    with session_scope() as session:
        row = session.execute(
            select(CashSession).where(CashSession.session_date == d)
        ).scalar_one_or_none()
        if row is not None:
            session.expunge(row)
        return row


def is_day_open(d: Optional[date] = None) -> bool:
    d = d or date.today()
    row = get_session_for_date(d)
    return row is not None and not row.is_closed


def open_day(opening_cash: float, *, opened_by: Optional[int] = None,
             notes: str = "", session_date: Optional[date] = None) -> int:
    """Open today (or a specific date). Errors if already open or closed."""
    d = session_date or date.today()
    if opening_cash is None or float(opening_cash) < 0:
        raise CashError("Opening cash must be zero or greater.")

    with session_scope() as session:
        existing = session.execute(
            select(CashSession).where(CashSession.session_date == d)
        ).scalar_one_or_none()

        if existing is not None:
            if existing.is_closed:
                raise CashError(
                    f"The day {d.isoformat()} has already been closed."
                )
            raise CashError(
                f"The day {d.isoformat()} is already open. "
                f"Opening cash: {existing.opening_cash}."
            )

        row = CashSession(
            session_date=d,
            opening_cash=float(opening_cash),
            is_closed=False,
            opened_by=opened_by,
            notes=notes or None,
        )
        session.add(row)
        session.flush()
        return row.id


def close_day(
    actual_cash: float,
    *,
    closed_by: Optional[int] = None,
    closing_notes: str = "",
    session_date: Optional[date] = None,
) -> int:
    """Close the day - writes the immutable DailyClosing snapshot and marks
    the CashSession as closed. Returns the DailyClosing.id."""
    d = session_date or date.today()
    if actual_cash is None or float(actual_cash) < 0:
        raise CashError("Actual cash counted must be zero or greater.")

    with session_scope() as session:
        cash_session = session.execute(
            select(CashSession).where(CashSession.session_date == d)
        ).scalar_one_or_none()

        if cash_session is None:
            raise CashError(
                f"The day {d.isoformat()} has not been opened yet."
            )
        if cash_session.is_closed:
            raise CashError(f"The day {d.isoformat()} is already closed.")

        # Already-closed protection at the DailyClosing level too
        existing_close = session.execute(
            select(DailyClosing).where(DailyClosing.session_date == d)
        ).scalar_one_or_none()
        if existing_close is not None:
            raise CashError(f"A closing already exists for {d.isoformat()}.")

        # Compute the day's tally from the actual data (not from the in-flight
        # tally object the UI showed - this is the authoritative version).
        start, end = day_bounds(d)

        cash_sales = float(session.execute(
            select(func.sum(Sale.total_amount))
            .where(and_(
                Sale.sale_date >= start,
                Sale.sale_date < end,
                Sale.payment_method == PAYMENT_CASH,
            ))
        ).scalar() or 0)

        mobile_money_sales = float(session.execute(
            select(func.sum(Sale.total_amount))
            .where(and_(
                Sale.sale_date >= start,
                Sale.sale_date < end,
                Sale.payment_method == PAYMENT_MOBILE_MONEY,
            ))
        ).scalar() or 0)

        bank_sales = float(session.execute(
            select(func.sum(Sale.total_amount))
            .where(and_(
                Sale.sale_date >= start,
                Sale.sale_date < end,
                Sale.payment_method == PAYMENT_BANK,
            ))
        ).scalar() or 0)

        cash_expenses = float(session.execute(
            select(func.sum(BusinessExpense.amount))
            .where(and_(
                BusinessExpense.expense_date >= start,
                BusinessExpense.expense_date < end,
                BusinessExpense.payment_method == PAYMENT_CASH,
            ))
        ).scalar() or 0)

        owner_withdrawals = float(session.execute(
            select(func.sum(OwnerWithdrawal.amount))
            .where(and_(
                OwnerWithdrawal.withdrawal_date >= start,
                OwnerWithdrawal.withdrawal_date < end,
            ))
        ).scalar() or 0)

        opening_cash = float(cash_session.opening_cash or 0)
        expected_cash = (
            opening_cash + cash_sales - cash_expenses - owner_withdrawals
        )
        diff = float(actual_cash) - expected_cash

        closing = DailyClosing(
            session_date=d,
            opening_cash=opening_cash,
            cash_sales=cash_sales,
            mobile_money_sales=mobile_money_sales,
            bank_sales=bank_sales,
            business_expenses=cash_expenses,
            owner_withdrawals=owner_withdrawals,
            expected_cash=expected_cash,
            actual_cash=float(actual_cash),
            difference=diff,
            closed_by=closed_by,
            closing_notes=closing_notes or None,
        )
        session.add(closing)

        cash_session.is_closed = True

        session.flush()
        return closing.id


# ---------------------------------------------------------------------------
# Live tally (used both for the live "today" panel and for historical days)
# ---------------------------------------------------------------------------

def get_daily_tally(d: Optional[date] = None) -> DailyTally:
    d = d or date.today()
    start, end = day_bounds(d)

    with session_scope() as session:
        cash_session = session.execute(
            select(CashSession).where(CashSession.session_date == d)
        ).scalar_one_or_none()

        opening_cash = float(cash_session.opening_cash or 0) if cash_session else 0.0
        is_open = cash_session is not None and not cash_session.is_closed
        is_closed = cash_session is not None and cash_session.is_closed

        cash_sales = float(session.execute(
            select(func.sum(Sale.total_amount))
            .where(and_(
                Sale.sale_date >= start,
                Sale.sale_date < end,
                Sale.payment_method == PAYMENT_CASH,
            ))
        ).scalar() or 0)

        mobile_money_sales = float(session.execute(
            select(func.sum(Sale.total_amount))
            .where(and_(
                Sale.sale_date >= start,
                Sale.sale_date < end,
                Sale.payment_method == PAYMENT_MOBILE_MONEY,
            ))
        ).scalar() or 0)

        bank_sales = float(session.execute(
            select(func.sum(Sale.total_amount))
            .where(and_(
                Sale.sale_date >= start,
                Sale.sale_date < end,
                Sale.payment_method == PAYMENT_BANK,
            ))
        ).scalar() or 0)

        cash_expenses = float(session.execute(
            select(func.sum(BusinessExpense.amount))
            .where(and_(
                BusinessExpense.expense_date >= start,
                BusinessExpense.expense_date < end,
                BusinessExpense.payment_method == PAYMENT_CASH,
            ))
        ).scalar() or 0)

        all_expenses = float(session.execute(
            select(func.sum(BusinessExpense.amount))
            .where(and_(
                BusinessExpense.expense_date >= start,
                BusinessExpense.expense_date < end,
            ))
        ).scalar() or 0)

        withdrawals = float(session.execute(
            select(func.sum(OwnerWithdrawal.amount))
            .where(and_(
                OwnerWithdrawal.withdrawal_date >= start,
                OwnerWithdrawal.withdrawal_date < end,
            ))
        ).scalar() or 0)

        expected = opening_cash + cash_sales - cash_expenses - withdrawals

        return DailyTally(
            session_date=d,
            is_open=is_open,
            is_closed=is_closed,
            opening_cash=opening_cash,
            cash_sales=cash_sales,
            mobile_money_sales=mobile_money_sales,
            bank_sales=bank_sales,
            business_expenses=cash_expenses,
            business_expenses_all=all_expenses,
            owner_withdrawals=withdrawals,
            expected_cash=expected,
        )


# ---------------------------------------------------------------------------
# Closing history
# ---------------------------------------------------------------------------

def list_closings(
    *,
    start: Optional[date] = None,
    end: Optional[date] = None,
    limit: int = 200,
) -> List[ClosingSummary]:
    with session_scope() as session:
        stmt = (
            select(DailyClosing)
            .order_by(DailyClosing.session_date.desc())
            .limit(limit)
        )
        if start:
            stmt = stmt.where(DailyClosing.session_date >= start)
        if end:
            stmt = stmt.where(DailyClosing.session_date <= end)
        rows = list(session.execute(stmt).scalars())
        return [
            ClosingSummary(
                id=r.id,
                session_date=r.session_date,
                opening_cash=float(r.opening_cash or 0),
                cash_sales=float(r.cash_sales or 0),
                mobile_money_sales=float(r.mobile_money_sales or 0),
                bank_sales=float(r.bank_sales or 0),
                business_expenses=float(r.business_expenses or 0),
                owner_withdrawals=float(r.owner_withdrawals or 0),
                expected_cash=float(r.expected_cash or 0),
                actual_cash=float(r.actual_cash or 0),
                difference=float(r.difference or 0),
                closed_at=r.closed_at,
                closing_notes=r.closing_notes or "",
            )
            for r in rows
        ]


def month_to_date_difference() -> float:
    """Sum of (actual - expected) for all closings this month - useful KPI."""
    start, end = month_bounds(date.today())
    start_d, end_d = start.date(), end.date()
    with session_scope() as session:
        value = session.execute(
            select(func.sum(DailyClosing.difference))
            .where(and_(
                DailyClosing.session_date >= start_d,
                DailyClosing.session_date < end_d,
            ))
        ).scalar()
        return float(value or 0)
