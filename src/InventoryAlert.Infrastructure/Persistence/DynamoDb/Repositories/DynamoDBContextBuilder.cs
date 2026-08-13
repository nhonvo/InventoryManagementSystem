using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

namespace InventoryAlert.Infrastructure.Persistence.DynamoDb.Repositories;

public class DynamoDBContextBuilder
{
    private Func<IAmazonDynamoDB>? _clientFactory;
    private DynamoDBContextConfig _config = new();

    public DynamoDBContextBuilder WithDynamoDBClient(Func<IAmazonDynamoDB> clientFactory)
    {
        _clientFactory = clientFactory;
        return this;
    }

    public DynamoDBContextBuilder WithDynamoDBClient(IAmazonDynamoDB client)
    {
        _clientFactory = () => client;
        return this;
    }

    public DynamoDBContextBuilder WithConfig(DynamoDBContextConfig config)
    {
        _config = config;
        return this;
    }

    public IDynamoDBContext Build()
    {
        if (_clientFactory == null)
        {
            throw new InvalidOperationException("DynamoDB client must be configured.");
        }

#pragma warning disable CS0618
        return new DynamoDBContext(_clientFactory(), _config);
#pragma warning restore CS0618
    }
}
