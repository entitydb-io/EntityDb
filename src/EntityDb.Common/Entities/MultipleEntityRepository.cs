﻿using EntityDb.Abstractions;
using EntityDb.Abstractions.Entities;
using EntityDb.Abstractions.Sources;
using EntityDb.Abstractions.Sources.Agents;
using EntityDb.Abstractions.States;
using EntityDb.Common.Disposables;
using EntityDb.Common.Exceptions;
using EntityDb.Common.Sources.Queries.Standard;

namespace EntityDb.Common.Entities;

internal sealed class MultipleEntityRepository<TEntity> : DisposableResourceBaseClass,
    IMultipleEntityRepository<TEntity>
    where TEntity : IEntity<TEntity>
{
    private readonly IAgentAccessor _agentAccessor;
    private readonly string _agentSignatureOptionsName;
    private readonly Dictionary<Id, TEntity> _knownEntities = new();
    private readonly List<Message> _messages = new();

    public MultipleEntityRepository
    (
        IAgentAccessor agentAccessor,
        string agentSignatureOptionsName,
        ISourceRepository sourceRepository,
        IStateRepository<TEntity>? stateRepository = null
    )
    {
        _agentSignatureOptionsName = agentSignatureOptionsName;
        _agentAccessor = agentAccessor;

        SourceRepository = sourceRepository;
        StateRepository = stateRepository;
    }

    public ISourceRepository SourceRepository { get; }
    public IStateRepository<TEntity>? StateRepository { get; }

    public void Create(Id entityId)
    {
        if (_knownEntities.ContainsKey(entityId))
        {
            throw new ExistingEntityException();
        }

        var entity = TEntity.Construct(entityId);

        _knownEntities.Add(entityId, entity);
    }

    public async Task<bool> TryLoad(StatePointer entityPointer, CancellationToken cancellationToken = default)
    {
        var entityId = entityPointer.Id;
        
        if (_knownEntities.TryGetValue(entityId, out var entity))
        {
            var knownEntityPointer = entity.GetPointer();
            
            if (entityPointer.IsSatisfiedBy(knownEntityPointer))
            {
                return true;
            }

            if (entityPointer.StateVersion.Value < knownEntityPointer.StateVersion.Value)
            {
                return false;
            }
        }
        else if (StateRepository is not null)
        {
            entity = await StateRepository.Get(entityPointer, cancellationToken) ??
                          TEntity.Construct(entityId);
        }
        else
        {
            entity = TEntity.Construct(entityId);
        }

        var query = new GetDeltasDataQuery(entityPointer, entity.GetPointer().StateVersion);

        entity = await SourceRepository
            .EnumerateDeltas(query, cancellationToken)
            .AggregateAsync
            (
                entity,
                (previousEntity, delta) => previousEntity.Reduce(delta),
                cancellationToken
            );

        if (!entityPointer.IsSatisfiedBy(entity.GetPointer()))
        {
            return false;
        }

        _knownEntities.Add(entityId, entity);

        return true;
    }

    public TEntity Get(Id entityId)
    {
        if (!_knownEntities.TryGetValue(entityId, out var entity))
        {
            throw new UnknownEntityException();
        }

        return entity;
    }

    public void Append(Id entityId, object delta)
    {
        if (!_knownEntities.TryGetValue(entityId, out var entity))
        {
            throw new UnknownEntityException();
        }

        entity = entity.Reduce(delta);

        _messages.Add(Message.NewMessage(entity, entity.GetPointer(), delta));

        _knownEntities[entityId] = entity;
    }

    public async Task<bool> Commit(CancellationToken cancellationToken = default)
    {
        if (_messages.Count == 0)
        {
            return true;
        }

        var agent = await _agentAccessor.GetAgent(_agentSignatureOptionsName, cancellationToken);

        var source = new Source
        {
            Id = Id.NewId(),
            TimeStamp = agent.TimeStamp,
            AgentSignature = agent.Signature,
            Messages = _messages.ToArray(),
        };

        var committed = await SourceRepository.Commit(source, cancellationToken);

        if (!committed)
        {
            return false;
        }

        _messages.Clear();

        return true;
    }

    public override async ValueTask DisposeAsync()
    {
        await SourceRepository.DisposeAsync();

        if (StateRepository is not null)
        {
            await StateRepository.DisposeAsync();
        }
    }
}
