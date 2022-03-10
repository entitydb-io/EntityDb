using EntityDb.Common.Transactions;
using EntityDb.MongoDb.Provisioner.Extensions;
using EntityDb.MongoDb.Sessions;
using EntityDb.MongoDb.Transactions;
using System.Threading.Tasks;

namespace EntityDb.MongoDb.Provisioner.Transactions;

internal sealed class
    AutoProvisionMongoDbTransactionRepositoryFactory : MongoDbTransactionRepositoryFactoryWrapper
{
    private static readonly object Lock = new();
    private static Task? _provisionTask;
    private static bool _provisioned;

    public AutoProvisionMongoDbTransactionRepositoryFactory(
        IMongoDbTransactionRepositoryFactory mongoDbTransactionRepositoryFactory) : base(
        mongoDbTransactionRepositoryFactory)
    {
    }

    public override async Task<IMongoSession> CreateSession(TransactionSessionOptions transactionSessionOptions)
    {
        var mongoSession = await base.CreateSession(transactionSessionOptions);

        if (_provisioned)
        {
            return mongoSession;
        }

        lock (Lock)
        {
            _provisionTask ??=
                mongoSession.MongoDatabase.Client.ProvisionCollections(mongoSession.MongoDatabase.DatabaseNamespace
                    .DatabaseName);
        }

        await _provisionTask;
        
        _provisioned = true;

        return mongoSession;
    }
}
