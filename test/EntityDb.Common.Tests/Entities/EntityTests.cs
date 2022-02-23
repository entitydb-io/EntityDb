﻿using EntityDb.Abstractions.Commands;
using EntityDb.Abstractions.Entities;
using EntityDb.Abstractions.Queries;
using EntityDb.Abstractions.Snapshots;
using EntityDb.Abstractions.Transactions;
using EntityDb.Abstractions.Transactions.Steps;
using EntityDb.Common.Exceptions;
using EntityDb.Common.Tests.Implementations.Commands;
using EntityDb.Common.Tests.Implementations.Entities;
using EntityDb.Common.Transactions.Builders;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EntityDb.Common.Tests.Entities
{
    public class EntityTests : TestsBase<Startup>
    {
        public EntityTests(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        private static ITransaction<TransactionEntity> BuildTransaction
        (
            IServiceScope serviceScope,
            Guid entityId,
            ulong from,
            ulong to
        )
        {
            var transactionBuilder = serviceScope.ServiceProvider
                .GetRequiredService<TransactionBuilder<TransactionEntity>>()
                .ForSingleEntity(entityId);

            for (var i = from; i <= to; i++)
            {
                transactionBuilder.Append(new DoNothing());
            }

            return transactionBuilder.Build(default!, Guid.NewGuid());
        }

        [Fact]
        public async Task GivenExistingEntityWithNoSnapshot_WhenGettingEntity_ThenGetCommandsRuns()
        {
            // ARRANGE

            const ulong ExpectedVersionNumber = 10;

            var entityId = Guid.NewGuid();

            var commands = new List<ICommand<TransactionEntity>>();

            var transactionRepositoryMock = new Mock<ITransactionRepository<TransactionEntity>>(MockBehavior.Strict);

            transactionRepositoryMock
                .Setup(repository => repository.PutTransaction(It.IsAny<ITransaction<TransactionEntity>>()))
                .ReturnsAsync((ITransaction<TransactionEntity> transaction) =>
                {
                    foreach (var transactionStep in transaction.Steps)
                    {
                        if (transactionStep is ICommandTransactionStep<TransactionEntity> commandTransactionStep)
                        {
                            commands.Add(commandTransactionStep.Command);
                        }
                    }

                    return true;
                });

            transactionRepositoryMock
                .Setup(factory => factory.DisposeAsync())
                .Returns(ValueTask.CompletedTask);

            transactionRepositoryMock
                .Setup(repository => repository.GetCommands(It.IsAny<ICommandQuery>()))
                .ReturnsAsync(() =>
                {
                    return commands.ToArray();
                })
                .Verifiable();

            var transactionRepositoryFactoryMock =
                new Mock<ITransactionRepositoryFactory<TransactionEntity>>(MockBehavior.Strict);

            transactionRepositoryFactoryMock
                .Setup(factory => factory.CreateRepository(It.IsAny<string>()))
                .ReturnsAsync(transactionRepositoryMock.Object);

            using var serviceScope = CreateServiceScope(serviceCollection =>
            {
                serviceCollection.AddSingleton(transactionRepositoryFactoryMock.Object);
            });

            await using var entityRepository = await serviceScope.ServiceProvider
                .GetRequiredService<IEntityRepositoryFactory<TransactionEntity>>()
                    .CreateRepository(TestSessionOptions.Write);

            var transaction = BuildTransaction(serviceScope, entityId, 1, ExpectedVersionNumber);

            var transactionInserted = await entityRepository.PutTransaction(transaction);

            // ARRANGE ASSERTIONS

            transactionInserted.ShouldBeTrue();

            // ACT

            var currenEntity = await entityRepository.GetCurrent(entityId);

            // ASSERT

            currenEntity.VersionNumber.ShouldBe(ExpectedVersionNumber);

            transactionRepositoryMock
                .Verify(repository => repository.GetCommands(It.IsAny<ICommandQuery>()), Times.Once);
        }

        [Fact]
        public async Task GivenNoSnapshotRepositoryFactory_WhenCreatingEntityRepositry_ThenNoSnapshotRepository()
        {
            // ARRANGE

            using var serviceScope = CreateServiceScope(serviceCollection =>
            {
                serviceCollection.AddSingleton(GetMockedTransactionRepositoryFactory());
            });

            // ACT

            var snapshotRepositoryFactory = serviceScope.ServiceProvider
                .GetService<ISnapshotRepositoryFactory<TransactionEntity>>();

            using var entityRepository = await serviceScope.ServiceProvider
                .GetRequiredService<IEntityRepositoryFactory<TransactionEntity>>()
                .CreateRepository("NOT NULL", "NOT NULL");

            // ASSERT

            snapshotRepositoryFactory.ShouldBeNull();

            entityRepository.SnapshotRepository.ShouldBeNull();
        }

        [Fact]
        public async Task GivenNoSnapshotSessionOptions_WhenCreatingEntityRepositry_ThenNoSnapshotRepository()
        {
            // ARRANGE

            using var serviceScope = CreateServiceScope(serviceCollection =>
            {
                serviceCollection.AddSingleton(GetMockedTransactionRepositoryFactory());
                serviceCollection.AddSingleton(GetMockedSnapshotRepositoryFactory());
            });

            // ACT

            var snapshotRepositoryFactory = serviceScope.ServiceProvider
                .GetService<ISnapshotRepositoryFactory<TransactionEntity>>();

            using var entityRepository = await serviceScope.ServiceProvider
                .GetRequiredService<IEntityRepositoryFactory<TransactionEntity>>()
                .CreateRepository("NOT NULL", null);

            // ASSERT

            snapshotRepositoryFactory.ShouldNotBeNull();

            entityRepository.SnapshotRepository.ShouldBeNull();
        }

        [Fact]
        public async Task GivenSnapshotRepositoryFactoryAndSnapshotSessionOptions_WhenCreatingEntityRepositry_ThenNoSnapshotRepository()
        {
            // ARRANGE

            using var serviceScope = CreateServiceScope(serviceCollection =>
            {
                serviceCollection.AddSingleton(GetMockedTransactionRepositoryFactory());
                serviceCollection.AddSingleton(GetMockedSnapshotRepositoryFactory());
            });

            // ACT

            var snapshotRepositoryFactory = serviceScope.ServiceProvider
                .GetService<ISnapshotRepositoryFactory<TransactionEntity>>();

            using var entityRepository = await serviceScope.ServiceProvider
                .GetRequiredService<IEntityRepositoryFactory<TransactionEntity>>()
                .CreateRepository("NOT NULL", "NOT NULL");

            // ASSERT

            snapshotRepositoryFactory.ShouldNotBeNull();

            entityRepository.SnapshotRepository.ShouldNotBeNull();
        }

        [Fact]
        public async Task GivenSnapshotAndNewCommands_WhenGettningSnapshotOrDefault_ThenReturnNewerThanSnapshot()
        {
            // ARRANGE

            var snapshot = new TransactionEntity(VersionNumber: 1);

            var newCommands = new[]
            {
                new DoNothing(),
                new DoNothing(),
            };

            using var serviceScope = CreateServiceScope(serviceCollection =>
            {
                serviceCollection.AddSingleton(GetMockedTransactionRepositoryFactory(newCommands));
                serviceCollection.AddSingleton(GetMockedSnapshotRepositoryFactory(snapshot));
            });

            // ACT

            using var entityRepository = await serviceScope.ServiceProvider
                .GetRequiredService<IEntityRepositoryFactory<TransactionEntity>>()
                .CreateRepository("NOT NULL", "NOT NULL");

            var snapshotOrDefault = await entityRepository.GetCurrent(default);

            // ASSERT

            snapshotOrDefault.ShouldNotBe(default);
            snapshotOrDefault.ShouldNotBe(snapshot);
            snapshotOrDefault!.VersionNumber.ShouldBe(snapshot.VersionNumber + Convert.ToUInt64(newCommands.Length));
        }

        [Fact]
        public async Task GivenNonExistentEntityId_WhenGettingCurrentEntity_ThenThrow()
        {
            // ARRANGE

            using var serviceScope = CreateServiceScope(serviceCollection =>
            {
                serviceCollection.AddSingleton(GetMockedTransactionRepositoryFactory());
            });

            await using var entityRepository = await serviceScope.ServiceProvider
                .GetRequiredService<IEntityRepositoryFactory<TransactionEntity>>()
                    .CreateRepository(default!);

            // ASSERT

            await Should.ThrowAsync<EntityNotCreatedException>(async () =>
            {
                await entityRepository.GetCurrent(default);
            });
        }
    }
}
