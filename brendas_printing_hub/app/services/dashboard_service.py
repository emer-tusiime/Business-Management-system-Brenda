"""
Aggregates the small numbers used by the dashboard. Keeps the dashboard page
free from raw SQL.
"""
from __future__ import annotations

from dataclasses import dataclass
from datetime import date, datetime
from typing import List, Tuple

from sqlalchemy import and_, func, select

from app.core.constants import (
    JOB_STATUS_COLLECTED,
    JOB_STATUS_CANCELLED,
    PAYMENT_CASH,
    SALE_CATEGORY_COMPUTER,
    SALE_CATEGORY_FRIDGE,
    SALE_CATEGORY_LABELLING,
)
from app.core.utils import day_bounds, month_bounds
from app.database.models import (
    BusinessExpense,
    LabellingJob,
    OwnerWithdrawal,
    Sale,
    CashSession,
)
from app.database.session import session_scope
from app.services import sales_service
from app.services.stock_service import low_stock_products


@dataclass
class DashboardSnapshot:
    today_total: float
    today_computer: float
    today_fridge: float
    today_labelling: float
    today_expenses: float
    today_withdrawals: float
    today_net_profit: float
    expected_cash: float
    month_total: float
    month_net_profit: float
    low_stock_count: int
    pending_jobs: int

    # For charts
    sales_by_category: List[Tuple[str, float]]
    monthly_trend: List[Tuple[str, float]]
    top_items: List[Tuple[str, float, float]]
    expenses_vs_withdrawals: Tuple[float, float]  # (expenses_month, withdrawals_month)


def _sum(session, model, *, start: datetime, end: datetime, date_col) -> float:
    value = session.execute(
        select(func.sum(model.amount)).where(and_(date_col >= start, date_col < end))
    ).scalar()
    return float(value or 0)


def get_dashboard_snapshot() -> DashboardSnapshot:
    today = date.today()
    today_start, today_end = day_bounds(today)
    month_start, month_end = month_bounds(today)

    by_cat_today = sales_service.totals_by_category(today_start, today_end)
    by_cat_month = sales_service.totals_by_category(month_start, month_end)
    by_payment_today = sales_service.totals_by_payment(today_start, today_end)

    today_total = sum(by_cat_today.values())
    month_total = sum(by_cat_month.values())

    with session_scope() as session:
        today_expenses = _sum(session, BusinessExpense,
                              start=today_start, end=today_end,
                              date_col=BusinessExpense.expense_date)
        today_withdrawals = _sum(session, OwnerWithdrawal,
                                 start=today_start, end=today_end,
                                 date_col=OwnerWithdrawal.withdrawal_date)
        month_expenses = _sum(session, BusinessExpense,
                              start=month_start, end=month_end,
                              date_col=BusinessExpense.expense_date)
        month_withdrawals = _sum(session, OwnerWithdrawal,
                                 start=month_start, end=month_end,
                                 date_col=OwnerWithdrawal.withdrawal_date)

        # Pending labelling jobs (not yet collected/cancelled)
        pending_jobs = session.execute(
            select(func.count(LabellingJob.id)).where(
                LabellingJob.job_status.notin_((JOB_STATUS_COLLECTED, JOB_STATUS_CANCELLED))
            )
        ).scalar() or 0

        # Opening cash from today's CashSession (if started)
        opening_cash_row = session.execute(
            select(CashSession.opening_cash).where(CashSession.session_date == today)
        ).scalar()
        opening_cash = float(opening_cash_row or 0)

    cash_sales_today = float(by_payment_today.get(PAYMENT_CASH, 0))
    today_net_profit = today_total - today_expenses
    expected_cash = opening_cash + cash_sales_today - today_expenses - today_withdrawals
    month_net_profit = month_total - month_expenses

    low_stock_count = len(low_stock_products())

    sales_by_category = [
        (SALE_CATEGORY_COMPUTER, by_cat_today.get(SALE_CATEGORY_COMPUTER, 0)),
        (SALE_CATEGORY_FRIDGE, by_cat_today.get(SALE_CATEGORY_FRIDGE, 0)),
        (SALE_CATEGORY_LABELLING, by_cat_today.get(SALE_CATEGORY_LABELLING, 0)),
    ]
    monthly_trend = sales_service.monthly_revenue_trend(months=6)
    top = sales_service.top_items(month_start, month_end, limit=5)
    top_items_disp = [(desc, qty, rev) for desc, qty, rev in top]

    return DashboardSnapshot(
        today_total=today_total,
        today_computer=by_cat_today.get(SALE_CATEGORY_COMPUTER, 0),
        today_fridge=by_cat_today.get(SALE_CATEGORY_FRIDGE, 0),
        today_labelling=by_cat_today.get(SALE_CATEGORY_LABELLING, 0),
        today_expenses=today_expenses,
        today_withdrawals=today_withdrawals,
        today_net_profit=today_net_profit,
        expected_cash=expected_cash,
        month_total=month_total,
        month_net_profit=month_net_profit,
        low_stock_count=low_stock_count,
        pending_jobs=int(pending_jobs),
        sales_by_category=sales_by_category,
        monthly_trend=monthly_trend,
        top_items=top_items_disp,
        expenses_vs_withdrawals=(month_expenses, month_withdrawals),
    )
