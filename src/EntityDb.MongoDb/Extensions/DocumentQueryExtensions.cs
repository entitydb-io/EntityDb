﻿using EntityDb.Abstractions.Annotations;
using EntityDb.Abstractions.ValueObjects;
using EntityDb.Common.Annotations;
using EntityDb.Common.Envelopes;
using EntityDb.MongoDb.Documents;
using EntityDb.MongoDb.Queries;
using EntityDb.MongoDb.Sessions;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EntityDb.MongoDb.Extensions;

internal static class DocumentQueryExtensions
{
    private static readonly ProjectionDefinitionBuilder<BsonDocument> ProjectionBuilder =
        Builders<BsonDocument>.Projection;

    private static readonly ProjectionDefinition<BsonDocument> NoDocumentIdProjection =
        ProjectionBuilder.Exclude(nameof(DocumentBase._id));

    private static readonly ProjectionDefinition<BsonDocument> EntityIdProjection =
        ProjectionBuilder.Include(nameof(IEntityDocument.EntityId));

    private static readonly ProjectionDefinition<BsonDocument> EntityIdsProjection =
        ProjectionBuilder.Include(nameof(IEntitiesDocument.EntityIds));

    private static readonly ProjectionDefinition<BsonDocument> TransactionIdProjection =
        ProjectionBuilder.Include(nameof(ITransactionDocument.TransactionId));

    private static readonly ProjectionDefinition<BsonDocument> DataProjection =
        ProjectionBuilder.Combine
        (
            ProjectionBuilder.Exclude(nameof(ITransactionDocument._id)),
            ProjectionBuilder.Include(nameof(ITransactionDocument.Data))
        );

    private static readonly ProjectionDefinition<BsonDocument> EntityVersionNumberProjection =
        ProjectionBuilder.Combine
        (
            ProjectionBuilder.Exclude(nameof(IEntityDocument._id)),
            ProjectionBuilder.Include(nameof(IEntityDocument.EntityVersionNumber))
        );

    public static async Task<VersionNumber> GetEntityVersionNumber<TDocument>
    (
        this DocumentQuery<TDocument> documentQuery,
        IMongoSession mongoSession
    )
        where TDocument : IEntityDocument
    {
        var documents = await documentQuery.Execute(mongoSession, EntityVersionNumberProjection);

        var document = documents.SingleOrDefault();

        return document == null
            ? default
            : document.EntityVersionNumber;
    }

    private static async Task<Id[]> GetIds<TDocument>
    (
        this DocumentQuery<TDocument> documentQuery,
        IMongoSession mongoSession,
        ProjectionDefinition<BsonDocument, TDocument> projection,
        Func<List<TDocument>, IEnumerable<Id>> mapToIds
    )
    {
        var skip = documentQuery.Skip;
        var limit = documentQuery.Limit;

        documentQuery = documentQuery with { Skip = null, Limit = null };

        var documents = await documentQuery.Execute(mongoSession, projection);

        var ids= mapToIds
            .Invoke(documents)
            .Distinct();

        if (skip.HasValue)
        {
            ids = ids.Skip(skip.Value);
        }

        if (limit.HasValue)
        {
            ids = ids.Take(limit.Value);
        }

        return ids.ToArray();
    }

    public static async Task<IEntityAnnotation<TData>[]> GetEntityAnnotation<TDocument, TData>
    (
        this DocumentQuery<TDocument> documentQuery,
        IMongoSession mongoSession, 
        IEnvelopeService<BsonDocument> envelopeService
    )
        where TDocument : IEntityDocument
    {
        var documents = await documentQuery.Execute(mongoSession, NoDocumentIdProjection);

        return documents
            .Select(document => new EntityAnnotation<TData>
            (
                document.TransactionId,
                document.TransactionTimeStamp,
                document.EntityId,
                document.EntityVersionNumber,
                envelopeService.Reconstruct<TData>(document.Data)
            ))
            .ToArray<IEntityAnnotation<TData>>();
    }

    public static async Task<TData[]> GetData<TDocument, TData>
    (
        this DocumentQuery<TDocument> documentQuery,
        IMongoSession mongoSession, 
        IEnvelopeService<BsonDocument> envelopeService
    )
        where TDocument : ITransactionDocument
    {
        var documents = await documentQuery.Execute(mongoSession, DataProjection);

        return documents
            .Select(document => envelopeService.Reconstruct<TData>(document.Data))
            .ToArray();
    }

    public static Task<Id[]> GetEntityIds<TDocument>
    (
        this DocumentQuery<TDocument> documentQuery,
        IMongoSession mongoSession
    )
        where TDocument : IEntityDocument
    {
        return documentQuery.GetIds
        (
            mongoSession,
            EntityIdProjection,
            documents => documents.Select(document => document.EntityId)
        );
    }

    public static Task<Id[]> GetEntitiesIds<TDocument>
    (
        this DocumentQuery<TDocument> documentQuery,
        IMongoSession mongoSession
    )
        where TDocument : IEntitiesDocument
    {
        return documentQuery.GetIds
        (
            mongoSession,
            EntityIdsProjection,
            documents => documents.SelectMany(document => document.EntityIds)
        );
    }

    public static Task<Id[]> GetTransactionIds<TDocument>
    (
        this DocumentQuery<TDocument> documentQuery,
        IMongoSession mongoSession
    )
        where TDocument : ITransactionDocument
    {
        return documentQuery.GetIds
        (
            mongoSession,
            TransactionIdProjection,
            documents => documents.Select(document => document.TransactionId)
        );
    }
}
