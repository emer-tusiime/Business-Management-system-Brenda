"""
Small generic helpers used across UI and services.
"""
from __future__ import annotations

from datetime import date, datetime, time, timedelta
from decimal import Decimal
from typing import Tuple, Union

Number = Union[int, float, Decimal, None]


# ---------------------------------------------------------------------------
# Money / number formatting
# ---------------------------------------------------------------------------

def format_money(amount: Number, with_symbol: bool = True) -> str:
    """Format a number as `UGX 250,000` (no decimals - shillings are whole units)."""
    if amount is None:
        amount = 0
    try:
        value = int(round(float(amount)))
    except (TypeError, ValueError):
        value = 0
    formatted = f"{value:,}"
    return f"UGX {formatted}" if with_symbol else formatted


def parse_money(text: str) -> int:
    """Parse a user-entered money string back into an int. Tolerates commas,
    spaces, and the UGX prefix."""
    if not text:
        return 0
    cleaned = (
        text.replace("UGX", "")
        .replace("ugx", "")
        .replace(",", "")
        .replace(" ", "")
        .strip()
    )
    if not cleaned:
        return 0
    try:
        return int(round(float(cleaned)))
    except ValueError:
        return 0


def safe_int(value, default: int = 0) -> int:
    try:
        return int(value)
    except (TypeError, ValueError):
        return default


def safe_float(value, default: float = 0.0) -> float:
    try:
        return float(value)
    except (TypeError, ValueError):
        return default


# ---------------------------------------------------------------------------
# Date helpers
# ---------------------------------------------------------------------------

def day_bounds(d: date) -> Tuple[datetime, datetime]:
    """Return (start_of_day, start_of_next_day) datetimes for a given date."""
    start = datetime.combine(d, time.min)
    end = datetime.combine(d + timedelta(days=1), time.min)
    return start, end


def month_bounds(d: date) -> Tuple[datetime, datetime]:
    """Return (start_of_month, start_of_next_month)."""
    start = datetime.combine(d.replace(day=1), time.min)
    if d.month == 12:
        next_month = date(d.year + 1, 1, 1)
    else:
        next_month = date(d.year, d.month + 1, 1)
    end = datetime.combine(next_month, time.min)
    return start, end


def week_bounds(d: date) -> Tuple[datetime, datetime]:
    """Return (start_of_week, start_of_next_week) - week starts Monday."""
    monday = d - timedelta(days=d.weekday())
    start = datetime.combine(monday, time.min)
    end = datetime.combine(monday + timedelta(days=7), time.min)
    return start, end


def format_date(d: Union[date, datetime, None], fmt: str = "%d %b %Y") -> str:
    if d is None:
        return ""
    return d.strftime(fmt)


def format_datetime(d: Union[datetime, None], fmt: str = "%d %b %Y %H:%M") -> str:
    if d is None:
        return ""
    return d.strftime(fmt)
