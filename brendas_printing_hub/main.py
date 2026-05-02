"""
Brenda's Printing Hub - Offline Business Management System
Entry point.
"""
from __future__ import annotations

import sys
from pathlib import Path

from PySide6.QtCore import Qt
from PySide6.QtGui import QFont, QIcon
from PySide6.QtWidgets import QApplication

# Ensure project root is on sys.path so `app...` and `config...` imports work
# whether we run from source or from a PyInstaller bundle.
ROOT = Path(__file__).resolve().parent
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from config.settings import APP_NAME, ASSETS_DIR, ensure_runtime_directories
from app.database.session import init_database
from app.database.seed import seed_initial_data
from app.ui.login_window import LoginWindow


def load_stylesheet() -> str:
    qss_path = ASSETS_DIR / "styles" / "app.qss"
    if qss_path.exists():
        return qss_path.read_text(encoding="utf-8")
    return ""


def main() -> int:
    # 1. Make sure the runtime folders exist (data/, backups/, exported_reports/)
    ensure_runtime_directories()

    # 2. Create QApplication
    QApplication.setHighDpiScaleFactorRoundingPolicy(
        Qt.HighDpiScaleFactorRoundingPolicy.PassThrough
    )
    app = QApplication(sys.argv)
    app.setApplicationName(APP_NAME)
    app.setOrganizationName("Brenda's Printing Hub")

    # System font fallback - Segoe UI on Windows, otherwise default sans-serif
    app.setFont(QFont("Segoe UI", 10))

    # Application icon (optional - file may not exist on first run)
    icon_path = ASSETS_DIR / "icons" / "app.png"
    if icon_path.exists():
        app.setWindowIcon(QIcon(str(icon_path)))

    # 3. Apply the global stylesheet
    qss = load_stylesheet()
    if qss:
        app.setStyleSheet(qss)

    # 4. Initialize the database (creates tables if missing, enables WAL)
    init_database()

    # 5. Seed default admin user, services, and products if database is empty
    seed_initial_data()

    # 6. Show the login window. The login window is responsible for opening
    #    the main window after a successful authentication.
    login = LoginWindow()
    login.show()

    return app.exec()


if __name__ == "__main__":
    sys.exit(main())
