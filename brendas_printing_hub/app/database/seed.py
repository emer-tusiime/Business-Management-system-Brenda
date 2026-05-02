"""
Seeds the database with default data on first run.

Only inserts records that don't already exist so re-running the app is safe.
"""
from __future__ import annotations

from sqlalchemy import select

from app.core.constants import (
    PRODUCT_CATEGORY_JUICE,
    PRODUCT_CATEGORY_SODA,
    PRODUCT_CATEGORY_WATER,
    ROLE_ADMIN,
    SERVICE_UNIT_ITEM,
    SERVICE_UNIT_PAGE,
)
from app.core.security import hash_password
from app.database.models import (
    Product,
    Service,
    ServiceCategory,
    Setting,
    User,
)
from app.database.session import session_scope

DEFAULT_ADMIN_USERNAME = "admin"
DEFAULT_ADMIN_PASSWORD = "admin123"


# (category_name, service_name, unit_type, default_price)
DEFAULT_SERVICES = [
    ("Typing", "Typing", SERVICE_UNIT_PAGE, 1000),
    ("Printing", "Printing B/W", SERVICE_UNIT_PAGE, 200),
    ("Printing", "Printing Color", SERVICE_UNIT_PAGE, 1000),
    ("Photocopying", "Photocopying", SERVICE_UNIT_PAGE, 200),
    ("Laminating", "Laminating A4", SERVICE_UNIT_ITEM, 2000),
    ("Binding", "Binding", SERVICE_UNIT_ITEM, 3000),
    ("Scanning", "Scanning", SERVICE_UNIT_PAGE, 500),
    ("Designing", "Designing", SERVICE_UNIT_ITEM, 10000),
    ("Photos", "Passport Photo Printing", SERVICE_UNIT_ITEM, 5000),
    ("Editing", "Document Editing", SERVICE_UNIT_PAGE, 1000),
]

# (name, brand, category, buying, selling, opening_stock, threshold)
DEFAULT_PRODUCTS = [
    ("Ice Water 500ml", "Hub", PRODUCT_CATEGORY_WATER, 200, 500, 0, 5),
    ("Rwenzori Water 500ml", "Rwenzori", PRODUCT_CATEGORY_WATER, 800, 1500, 0, 5),
    ("Soda 500ml", "Coca-Cola", PRODUCT_CATEGORY_SODA, 1500, 2500, 0, 5),
    ("Soda 300ml", "Coca-Cola", PRODUCT_CATEGORY_SODA, 1000, 1500, 0, 5),
    ("Juice 500ml", "Splash", PRODUCT_CATEGORY_JUICE, 1500, 2500, 0, 5),
]


def seed_initial_data() -> None:
    with session_scope() as session:
        # Default admin user
        existing_admin = session.execute(
            select(User).where(User.username == DEFAULT_ADMIN_USERNAME)
        ).scalar_one_or_none()
        if existing_admin is None:
            session.add(
                User(
                    username=DEFAULT_ADMIN_USERNAME,
                    full_name="System Administrator",
                    password_hash=hash_password(DEFAULT_ADMIN_PASSWORD),
                    role=ROLE_ADMIN,
                    is_active=True,
                    must_change_password=True,
                )
            )

        # Service categories + services
        category_cache: dict[str, ServiceCategory] = {}
        for cat_name, svc_name, unit, price in DEFAULT_SERVICES:
            cat = category_cache.get(cat_name)
            if cat is None:
                cat = session.execute(
                    select(ServiceCategory).where(ServiceCategory.name == cat_name)
                ).scalar_one_or_none()
                if cat is None:
                    cat = ServiceCategory(name=cat_name)
                    session.add(cat)
                    session.flush()
                category_cache[cat_name] = cat

            existing_svc = session.execute(
                select(Service).where(Service.name == svc_name)
            ).scalar_one_or_none()
            if existing_svc is None:
                session.add(
                    Service(
                        name=svc_name,
                        category_id=cat.id,
                        unit_type=unit,
                        default_price=float(price),
                        is_active=True,
                    )
                )

        # Default fridge products (zero opening stock - the owner restocks)
        for name, brand, cat, buy, sell, qty, threshold in DEFAULT_PRODUCTS:
            existing = session.execute(
                select(Product).where(Product.name == name, Product.brand == brand)
            ).scalar_one_or_none()
            if existing is None:
                session.add(
                    Product(
                        name=name,
                        brand=brand,
                        category=cat,
                        buying_price=float(buy),
                        selling_price=float(sell),
                        current_stock=int(qty),
                        low_stock_threshold=int(threshold),
                        is_active=True,
                    )
                )

        # Default settings
        defaults = {
            "business_name": "Brenda's Printing Hub",
            "business_phone": "",
            "business_location": "",
            "currency": "UGX",
            "default_opening_cash": "0",
            "low_stock_threshold": "5",
            "report_header": "Brenda's Printing Hub - Business Report",
            "backup_location": "",
        }
        for key, value in defaults.items():
            existing = session.get(Setting, key)
            if existing is None:
                session.add(Setting(key=key, value=value))
