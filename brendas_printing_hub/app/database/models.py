"""
SQLAlchemy ORM models.

Schema overview
---------------
users               - login accounts with role + bcrypt hashed password
service_categories  - groups for computer services
services            - typing, printing, photocopying, ...
products            - drinks / fridge stock
stock_movements     - every +/- change to a product's quantity (audit trail)
sales               - parent sale row (header)
sale_items          - line items (a sale can have multiple items)
labelling_jobs      - labelling work for clothing
labelling_payments  - deposits / part-payments against a labelling job
business_expenses   - operating costs (reduce profit)
owner_withdrawals   - personal money taken from the till (do NOT reduce profit)
cash_sessions       - one row per business day (opening + closing snapshot)
daily_closings      - finalised end-of-day record (immutable)
audit_logs          - sensitive action history
settings            - key/value app configuration
"""
from __future__ import annotations

from datetime import date as date_, datetime
from decimal import Decimal
from typing import List, Optional

from sqlalchemy import (
    Boolean,
    Date,
    DateTime,
    Float,
    ForeignKey,
    Index,
    Integer,
    String,
    Text,
    UniqueConstraint,
)
from sqlalchemy.orm import DeclarativeBase, Mapped, mapped_column, relationship


class Base(DeclarativeBase):
    pass


# ---------------------------------------------------------------------------
# Users
# ---------------------------------------------------------------------------

class User(Base):
    __tablename__ = "users"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    username: Mapped[str] = mapped_column(String(64), unique=True, nullable=False)
    full_name: Mapped[Optional[str]] = mapped_column(String(120))
    password_hash: Mapped[str] = mapped_column(String(255), nullable=False)
    role: Mapped[str] = mapped_column(String(32), nullable=False)
    is_active: Mapped[bool] = mapped_column(Boolean, default=True, nullable=False)
    must_change_password: Mapped[bool] = mapped_column(
        Boolean, default=False, nullable=False
    )
    last_login: Mapped[Optional[datetime]] = mapped_column(DateTime)
    created_at: Mapped[datetime] = mapped_column(
        DateTime, default=datetime.utcnow, nullable=False
    )

    def __repr__(self) -> str:  # pragma: no cover
        return f"<User {self.username} ({self.role})>"


# ---------------------------------------------------------------------------
# Services
# ---------------------------------------------------------------------------

class ServiceCategory(Base):
    __tablename__ = "service_categories"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    name: Mapped[str] = mapped_column(String(80), unique=True, nullable=False)
    services: Mapped[List["Service"]] = relationship(back_populates="category")


class Service(Base):
    __tablename__ = "services"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    name: Mapped[str] = mapped_column(String(120), nullable=False)
    category_id: Mapped[Optional[int]] = mapped_column(
        ForeignKey("service_categories.id", ondelete="SET NULL")
    )
    unit_type: Mapped[str] = mapped_column(String(32), default="per item", nullable=False)
    default_price: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)
    is_active: Mapped[bool] = mapped_column(Boolean, default=True, nullable=False)
    created_at: Mapped[datetime] = mapped_column(
        DateTime, default=datetime.utcnow, nullable=False
    )

    category: Mapped[Optional[ServiceCategory]] = relationship(back_populates="services")

    __table_args__ = (UniqueConstraint("name", name="uq_service_name"),)


# ---------------------------------------------------------------------------
# Products & stock
# ---------------------------------------------------------------------------

class Product(Base):
    __tablename__ = "products"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    name: Mapped[str] = mapped_column(String(120), nullable=False)
    brand: Mapped[Optional[str]] = mapped_column(String(80))
    category: Mapped[str] = mapped_column(String(64), default="Other", nullable=False)
    buying_price: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)
    selling_price: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)
    current_stock: Mapped[int] = mapped_column(Integer, default=0, nullable=False)
    low_stock_threshold: Mapped[int] = mapped_column(Integer, default=5, nullable=False)
    is_active: Mapped[bool] = mapped_column(Boolean, default=True, nullable=False)
    created_at: Mapped[datetime] = mapped_column(
        DateTime, default=datetime.utcnow, nullable=False
    )

    movements: Mapped[List["StockMovement"]] = relationship(
        back_populates="product", cascade="all, delete-orphan"
    )

    __table_args__ = (UniqueConstraint("name", "brand", name="uq_product_name_brand"),)


class StockMovement(Base):
    __tablename__ = "stock_movements"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    product_id: Mapped[int] = mapped_column(
        ForeignKey("products.id", ondelete="CASCADE"), nullable=False
    )
    movement_type: Mapped[str] = mapped_column(String(32), nullable=False)
    quantity: Mapped[int] = mapped_column(Integer, nullable=False)  # +ve in, -ve out
    unit_cost: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)
    notes: Mapped[Optional[str]] = mapped_column(Text)
    sale_id: Mapped[Optional[int]] = mapped_column(
        ForeignKey("sales.id", ondelete="SET NULL")
    )
    created_by: Mapped[Optional[int]] = mapped_column(
        ForeignKey("users.id", ondelete="SET NULL")
    )
    created_at: Mapped[datetime] = mapped_column(
        DateTime, default=datetime.utcnow, nullable=False, index=True
    )

    product: Mapped[Product] = relationship(back_populates="movements")


# ---------------------------------------------------------------------------
# Sales
# ---------------------------------------------------------------------------

class Sale(Base):
    __tablename__ = "sales"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    sale_date: Mapped[datetime] = mapped_column(
        DateTime, default=datetime.utcnow, nullable=False, index=True
    )
    category: Mapped[str] = mapped_column(String(32), nullable=False, index=True)
    payment_method: Mapped[str] = mapped_column(String(32), nullable=False, index=True)
    total_amount: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)
    notes: Mapped[Optional[str]] = mapped_column(Text)
    recorded_by: Mapped[Optional[int]] = mapped_column(
        ForeignKey("users.id", ondelete="SET NULL")
    )
    created_at: Mapped[datetime] = mapped_column(
        DateTime, default=datetime.utcnow, nullable=False
    )

    items: Mapped[List["SaleItem"]] = relationship(
        back_populates="sale", cascade="all, delete-orphan"
    )

    __table_args__ = (
        Index("ix_sales_date_category", "sale_date", "category"),
    )


class SaleItem(Base):
    __tablename__ = "sale_items"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    sale_id: Mapped[int] = mapped_column(
        ForeignKey("sales.id", ondelete="CASCADE"), nullable=False
    )
    # Either product_id (fridge) or service_id (computer work) or neither
    # (free-form labelling line). description is always populated for display.
    product_id: Mapped[Optional[int]] = mapped_column(
        ForeignKey("products.id", ondelete="SET NULL")
    )
    service_id: Mapped[Optional[int]] = mapped_column(
        ForeignKey("services.id", ondelete="SET NULL")
    )
    description: Mapped[str] = mapped_column(String(255), nullable=False)
    quantity: Mapped[float] = mapped_column(Float, default=1.0, nullable=False)
    unit_price: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)
    line_total: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)

    sale: Mapped[Sale] = relationship(back_populates="items")


# ---------------------------------------------------------------------------
# Labelling
# ---------------------------------------------------------------------------

class LabellingJob(Base):
    __tablename__ = "labelling_jobs"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    customer_name: Mapped[str] = mapped_column(String(120), nullable=False)
    customer_phone: Mapped[Optional[str]] = mapped_column(String(40))
    item_type: Mapped[str] = mapped_column(String(64), nullable=False)
    description: Mapped[Optional[str]] = mapped_column(Text)
    quantity: Mapped[int] = mapped_column(Integer, default=1, nullable=False)
    unit_price: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)
    total_amount: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)
    amount_paid: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)
    payment_status: Mapped[str] = mapped_column(
        String(32), default="Unpaid", nullable=False, index=True
    )
    job_status: Mapped[str] = mapped_column(
        String(32), default="Pending", nullable=False, index=True
    )
    due_date: Mapped[Optional[date_]] = mapped_column(Date)
    notes: Mapped[Optional[str]] = mapped_column(Text)
    created_by: Mapped[Optional[int]] = mapped_column(
        ForeignKey("users.id", ondelete="SET NULL")
    )
    created_at: Mapped[datetime] = mapped_column(
        DateTime, default=datetime.utcnow, nullable=False, index=True
    )
    updated_at: Mapped[datetime] = mapped_column(
        DateTime, default=datetime.utcnow, onupdate=datetime.utcnow, nullable=False
    )

    payments: Mapped[List["LabellingPayment"]] = relationship(
        back_populates="job", cascade="all, delete-orphan"
    )

    @property
    def balance(self) -> float:
        return float((self.total_amount or 0) - (self.amount_paid or 0))


class LabellingPayment(Base):
    __tablename__ = "labelling_payments"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    job_id: Mapped[int] = mapped_column(
        ForeignKey("labelling_jobs.id", ondelete="CASCADE"), nullable=False
    )
    amount: Mapped[float] = mapped_column(Float, nullable=False)
    payment_method: Mapped[str] = mapped_column(String(32), default="Cash", nullable=False)
    paid_on: Mapped[datetime] = mapped_column(
        DateTime, default=datetime.utcnow, nullable=False
    )
    notes: Mapped[Optional[str]] = mapped_column(Text)
    recorded_by: Mapped[Optional[int]] = mapped_column(
        ForeignKey("users.id", ondelete="SET NULL")
    )

    job: Mapped[LabellingJob] = relationship(back_populates="payments")


# ---------------------------------------------------------------------------
# Business expenses (reduce profit)
# ---------------------------------------------------------------------------

class BusinessExpense(Base):
    __tablename__ = "business_expenses"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    expense_date: Mapped[datetime] = mapped_column(
        DateTime, default=datetime.utcnow, nullable=False, index=True
    )
    category: Mapped[str] = mapped_column(String(64), nullable=False, index=True)
    description: Mapped[Optional[str]] = mapped_column(Text)
    amount: Mapped[float] = mapped_column(Float, nullable=False)
    payment_method: Mapped[str] = mapped_column(String(32), default="Cash", nullable=False)
    notes: Mapped[Optional[str]] = mapped_column(Text)
    recorded_by: Mapped[Optional[int]] = mapped_column(
        ForeignKey("users.id", ondelete="SET NULL")
    )
    created_at: Mapped[datetime] = mapped_column(
        DateTime, default=datetime.utcnow, nullable=False
    )


# ---------------------------------------------------------------------------
# Owner withdrawals (do NOT reduce profit, but reduce available cash)
# ---------------------------------------------------------------------------

class OwnerWithdrawal(Base):
    __tablename__ = "owner_withdrawals"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    withdrawal_date: Mapped[datetime] = mapped_column(
        DateTime, default=datetime.utcnow, nullable=False, index=True
    )
    reason: Mapped[str] = mapped_column(String(64), nullable=False, index=True)
    amount: Mapped[float] = mapped_column(Float, nullable=False)
    taken_by: Mapped[Optional[str]] = mapped_column(String(120))
    approved_by: Mapped[Optional[int]] = mapped_column(
        ForeignKey("users.id", ondelete="SET NULL")
    )
    notes: Mapped[Optional[str]] = mapped_column(Text)
    created_at: Mapped[datetime] = mapped_column(
        DateTime, default=datetime.utcnow, nullable=False
    )


# ---------------------------------------------------------------------------
# Cash management
# ---------------------------------------------------------------------------

class CashSession(Base):
    """One row per business day. Created when the day is opened, finalised
    when closed."""
    __tablename__ = "cash_sessions"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    session_date: Mapped[date_] = mapped_column(Date, unique=True, nullable=False)
    opening_cash: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)
    is_closed: Mapped[bool] = mapped_column(Boolean, default=False, nullable=False)
    opened_by: Mapped[Optional[int]] = mapped_column(
        ForeignKey("users.id", ondelete="SET NULL")
    )
    opened_at: Mapped[datetime] = mapped_column(
        DateTime, default=datetime.utcnow, nullable=False
    )
    notes: Mapped[Optional[str]] = mapped_column(Text)


class DailyClosing(Base):
    """Immutable snapshot written when the day is closed. Used for audit and
    historical reporting."""
    __tablename__ = "daily_closings"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    session_date: Mapped[date_] = mapped_column(Date, unique=True, nullable=False, index=True)
    opening_cash: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)
    cash_sales: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)
    mobile_money_sales: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)
    bank_sales: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)
    business_expenses: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)
    owner_withdrawals: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)
    expected_cash: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)
    actual_cash: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)
    difference: Mapped[float] = mapped_column(Float, default=0.0, nullable=False)
    closed_by: Mapped[Optional[int]] = mapped_column(
        ForeignKey("users.id", ondelete="SET NULL")
    )
    closed_at: Mapped[datetime] = mapped_column(
        DateTime, default=datetime.utcnow, nullable=False
    )
    closing_notes: Mapped[Optional[str]] = mapped_column(Text)


# ---------------------------------------------------------------------------
# Audit logs
# ---------------------------------------------------------------------------

class AuditLog(Base):
    __tablename__ = "audit_logs"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    user_id: Mapped[Optional[int]] = mapped_column(
        ForeignKey("users.id", ondelete="SET NULL")
    )
    username: Mapped[Optional[str]] = mapped_column(String(64))
    action: Mapped[str] = mapped_column(String(32), nullable=False, index=True)
    module: Mapped[str] = mapped_column(String(64), nullable=False, index=True)
    description: Mapped[Optional[str]] = mapped_column(Text)
    created_at: Mapped[datetime] = mapped_column(
        DateTime, default=datetime.utcnow, nullable=False, index=True
    )


# ---------------------------------------------------------------------------
# Settings (key/value)
# ---------------------------------------------------------------------------

class Setting(Base):
    __tablename__ = "settings"

    key: Mapped[str] = mapped_column(String(64), primary_key=True)
    value: Mapped[Optional[str]] = mapped_column(Text)
    updated_at: Mapped[datetime] = mapped_column(
        DateTime, default=datetime.utcnow, onupdate=datetime.utcnow, nullable=False
    )
