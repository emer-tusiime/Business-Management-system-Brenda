# Business Manager - Stationery & Retail Management System

A comprehensive offline Windows desktop application for managing stationery and small retail business operations.

## Features

### Core Business Operations
- **Sales Management**: Complete point-of-sale system with receipt generation
- **Expense Tracking**: Categorized expense management and reporting
- **Inventory Management**: Stock tracking with low-level alerts
- **Financial Reporting**: Daily, monthly, and custom date range reports
- **User Management**: Role-based access control (Admin/Attendant)

### Business Modules Supported
- Typing services
- Printing (Black & Color)
- Photocopying (Standard & Premium)
- Document binding
- Document sealing
- Labelling services
- Email creation
- Passport applications
- Branding services
- Fridge/Drinks sales

### Technical Features
- **Offline-First**: Works completely offline on a single Windows computer
- **Modern UI**: Material Design with WPF and clean interface
- **Secure**: User authentication with role-based permissions
- **Scalable**: Clean Architecture with Entity Framework Core
- **Reporting**: QuestPDF for professional report generation
- **Charts**: LiveCharts2 for data visualization
- **Backup/Restore**: Manual database backup and restore functionality

## Technology Stack

- **Language**: C# (.NET 8)
- **Framework**: WPF with MVVM pattern
- **Architecture**: Clean Architecture (Domain, Application, Infrastructure, UI)
- **Database**: MySQL with Entity Framework Core (Pomelo provider)
- **UI Framework**: Material Design in XAML
- **Charts**: LiveCharts2
- **Reports**: QuestPDF
- **Validation**: FluentValidation
- **DI**: Microsoft.Extensions.DependencyInjection

## Prerequisites

1. **.NET 8 Runtime** - Download from [Microsoft](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **MySQL Server** - Version 8.0 or higher
3. **Windows OS** - Windows 10 or higher

## Installation

### 1. Database Setup

1. Install MySQL Server on your machine
2. Create a database named `businessmanager_db`
3. Create a MySQL user with appropriate privileges

```sql
CREATE DATABASE businessmanager_db;
CREATE USER 'businessmanager'@'localhost' IDENTIFIED BY 'your_password';
GRANT ALL PRIVILEGES ON businessmanager_db.* TO 'businessmanager'@'localhost';
FLUSH PRIVILEGES;
```

### 2. Application Setup

1. Clone or download the application files
2. Navigate to the application directory
3. Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=businessmanager_db;user=businessmanager;password=your_password;"
  }
}
```

4. Run the application - it will automatically create the database schema and seed initial data

### 3. First Login

- **Username**: `admin`
- **Password**: `Admin123`

> **Important**: Change the default admin password after first login

## Default Configuration

### Default Prices (Editable in Settings)
- Photocopy Standard: 200
- Photocopy Premium: 500
- Printing Black: 500
- Printing Color: 1000
- Water Ice Big: 1000
- Water Ice Small: 500
- Soda: 1000
- Rwenzori Water Big: 2000
- Rwenzori Water Small: 1000
- Sealing: 1500
- Labelling: 5000 (starting price)
- Binding: Flexible pricing
- Branding: Flexible pricing
- Passport Application: Flexible pricing
- Email Creation: Flexible pricing
- Typing: Flexible pricing

### Default Inventory Items
- Water products (various sizes)
- Soda
- Stationery items (paper, ink, binding materials, etc.)

## Usage Guide

### Dashboard
- View today's and this month's income, expenses, and profit
- Monitor low stock alerts
- Access quick action buttons for common tasks
- View income by module and monthly trend charts

### Sales Module
- Create new sales with items from different categories
- Support for both fixed-price and flexible-price services
- Automatic receipt number generation
- Daily sales history with filtering options

### Expense Management
- Add expenses with categories
- Track expenses by date range and category
- Monthly and daily expense summaries

### Inventory Management
- Track stock levels for all products
- Add/remove stock with reason tracking
- Low stock alerts and reorder level management
- Inventory movement history

### Reports
- Daily income/expense/profit reports
- Monthly financial summaries
- Income by service/module
- Expense by category
- Export to PDF functionality

### Settings
- Configure business information
- Edit service and product prices
- Set backup preferences
- Customize receipt templates

## Backup and Restore

### Creating Backups
1. Navigate to Backup & Restore module
2. Click "Create Backup"
3. Choose backup location
4. Backup file will be saved with timestamp

### Restoring Backups
1. Navigate to Backup & Restore module
2. Click "Restore Backup"
3. Select backup file
4. Confirm restore operation

> **Warning**: Restore operation will replace all current data

## Security

- User authentication with encrypted passwords
- Role-based access control
- Audit logging for important operations
- Local database storage (no cloud exposure)

## Troubleshooting

### Database Connection Issues
1. Verify MySQL Server is running
2. Check connection string in appsettings.json
3. Ensure database user has proper permissions
4. Verify firewall settings for MySQL port (3306)

### Application Won't Start
1. Ensure .NET 8 Runtime is installed
2. Check application logs in the `logs` folder
3. Verify all required files are present

### Performance Issues
1. Regular database maintenance (optimize tables)
2. Clean up old audit logs periodically
3. Keep backup files organized

## Development

### Building from Source
```bash
dotnet build
dotnet run --project src/BusinessManager.App
```

### Database Migrations
```bash
dotnet ef migrations add MigrationName --project src/BusinessManager.Infrastructure
dotnet ef database update --project src/BusinessManager.Infrastructure
```

## Support

For technical support and questions:
- Check the troubleshooting section above
- Review application logs for error details
- Contact system administrator

## License

This software is proprietary and licensed for single-business use only.

## Version History

### v1.0.0 (Current)
- Initial release with core business management features
- Sales, expense, and inventory management
- Reporting and analytics
- User management and security
- Backup and restore functionality
