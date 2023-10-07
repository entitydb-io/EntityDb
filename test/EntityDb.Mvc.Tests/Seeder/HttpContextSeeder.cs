using Bogus;
using EntityDb.Abstractions.ValueObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;

namespace EntityDb.Mvc.Tests.Seeder;

public static class HttpContextSeeder
{
    private static ConnectionInfo CreateConnectionInfo(HttpContextSeederOptions httpContextSeederOptions)
    {
        var connectionInfoMock = new Mock<ConnectionInfo>(MockBehavior.Strict);

        connectionInfoMock
            .SetupGet(info => info.Id)
            .Returns(Id.NewId().ToString());

        var faker = new Faker();

        connectionInfoMock
            .SetupGet(info => info.RemoteIpAddress)
            .Returns(httpContextSeederOptions.HasIpAddress ? faker.Internet.IpAddress() : null);

        connectionInfoMock
            .SetupGet(info => info.RemotePort)
            .Returns(faker.Internet.Port());

        connectionInfoMock
            .SetupGet(info => info.LocalIpAddress)
            .Returns(httpContextSeederOptions.HasIpAddress ? faker.Internet.IpAddress() : null);

        connectionInfoMock
            .SetupGet(info => info.LocalPort)
            .Returns(faker.Internet.Port());

        return connectionInfoMock.Object;
    }

    private static HttpRequest CreateHttpRequest(HttpContextSeederOptions httpContextSeederOptions)
    {
        var query = httpContextSeederOptions.QueryStringParams.ToDictionary(x => x.Key, x => new StringValues(x.Value));

        var queryCollectionMock = new Mock<IQueryCollection>(MockBehavior.Strict);

        queryCollectionMock
            .Setup(dictionary => dictionary.GetEnumerator())
            .Returns(query.GetEnumerator());

        var headers = httpContextSeederOptions.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value));

        var headerDictionaryMock = new Mock<IHeaderDictionary>(MockBehavior.Strict);

        headerDictionaryMock
            .Setup(dictionary => dictionary.GetEnumerator())
            .Returns(headers.GetEnumerator());

        var httpRequestMock = new Mock<HttpRequest>(MockBehavior.Strict);

        httpRequestMock
            .SetupGet(request => request.Method)
            .Returns(httpContextSeederOptions.Method);

        httpRequestMock
            .SetupGet(request => request.Scheme)
            .Returns(httpContextSeederOptions.Scheme);

        httpRequestMock
            .SetupGet(request => request.Host)
            .Returns(new HostString(httpContextSeederOptions.Host));

        httpRequestMock
            .SetupGet(request => request.Path)
            .Returns(new PathString(httpContextSeederOptions.Path));

        httpRequestMock
            .SetupGet(request => request.Protocol)
            .Returns(httpContextSeederOptions.Protocol);

        httpRequestMock
            .SetupGet(request => request.Query)
            .Returns(queryCollectionMock.Object);

        httpRequestMock
            .SetupGet(request => request.Headers)
            .Returns(headerDictionaryMock.Object);

        return httpRequestMock.Object;
    }

    public static HttpContext CreateHttpContext(HttpContextSeederOptions httpContextSeederOptions)
    {
        var httpContextMock = new Mock<HttpContext>(MockBehavior.Strict);

        httpContextMock
            .SetupGet(context => context.Request)
            .Returns(CreateHttpRequest(httpContextSeederOptions));

        httpContextMock
            .SetupGet(context => context.Connection)
            .Returns(CreateConnectionInfo(httpContextSeederOptions));

        return httpContextMock.Object;
    }
}