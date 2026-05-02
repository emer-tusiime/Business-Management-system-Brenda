"""
Application-wide settings and runtime paths.

When packaged with PyInstaller, sys.frozen is set and we pivot the runtime
directories (data/, backups/, exported_reports/) to live next to the
executable instead of inside the temporary _MEIPASS bundle, so user data
survives upgrades and reinstalls.
"""
from __future__ import annotations

import os
import sys
from pathlib import Path

APP_NAME = "Brenda's Printing Hub"
APP_VERSION = "1.0.0"
CURRENCY_CODE = "UGX"

# ---------------------------------------------------------------------------
# Path resolution
# ---------------------------------------------------------------------------

def _runtime_root() -> Path:
    """Return the root directory used for *user* data (db, backups, exports)."""
    if getattr(sys, "frozen", False):
        # Packaged with PyInstaller -> sit next to the .exe
        return Path(sys.executable).resolve().parent
    return Path(__file__).resolve().parent.parent


def _bundle_root() -> Path:
    """Return the root used for *read-only* assets bundled with the app."""
    if getattr(sys, "frozen", False) and hasattr(sys, "_MEIPASS"):
        return Path(sys._MEIPASS)  # type: ignore[attr-defined]
    return Path(__file__).resolve().parent.parent


PROJECT_ROOT = _runtime_root()
BUNDLE_ROOT = _bundle_root()

DATA_DIR = PROJECT_ROOT / "data"
BACKUPS_DIR = PROJECT_ROOT / "backups"
EXPORTS_DIR = PROJECT_ROOT / "exported_reports"
ASSETS_DIR = BUNDLE_ROOT / "app" / "assets"

DATABASE_PATH = DATA_DIR / "brendas_hub.db"
DATABASE_URL = f"sqlite:///{DATABASE_PATH.as_posix()}"

# ---------------------------------------------------------------------------
# Defaults
# ---------------------------------------------------------------------------

DEFAULT_OPENING_CASH = 0
DEFAULT_LOW_STOCK_THRESHOLD = 5
DEFAULT_BUSINESS_PHONE = ""
DEFAULT_BUSINESS_LOCATION = ""

# Auto backup: warn if no backup happened in the last N days
BACKUP_WARNING_DAYS = 7


def ensure_runtime_directories() -> None:
    """Make sure all writable runtime folders exist."""
    for d in (DATA_DIR, BACKUPS_DIR, EXPORTS_DIR):
        d.mkdir(parents=True, exist_ok=True)
