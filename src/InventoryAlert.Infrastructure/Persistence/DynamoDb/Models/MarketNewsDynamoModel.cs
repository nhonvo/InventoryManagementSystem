using Amazon.DynamoDBv2.DataModel;
using InventoryAlert.Domain.Entities.Dynamodb;

namespace InventoryAlert.Infrastructure.Persistence.DynamoDb.Models;

[DynamoDBTable("inventoryalert-market-news")]
public class MarketNewsDynamoModel : MarketNewsDynamoEntry
{
    [DynamoDBHashKey]
    public new string PK { get => base.PK; set => base.PK = value; }

    [DynamoDBRangeKey]
    public new string SK { get => base.SK; set => base.SK = value; }
}
