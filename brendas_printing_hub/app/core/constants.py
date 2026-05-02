"""
Domain constants - single source of truth for enum-like string values used
throughout the database, services, and UI. Kept as plain strings (not Python
enums) to keep SQLAlchemy filter expressions and PySide6 combo boxes simple.
"""
from __future__ import annotations

# ---------------------------------------------------------------------------
# Roles
# ---------------------------------------------------------------------------

ROLE_ADMIN = "Admin"
ROLE_MANAGER = "Manager"
ROLE_WORKER = "Worker"
ALL_ROLES = (ROLE_ADMIN, ROLE_MANAGER, ROLE_WORKER)

# Module access matrix - which roles can OPEN which page.
# Delete actions are guarded separately and always require ROLE_ADMIN.
MODULE_ACCESS = {
    "dashboard": ALL_ROLES,
    "sales": ALL_ROLES,
    "services": (ROLE_ADMIN, ROLE_MANAGER),
    "products": (ROLE_ADMIN, ROLE_MANAGER),
    "labelling": ALL_ROLES,
    "expenses": (ROLE_ADMIN, ROLE_MANAGER),
    "withdrawals": (ROLE_ADMIN, ROLE_MANAGER),
    "cash": (ROLE_ADMIN, ROLE_MANAGER),
    "reports": (ROLE_ADMIN, ROLE_MANAGER),
    "settings": (ROLE_ADMIN,),
    "audit": (ROLE_ADMIN,),
}

# ---------------------------------------------------------------------------
# Sales / payments
# ---------------------------------------------------------------------------

SALE_CATEGORY_COMPUTER = "Computer Work"
SALE_CATEGORY_FRIDGE = "Fridge"
SALE_CATEGORY_LABELLING = "Labelling"
SALE_CATEGORIES = (
    SALE_CATEGORY_COMPUTER,
    SALE_CATEGORY_FRIDGE,
    SALE_CATEGORY_LABELLING,
)

PAYMENT_CASH = "Cash"
PAYMENT_MOBILE_MONEY = "Mobile Money"
PAYMENT_BANK = "Bank"
PAYMENT_MIXED = "Mixed"
PAYMENT_METHODS = (PAYMENT_CASH, PAYMENT_MOBILE_MONEY, PAYMENT_BANK, PAYMENT_MIXED)

# ---------------------------------------------------------------------------
# Services / products
# ---------------------------------------------------------------------------

SERVICE_UNIT_PAGE = "per page"
SERVICE_UNIT_ITEM = "per item"
SERVICE_UNIT_BOOK = "per book"
SERVICE_UNIT_HOUR = "per hour"
SERVICE_UNITS = (SERVICE_UNIT_PAGE, SERVICE_UNIT_ITEM, SERVICE_UNIT_BOOK, SERVICE_UNIT_HOUR)

PRODUCT_CATEGORY_WATER = "Water"
PRODUCT_CATEGORY_SODA = "Soda"
PRODUCT_CATEGORY_JUICE = "Juice"
PRODUCT_CATEGORY_OTHER = "Other"
PRODUCT_CATEGORIES = (
    PRODUCT_CATEGORY_WATER,
    PRODUCT_CATEGORY_SODA,
    PRODUCT_CATEGORY_JUICE,
    PRODUCT_CATEGORY_OTHER,
)

STOCK_MOVEMENT_PURCHASE = "Purchase"
STOCK_MOVEMENT_SALE = "Sale"
STOCK_MOVEMENT_ADJUSTMENT = "Adjustment"
STOCK_MOVEMENT_OPENING = "Opening"
STOCK_MOVEMENT_TYPES = (
    STOCK_MOVEMENT_PURCHASE,
    STOCK_MOVEMENT_SALE,
    STOCK_MOVEMENT_ADJUSTMENT,
    STOCK_MOVEMENT_OPENING,
)

# ---------------------------------------------------------------------------
# Labelling
# ---------------------------------------------------------------------------

JOB_STATUS_PENDING = "Pending"
JOB_STATUS_IN_PROGRESS = "In Progress"
JOB_STATUS_COMPLETED = "Completed"
JOB_STATUS_COLLECTED = "Collected"
JOB_STATUS_CANCELLED = "Cancelled"
JOB_STATUSES = (
    JOB_STATUS_PENDING,
    JOB_STATUS_IN_PROGRESS,
    JOB_STATUS_COMPLETED,
    JOB_STATUS_COLLECTED,
    JOB_STATUS_CANCELLED,
)

PAY_STATUS_UNPAID = "Unpaid"
PAY_STATUS_PARTIAL = "Partially Paid"
PAY_STATUS_PAID = "Fully Paid"
PAY_STATUSES = (PAY_STATUS_UNPAID, PAY_STATUS_PARTIAL, PAY_STATUS_PAID)

LABELLING_ITEM_TYPES = (
    "Jersey",
    "Shirt",
    "School Uniform",
    "Custom Clothing",
    "Bag",
    "Other",
)

# ---------------------------------------------------------------------------
# Expense / withdrawal categories
# ---------------------------------------------------------------------------

EXPENSE_CATEGORIES = (
    "Paper",
    "Ink/Toner",
    "Electricity",
    "Rent",
    "Internet",
    "Repairs",
    "Staff Salary",
    "Stock Purchase",
    "Transport",
    "Other",
)

WITHDRAWAL_REASONS = (
    "Home affairs",
    "Family support",
    "Personal transport",
    "School fees",
    "Money taken home",
    "Emergency",
    "Other",
)

# ---------------------------------------------------------------------------
# Audit log actions
# ---------------------------------------------------------------------------

AUDIT_LOGIN = "LOGIN"
AUDIT_LOGOUT = "LOGOUT"
AUDIT_CREATE = "CREATE"
AUDIT_UPDATE = "UPDATE"
AUDIT_DELETE = "DELETE"
AUDIT_BACKUP = "BACKUP"
AUDIT_RESTORE = "RESTORE"
AUDIT_CLOSE_DAY = "CLOSE_DAY"
AUDIT_STOCK_ADJUST = "STOCK_ADJUST"
