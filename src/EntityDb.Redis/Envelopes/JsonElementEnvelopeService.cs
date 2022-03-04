﻿using EntityDb.Common.Envelopes;
using EntityDb.Common.Exceptions;
using EntityDb.Common.TypeResolvers;
using EntityDb.Redis.Converters;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;

namespace EntityDb.Redis.Envelopes;

internal sealed class JsonElementEnvelopeService : IEnvelopeService<JsonElement>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions();
    private readonly ILogger<IEnvelopeService<JsonElement>> _logger;
    private readonly ITypeResolver _typeResolver;

    static JsonElementEnvelopeService()
    {
        JsonSerializerOptions.Converters.Add(new EnvelopeHeadersConverter());
    }
    
    public JsonElementEnvelopeService
    (
        ILogger<IEnvelopeService<JsonElement>> logger,
        ITypeResolver typeResolver
    )
    {
        _logger = logger;
        _typeResolver = typeResolver;
    }
    
    public Envelope<JsonElement> Deconstruct<TData>(TData data)
    {
        try
        {
            var dataType = data!.GetType();
            
            var json = JsonSerializer.Serialize(data, dataType, JsonSerializerOptions);

            var jsonElement = JsonSerializer.Deserialize<JsonElement>(json, JsonSerializerOptions);

            var headers = EnvelopeHelper.GetEnvelopeHeaders(dataType);

            return new Envelope<JsonElement>(headers, jsonElement);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to deconstruct.");

            throw new SerializeException();
        }
    }

    public byte[] Serialize(Envelope<JsonElement> envelope)
    {
        try
        {
            return JsonSerializer.SerializeToUtf8Bytes(envelope, typeof(Envelope<JsonElement>), JsonSerializerOptions);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to serialize.");

            throw new SerializeException();
        }
    }

    public Envelope<JsonElement> Deserialize(byte[] rawData)
    {
        try
        {
            return (Envelope<JsonElement>)JsonSerializer.Deserialize(rawData, typeof(Envelope<JsonElement>), JsonSerializerOptions)!;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to deserialize.");

            throw new DeserializeException();
        }
    }
    
    public TData Reconstruct<TData>(Envelope<JsonElement> envelope)
    {
        try
        {
            return (TData)JsonSerializer.Deserialize(envelope.Value.GetRawText(), _typeResolver.ResolveType(envelope.Headers), JsonSerializerOptions)!;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to reconstruct.");

            throw new DeserializeException();
        }
    }
}
