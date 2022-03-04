﻿using EntityDb.Common.Tests.Agents;
using EntityDb.Mvc.Tests.Seeder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;

namespace EntityDb.Mvc.Tests.Agents;

public class HttpContextAgentAccessorTests : AgentAccessorTestsBase<Startup, HttpContextSeederOptions>
{
    public HttpContextAgentAccessorTests(IServiceProvider startupServiceProvider) : base(startupServiceProvider)
    {
    }

    protected override void ConfigureInactiveAgentAccessor(IServiceCollection serviceCollection)
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);

        httpContextAccessorMock
            .SetupGet(accessor => accessor.HttpContext)
            .Returns(default(HttpContext?));

        serviceCollection.AddSingleton(httpContextAccessorMock.Object);
    }

    protected override void ConfigureActiveAgentAccessor(IServiceCollection serviceCollection, HttpContextSeederOptions httpContextSeederOptions)
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);

        httpContextAccessorMock
            .SetupGet(accessor => accessor.HttpContext)
            .Returns(HttpContextSeeder.CreateHttpContext(httpContextSeederOptions));

        serviceCollection.AddSingleton(httpContextAccessorMock.Object);
    }

    protected override IEnumerable<HttpContextSeederOptions> GetAgentAccessorOptions()
    {
        return new[]
        {
            new HttpContextSeederOptions
            {
                Headers = new Dictionary<string, string[]>
                {
                    ["Content-Type"] = new[]{ "application/json" }
                },
                HasIpAddress = true
            },
            new HttpContextSeederOptions
            {
                Headers = new Dictionary<string, string[]>
                {
                    ["Content-Type"] = new[]{ "application/json" }
                },
                HasIpAddress = false
            }
        };
    }
}