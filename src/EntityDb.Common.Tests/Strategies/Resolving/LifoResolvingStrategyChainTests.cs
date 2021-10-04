﻿using EntityDb.Abstractions.Loggers;
using EntityDb.Abstractions.Strategies;
using EntityDb.Common.Exceptions;
using EntityDb.Common.Strategies.Resolving;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace EntityDb.Common.Tests.Strategies.Resolving
{
    public class LifoResolvingStrategyChainTests
    {
        [Fact]
        public void GivenResolvingStrategyThrows_WhenExceptionThrown_ThenExceptionIsLogged()
        {
            // ARRANGE

            var loggerMock = new Mock<ILogger>(MockBehavior.Strict);

            loggerMock
                .Setup(logger => logger.LogError(It.IsAny<Exception>(), It.IsAny<string>()))
                .Verifiable();

            var loggerFactoryMock = new Mock<ILoggerFactory>();

            loggerFactoryMock
                .Setup(factory => factory.CreateLogger(It.IsAny<Type>()))
                .Returns(loggerMock.Object);

            var resolvingStrategyMock = new Mock<IResolvingStrategy>();

            resolvingStrategyMock
                .Setup(strategy => strategy.ResolveType(It.IsAny<Dictionary<string, string>>()))
                .Throws(new Exception());

            var resolvingStrategyChain = new LifoResolvingStrategyChain(loggerFactoryMock.Object, new[]
            {
                resolvingStrategyMock.Object,
            });

            // ASSERT

            Should.Throw<CannotResolveTypeException>(() => resolvingStrategyChain.ResolveType(default!));

            loggerMock.Verify();
        }
    }
}
