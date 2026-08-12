namespace InventoryAlert.Domain.Configuration;

public class SharedDatabaseSettings
{
    private string _defaultConnection = string.Empty;

    public string DefaultConnection
    {
        get => _defaultConnection;
        set => _defaultConnection = value
            .Replace("\r", "")
            .Replace("\n", " ")
            .Trim();
    }
}

