using EntityDb.Abstractions.Transactions;
using EntityDb.Abstractions.Transactions.Builders;
using EntityDb.Abstractions.ValueObjects;
using EntityDb.Common.Entities;

namespace EntityDb.Common.Transactions.Builders;

internal sealed class SingleEntityTransactionBuilder<TEntity> : ISingleEntityTransactionBuilder<TEntity>
    where TEntity : IEntity<TEntity>
{
    private readonly ITransactionBuilder<TEntity> _transactionBuilder;

    internal SingleEntityTransactionBuilder(ITransactionBuilder<TEntity> transactionBuilder, Id entityId)
    {
        _transactionBuilder = transactionBuilder;
        EntityId = entityId;
    }

    public Id EntityId { get; }

    public TEntity GetEntity()
    {
        return _transactionBuilder.GetEntity(EntityId);
    }

    public bool IsEntityKnown()
    {
        return _transactionBuilder.IsEntityKnown(EntityId);
    }

    public ISingleEntityTransactionBuilder<TEntity> Load(TEntity entity)
    {
        _transactionBuilder.Load(EntityId, entity);

        return this;
    }

    public ISingleEntityTransactionBuilder<TEntity> Append(object command)
    {
        _transactionBuilder.Append(EntityId, command);

        return this;
    }

    public ITransaction Build(Id transactionId)
    {
        return _transactionBuilder.Build(transactionId);
    }
}
