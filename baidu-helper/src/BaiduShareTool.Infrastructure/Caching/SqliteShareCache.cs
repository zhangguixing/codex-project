using BaiduShareTool.Application.Caching;
using Microsoft.Data.Sqlite;

namespace BaiduShareTool.Infrastructure.Caching;

/// <summary>SQLite 分享缓存实现。</summary>
public sealed class SqliteShareCache : IShareCache
{
    private readonly string connectionString = $"Data Source={Path.Combine(AppContext.BaseDirectory, "data", "app.db")}";

    public async Task<ShareCacheEntry?> FindAsync(string providerId, string fileId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT provider, file_id, file_path, file_name, share_url, share_time, status, last_error, password_fingerprint, password_verified FROM share_cache WHERE provider = $provider AND file_id = $file_id LIMIT 1";
        command.Parameters.AddWithValue("$provider", providerId);
        command.Parameters.AddWithValue("$file_id", fileId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new ShareCacheEntry(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4), ParseDate(reader, 5), reader.GetString(6), reader.IsDBNull(7) ? null : reader.GetString(7), reader.IsDBNull(8) ? string.Empty : reader.GetString(8), !reader.IsDBNull(9) && reader.GetInt32(9) == 1);
    }

    public async Task SaveAsync(ShareCacheEntry entry, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO share_cache (provider, file_id, file_path, file_name, share_url, share_time, status, last_error, password_fingerprint, password_verified, updated_at)
            VALUES ($provider, $file_id, $file_path, $file_name, $share_url, $share_time, $status, $last_error, $password_fingerprint, $password_verified, $updated_at)
            ON CONFLICT(provider, file_id) DO UPDATE SET file_path = excluded.file_path, file_name = excluded.file_name, share_url = excluded.share_url, share_time = excluded.share_time, status = excluded.status, last_error = excluded.last_error, password_fingerprint = excluded.password_fingerprint, password_verified = excluded.password_verified, updated_at = excluded.updated_at;
            """;
        command.Parameters.AddWithValue("$provider", entry.ProviderId);
        command.Parameters.AddWithValue("$file_id", entry.FileId);
        command.Parameters.AddWithValue("$file_path", entry.FilePath);
        command.Parameters.AddWithValue("$file_name", entry.FileName);
        command.Parameters.AddWithValue("$share_url", (object?)entry.ShareUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("$share_time", entry.ShareTime?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$status", entry.Status);
        command.Parameters.AddWithValue("$last_error", (object?)entry.LastError ?? DBNull.Value);
        command.Parameters.AddWithValue("$password_fingerprint", entry.PasswordFingerprint);
        command.Parameters.AddWithValue("$password_verified", entry.PasswordVerified ? 1 : 0);
        command.Parameters.AddWithValue("$updated_at", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static DateTimeOffset? ParseDate(SqliteDataReader reader, int index)
        => reader.IsDBNull(index) ? null : DateTimeOffset.TryParse(reader.GetString(index), out var value) ? value : null;
}
