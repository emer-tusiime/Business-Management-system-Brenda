using System;
using System.IO;

namespace BusinessManager.Infrastructure.Data;

public static class DatabasePathHelper
{
    public const string AppFolderName = "AlindaBrenda";
    public const string DatabaseFileName = "business.db";

    public static string GetDatabaseDirectory()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFolderName);
        Directory.CreateDirectory(folder);
        return folder;
    }

    public static string GetDatabasePath()
    {
        return Path.Combine(GetDatabaseDirectory(), DatabaseFileName);
    }

    public static string GetConnectionString()
    {
        return $"Data Source={GetDatabasePath()}";
    }
}
