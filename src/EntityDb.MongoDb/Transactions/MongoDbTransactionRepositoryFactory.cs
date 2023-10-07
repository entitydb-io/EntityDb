using EntityDb.Abstractions.Transactions;
using EntityDb.Common.Disposables;
using EntityDb.Common.Envelopes;
using EntityDb.Common.Transactions;
using EntityDb.MongoDb.Sessions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EntityDb.MongoDb.Transactions;

internal class MongoDbTransactionRepositoryFactory : DisposableResourceBaseClass, IMongoDbTransactionRepositoryFactory
{
    private readonly IEnvelopeService<BsonDocument> _envelopeService;
    private readonly IOptionsFactory<MongoDbTransactionSessionOptions> _optionsFactory;
    private readonly IServiceProvider _serviceProvider;

    public MongoDbTransactionRepositoryFactory
    (
        IServiceProvider serviceProvider,
        IOptionsFactory<MongoDbTransactionSessionOptions> optionsFactory,
        IEnvelopeService<BsonDocument> envelopeService
    )
    {
        _serviceProvider = serviceProvider;
        _optionsFactory = optionsFactory;
        _envelopeService = envelopeService;
    }

    public MongoDbTransactionSessionOptions GetTransactionSessionOptions(string transactionSessionOptionsName)
    {
        return _optionsFactory.Create(transactionSessionOptionsName);
    }

    public async Task<IMongoSession> CreateSession(MongoDbTransactionSessionOptions options,
        CancellationToken cancellationToken)
    {
        var mongoClient = new MongoClient(options.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(options.DatabaseName);

        var clientSessionHandle =
            await mongoClient.StartSessionAsync(new ClientSessionOptions { CausalConsistency = true },
                cancellationToken);



        return MongoSession.Create
        (
            _serviceProvider,
            mongoDatabase,
            clientSessionHandle,
            options
        );
    }

    public ITransactionRepository CreateRepository
    (
        IMongoSession mongoSession
    )
    {
        var mongoDbTransactionRepository = new MongoDbTransactionRepository
        (
            mongoSession,
            _envelopeService
        );

        return TryCatchTransactionRepository.Create(_serviceProvider, mongoDbTransactionRepository);
    }
}
