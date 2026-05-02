# Brenda's Printing Hub - Business Management System

An offline standalone desktop application built with PySide6, SQLAlchemy, and SQLite.
Designed for a small printing business that handles computer work, fridge sales,
and labelling services.

## Features

- Login & user management (Admin / Manager / Worker roles, hashed passwords)
- Dashboard with KPI cards and charts
- Sales module (Computer / Fridge / Labelling)
- Stock management with low-stock alerts
- Labelling jobs with deposits, balances, and statuses
- Business expenses vs Owner withdrawals (separated for true profit)
- Daily cash session and end-of-day closing
- Powerful reports (Daily, Monthly, P&L, Stock, Sales-by-category, etc.)
- Export to PDF & Excel
- Backup & Restore
- Audit logs
- Currency: Uganda Shillings (UGX)

## Default Login

```
Username: admin
Password: admin123
```

You will be prompted to change the password on first login.

## Running locally (development)

```bash
# 1. Create a virtual environment
python -m venv .venv

# Windows
.venv\Scripts\activate
# macOS / Linux
source .venv/bin/activate

# 2. Install dependencies
pip install -r requirements.txt

# 3. Run the app
python main.py
```

The SQLite database is created automatically at `data/brendas_hub.db` on first
run. Default services, products, and the admin user are seeded.

## Packaging into a Windows .exe with PyInstaller

From the project root on a Windows machine:

```bash
pip install pyinstaller
pyinstaller --noconfirm --windowed --name "BrendasPrintingHub" ^
    --add-data "app/assets;app/assets" ^
    --icon "app/assets/icons/app.ico" ^
    main.py
```

The packaged app appears in `dist/BrendasPrintingHub/`. Ship the entire folder
to the client (or wrap it with Inno Setup for a single installer).

After packaging, the app will create `data/`, `backups/`, and
`exported_reports/` folders next to the executable on first launch.

## Project structure

```
brendas_printing_hub/
├── main.py
├── requirements.txt
├── config/
├── app/
│   ├── core/         # auth, security, helpers, constants
│   ├── database/     # models, session, migrations, seed
│   ├── services/     # business logic (sales, stock, reports, ...)
│   ├── ui/           # PySide6 windows and pages
│   ├── reports/      # PDF + Excel generators
│   └── assets/       # icons, QSS stylesheet
├── data/             # SQLite database (auto-created)
├── backups/          # Manual + automatic DB backups
└── exported_reports/ # PDF / Excel exports
```
