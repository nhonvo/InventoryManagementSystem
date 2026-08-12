using Amazon.DynamoDBv2.DataModel;
using InventoryAlert.Domain.Entities.Dynamodb;

namespace InventoryAlert.Infrastructure.Persistence.DynamoDb.Models;

[DynamoDBTable("inventoryalert-company-news")]
public class CompanyNewsDynamoModel : CompanyNewsDynamoEntry
{
    [DynamoDBHashKey]
    public new string PK { get => base.PK; set => base.PK = value; }

    [DynamoDBRangeKey]
    public new string SK { get => base.SK; set => base.SK = value; }
}
