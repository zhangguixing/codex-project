using BaiduShareTool.Application.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace BaiduShareTool.Infrastructure.Startup;

/// <summary>创建运行目录和 SQLite 数据库结构。</summary>
public sealed class AppStartupService(ILogger<AppStartupService> logger) : IAppStartupService
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(dataDirectory);
        Directory.CreateDirectory(Path.Combine(dataDirectory, "logs"));
        var databasePath = Path.Combine(dataDirectory, "app.db");
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS app_settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS tasks (
                task_id TEXT PRIMARY KEY,
                provider TEXT NOT NULL DEFAULT 'baidu',
                root_path TEXT NOT NULL,
                status TEXT NOT NULL,
                scanned_count INTEGER NOT NULL DEFAULT 0,
                success_count INTEGER NOT NULL DEFAULT 0,
                failed_count INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS task_items (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                task_id TEXT NOT NULL,
                file_id TEXT NOT NULL,
                file_path TEXT NOT NULL,
                file_name TEXT NOT NULL,
                file_size INTEGER NOT NULL DEFAULT 0,
                status TEXT NOT NULL,
                share_url TEXT,
                retry_count INTEGER NOT NULL DEFAULT 0,
                last_error TEXT,
                password_fingerprint TEXT NOT NULL DEFAULT '',
                password_verified INTEGER NOT NULL DEFAULT 0,
                updated_at TEXT NOT NULL,
                UNIQUE(task_id, file_id)
            );
            CREATE TABLE IF NOT EXISTS share_cache (
                provider TEXT NOT NULL,
                file_id TEXT NOT NULL,
                file_path TEXT NOT NULL,
                file_name TEXT NOT NULL,
                share_url TEXT,
                share_time TEXT,
                status TEXT NOT NULL,
                last_error TEXT,
                updated_at TEXT NOT NULL,
                PRIMARY KEY (provider, file_id)
            );
            CREATE TABLE IF NOT EXISTS export_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                task_id TEXT NOT NULL,
                export_time TEXT NOT NULL,
                root_path TEXT NOT NULL,
                file_count INTEGER NOT NULL,
                success_count INTEGER NOT NULL,
                failed_count INTEGER NOT NULL,
                file_path TEXT NOT NULL,
                status TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
        foreach (var migration in new[]
        {
            "ALTER TABLE tasks ADD COLUMN provider TEXT NOT NULL DEFAULT 'baidu'",
            "ALTER TABLE tasks ADD COLUMN scanned_count INTEGER NOT NULL DEFAULT 0",
            "ALTER TABLE tasks ADD COLUMN success_count INTEGER NOT NULL DEFAULT 0",
            "ALTER TABLE tasks ADD COLUMN failed_count INTEGER NOT NULL DEFAULT 0",
            "ALTER TABLE share_cache ADD COLUMN password_fingerprint TEXT NOT NULL DEFAULT ''",
            "ALTER TABLE share_cache ADD COLUMN password_verified INTEGER NOT NULL DEFAULT 0"
        })
        {
            try
            {
                await using var migrationCommand = connection.CreateCommand();
                migrationCommand.CommandText = migration;
                await migrationCommand.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqliteException exception) when (exception.SqliteErrorCode == 1) { }
        }
        logger.LogInformation("应用基础设施初始化完成，数据库路径：{DatabasePath}", databasePath);
    }
}
