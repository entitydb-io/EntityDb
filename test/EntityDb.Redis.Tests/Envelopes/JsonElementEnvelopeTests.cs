﻿using EntityDb.Abstractions.Loggers;
using EntityDb.Abstractions.TypeResolvers;
using EntityDb.Common.Extensions;
using EntityDb.Common.Tests;
using EntityDb.Redis.Envelopes;
using EntityDb.Redis.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Text;
using Xunit;

namespace EntityDb.Redis.Tests.Envelopes;

public class JsonElementEnvelopeTests : TestsBase<Startup>
{
    public JsonElementEnvelopeTests(IServiceProvider startupServiceProvider) : base(startupServiceProvider)
    {
    }

    [Fact]
    public void WhenGoingThroughFullCycle_ThenOriginalMatchesReconstructed()
    {
        // ARRANGE

        using var serviceScope = CreateServiceScope();

        var logger = serviceScope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger<JsonElementEnvelopeTests>();

        var typeResolver = serviceScope.ServiceProvider
            .GetRequiredService<ITypeResolver>();

        var originalTestRecord = new TestRecord<bool>(true);

        IRecord boxedTestRecord = originalTestRecord;

        // ACT

        var jsonElementEnvelope = JsonElementEnvelope.Deconstruct(boxedTestRecord, logger);

        var json = jsonElementEnvelope.Serialize(logger);

        var reconstructedJsonElementEnvelope = JsonElementEnvelope.Deserialize(json, logger);

        var reconstructedTestRecord =
            reconstructedJsonElementEnvelope.Reconstruct<IRecord>(logger, typeResolver);

        var unboxedTestRecord = (TestRecord<bool>)reconstructedTestRecord;

        // ASSERT

        unboxedTestRecord.TestProperty.ShouldBe(originalTestRecord.TestProperty);
    }

    [Fact]
    public void WhenDeserializingInvalidJsonBytes_ThrowDeserializeException()
    {
        // ARRANGE

        const string invalidJson = "I AM A STRING VALUE, NOT JSON!";

        using var serviceScope = CreateServiceScope();

        var logger = serviceScope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger<JsonElementEnvelopeTests>();

        var invalidJsonBytes = Encoding.UTF8.GetBytes(invalidJson);

        // ACT

        Should.Throw<DeserializeException>(() =>
        {
            JsonElementEnvelope.Deserialize(invalidJsonBytes, logger);
        });
    }

    [Fact]
    public void WhenReconstructingJsonElementEnvelopeWithNullValue_ThrowDeserializeException()
    {
        // ARRANGE

        using var serviceScope = CreateServiceScope();

        var logger = serviceScope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger<JsonElementEnvelopeTests>();

        var typeResolver = serviceScope.ServiceProvider
            .GetRequiredService<ITypeResolver>();

        var jsonElementEnvelope = new JsonElementEnvelope();

        // ACT

        Should.Throw<DeserializeException>(() =>
        {
            jsonElementEnvelope.Reconstruct<object>(logger, typeResolver);
        });
    }

    [Fact]
    public void WhenSerializingJsonElementEnvelopeWithNullValue_ThrowSerializeException()
    {
        // ARRANGE

        using var serviceScope = CreateServiceScope();

        var logger = serviceScope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger<JsonElementEnvelopeTests>();

        var jsonElementEnvelope = new JsonElementEnvelope();

        // ACT

        Should.Throw<SerializeException>(() =>
        {
            jsonElementEnvelope.Serialize(logger);
        });
    }

    [Fact]
    public void WhenDeconstructingNull_ThrowSerializeException()
    {
        // ACT

        using var serviceScope = CreateServiceScope();

        var logger = serviceScope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger<JsonElementEnvelopeTests>();

        Should.Throw<SerializeException>(() =>
        {
            JsonElementEnvelope.Deconstruct(default!, logger);
        });
    }

    private interface IRecord
    {
    }

    private record TestRecord<T>(T TestProperty) : IRecord;
}