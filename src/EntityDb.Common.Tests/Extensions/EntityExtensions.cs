﻿using EntityDb.Abstractions.Commands;
using EntityDb.Abstractions.Facts;
using EntityDb.Common.Extensions;
using Moq;
using System.Linq;
using Xunit;

namespace EntityDb.Common.Tests.Extensions
{
    public class EntityExtensions
    {
        [Theory]
        [InlineData(2)]
        public void GivenEntityAndRepeatedCommand_WhenExecutingAndReducing_ThenEnsureExecuteAndReduceAreBothCalled(int numberOfTimes)
        {
            // ARRANGE

            var factMock = new Mock<IFact<object>>(MockBehavior.Strict);

            factMock
                .Setup(fact => fact.Reduce(It.IsAny<object>()))
                .Returns((object @object) => @object)
                .Verifiable();

            var commandMock = new Mock<ICommand<object>>(MockBehavior.Strict);

            commandMock
                .Setup(command => command.Execute(It.IsAny<object>()))
                .Returns(new[] { factMock.Object })
                .Verifiable();

            var command = commandMock.Object;

            var entity = new object();

            // ACT

            entity.ExecuteAndReduce(Enumerable.Repeat(command, numberOfTimes));

            // ASSERT

            commandMock.Verify(command => command.Execute(It.IsAny<object>()), Times.Exactly(numberOfTimes));

            factMock.Verify(fact => fact.Reduce(It.IsAny<object>()), Times.Exactly(numberOfTimes));
        }
    }
}
