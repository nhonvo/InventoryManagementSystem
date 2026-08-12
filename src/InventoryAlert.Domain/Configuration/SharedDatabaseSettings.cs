namespace InventoryAlert.Domain.Configuration;

public class SharedDatabaseSettings
{
    private string _defaultConnection = string.Empty;

    public string DefaultConnection
    {
        get => _defaultConnection;
        set => _defaultConnection = NormalizeConnectionString(value);
    }

    private static string NormalizeConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return string.Empty;

        // 1. Collapse all multi-spaces, newlines, tabs into single spaces
        var cleaned = System.Text.RegularExpressions.Regex.Replace(connectionString, @"\s+", " ").Trim();

        // 2. Normalize Npgsql parameter keys without internal spaces
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"Trust\s+Server\s+Certificate", "TrustServerCertificate", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"SSL\s+Mode", "SSLMode", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // 3. Handle URI format postgresql://...
        if (cleaned.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
            cleaned.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(cleaned);
                var userInfo = uri.UserInfo.Split(':', 2);
                var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
                var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 5432;
                var dbName = uri.AbsolutePath.TrimStart('/');

                var sslMode = "Require";
                if (!string.IsNullOrEmpty(uri.Query))
                {
                    var query = uri.Query.TrimStart('?');
                    foreach (var part in query.Split('&'))
                    {
                        var kv = part.Split('=', 2);
                        if (kv.Length == 2 && kv[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                        {
                            sslMode = kv[1].Equals("require", StringComparison.OrdinalIgnoreCase) ? "Require" : kv[1];
                        }
                    }
                }

                return $"Host={host};Port={port};Database={dbName};Username={username};Password={password};SSLMode={sslMode};TrustServerCertificate=true";
            }
            catch
            {
                // Fallback to cleaned if parsing fails
            }
        }

        return cleaned;
    }
}

