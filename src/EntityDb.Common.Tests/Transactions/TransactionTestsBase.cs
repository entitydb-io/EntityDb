﻿using EntityDb.Abstractions.Commands;
using EntityDb.Abstractions.Facts;
using EntityDb.Abstractions.Leases;
using EntityDb.Abstractions.Loggers;
using EntityDb.Abstractions.Queries;
using EntityDb.Abstractions.Tags;
using EntityDb.Abstractions.Transactions;
using EntityDb.Common.Entities;
using EntityDb.Common.Exceptions;
using EntityDb.Common.Extensions;
using EntityDb.Common.Leases;
using EntityDb.Common.Queries;
using EntityDb.Common.Queries.Modified;
using EntityDb.Common.Tags;
using EntityDb.Common.Transactions;
using EntityDb.TestImplementations.Commands;
using EntityDb.TestImplementations.Entities;
using EntityDb.TestImplementations.Facts;
using EntityDb.TestImplementations.Leases;
using EntityDb.TestImplementations.Queries;
using EntityDb.TestImplementations.Source;
using EntityDb.TestImplementations.Tags;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EntityDb.Common.Tests.Transactions
{
    public abstract class TransactionTestsBase
    {
        private class ExpectedObjects
        {
            public readonly List<Guid> TrueTransactionIds = new();
            public readonly List<Guid> FalseTransactionIds = new();

            public readonly List<Guid> TrueEntityIds = new();
            public readonly List<Guid> FalseEntityIds = new();

            public readonly List<object> TrueSources = new();
            public readonly List<object> FalseSources = new();

            public readonly List<ICommand<TransactionEntity>> TrueCommands = new();
            public readonly List<ICommand<TransactionEntity>> FalseCommands = new();

            public readonly List<IFact<TransactionEntity>> TrueFacts = new();
            public readonly List<IFact<TransactionEntity>> FalseFacts = new();

            public readonly List<ILease> TrueLeases = new();
            public readonly List<ILease> FalseLeases = new();

            public readonly List<ITag> TrueTags = new();
            public readonly List<ITag> FalseTags = new();

            public void Add
            (
                bool condition,
                Guid transactionId,
                Guid entityId,
                object source,
                IEnumerable<ICommand<TransactionEntity>> commands,
                IEnumerable<IFact<TransactionEntity>> facts,
                IEnumerable<ILease> leases,
                IEnumerable<ITag> tags
            )
            {
                if (condition)
                {
                    TrueTransactionIds.Add(transactionId);
                    TrueEntityIds.Add(entityId);
                    TrueSources.Add(source);
                    TrueCommands.AddRange(commands);
                    TrueFacts.AddRange(facts);
                    TrueLeases.AddRange(leases);
                    TrueTags.AddRange(tags);
                }
                else
                {
                    FalseTransactionIds.Add(transactionId);
                    FalseEntityIds.Add(entityId);
                    FalseSources.Add(source);
                    FalseCommands.AddRange(commands);
                    FalseFacts.AddRange(facts);
                    FalseLeases.AddRange(leases);
                    FalseTags.AddRange(tags);
                }
            }
        }

        private readonly IServiceProvider _serviceProvider;

        protected TransactionTestsBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private Task<ITransactionRepository<TransactionEntity>> CreateRepository(bool readOnly = false, bool tolerateLag = false, ILogger? loggerOverride = null)
        {
            return _serviceProvider.CreateTransactionRepository<TransactionEntity>(new TransactionSessionOptions
            {
                ReadOnly = readOnly,
                SecondaryPreferred = tolerateLag,
                LoggerOverride = loggerOverride,
            });
        }

        private async Task TestGet<TResult>
        (
            List<ITransaction<TransactionEntity>> transactions,
            Func<bool, TResult[]> getExpectedResults,
            Func<ITransactionRepository<TransactionEntity>, bool, bool, int?, int?, Task<TResult[]>> getActualResults
        )
        {
            // ARRANGE

            var expectedTrueResults = getExpectedResults.Invoke(false);
            var expectedFalseResults = getExpectedResults.Invoke(true);
            var reversedExpectedTrueResults = expectedTrueResults.Reverse().ToArray();
            var reversedExpectedFalseResults = expectedFalseResults.Reverse().ToArray();
            var expectedSkipTakeResults = expectedTrueResults.Skip(1).Take(1);

            await using var transactionRepository = await CreateRepository();

            foreach (var transaction in transactions)
            {
                var transactionInserted = await transactionRepository.PutTransaction(transaction);

                transactionInserted.ShouldBeTrue();
            }

            // ACT

            var actualTrueResults = await getActualResults.Invoke(transactionRepository, false, false, null, null);
            var actualFalseResults = await getActualResults.Invoke(transactionRepository, true, false, null, null);
            var reversedActualTrueResults = await getActualResults.Invoke(transactionRepository, false, true, null, null);
            var reversedActualFalseResults = await getActualResults.Invoke(transactionRepository, true, true, null, null);
            var actualSkipTakeResults = await getActualResults.Invoke(transactionRepository, false, false, 1, 1);

            // ASSERT

            actualTrueResults.SequenceEqual(expectedTrueResults).ShouldBeTrue();
            actualFalseResults.SequenceEqual(expectedFalseResults).ShouldBeTrue();
            reversedActualTrueResults.SequenceEqual(reversedExpectedTrueResults).ShouldBeTrue();
            reversedActualFalseResults.SequenceEqual(reversedExpectedFalseResults).ShouldBeTrue();
            actualSkipTakeResults.SequenceEqual(expectedSkipTakeResults).ShouldBeTrue();
        }

        private Task TestGetTransactionIds(ISourceQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<ISourceQuery, ISourceQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseTransactionIds : expectedObjects.TrueTransactionIds).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQueryOptions = new ModifiedQueryOptions
                    {
                        InvertFilter = invertFilter,
                        ReverseSort = reverseSort,
                        ReplaceSkip = skip,
                        ReplaceTake = take,
                    };

                    var modifiedQuery = query.Modify(modifiedQueryOptions);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetTransactionIds(modifiedQuery);
                }
            );
        }

        private Task TestGetTransactionIds(ICommandQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<ICommandQuery, ICommandQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseTransactionIds : expectedObjects.TrueTransactionIds).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQueryOptions = new ModifiedQueryOptions
                    {
                        InvertFilter = invertFilter,
                        ReverseSort = reverseSort,
                        ReplaceSkip = skip,
                        ReplaceTake = take,
                    };

                    var modifiedQuery = query.Modify(modifiedQueryOptions);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetTransactionIds(modifiedQuery);
                }
            );
        }

        private Task TestGetTransactionIds(IFactQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<IFactQuery, IFactQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseTransactionIds : expectedObjects.TrueTransactionIds).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQueryOptions = new ModifiedQueryOptions
                    {
                        InvertFilter = invertFilter,
                        ReverseSort = reverseSort,
                        ReplaceSkip = skip,
                        ReplaceTake = take,
                    };

                    var modifiedQuery = query.Modify(modifiedQueryOptions);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetTransactionIds(modifiedQuery);
                }
            );
        }

        private Task TestGetTransactionIds(ILeaseQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<ILeaseQuery, ILeaseQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseTransactionIds : expectedObjects.TrueTransactionIds).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQueryOptions = new ModifiedQueryOptions
                    {
                        InvertFilter = invertFilter,
                        ReverseSort = reverseSort,
                        ReplaceSkip = skip,
                        ReplaceTake = take,
                    };

                    var modifiedQuery = query.Modify(modifiedQueryOptions);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetTransactionIds(modifiedQuery);
                }
            );
        }

        private Task TestGetEntityIds(ISourceQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<ISourceQuery, ISourceQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseEntityIds : expectedObjects.TrueEntityIds).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQueryOptions = new ModifiedQueryOptions
                    {
                        InvertFilter = invertFilter,
                        ReverseSort = reverseSort,
                        ReplaceSkip = skip,
                        ReplaceTake = take,
                    };

                    var modifiedQuery = query.Modify(modifiedQueryOptions);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetEntityIds(modifiedQuery);
                }
            );
        }

        private Task TestGetEntityIds(ICommandQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<ICommandQuery, ICommandQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseEntityIds : expectedObjects.TrueEntityIds).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQueryOptions = new ModifiedQueryOptions
                    {
                        InvertFilter = invertFilter,
                        ReverseSort = reverseSort,
                        ReplaceSkip = skip,
                        ReplaceTake = take,
                    };

                    var modifiedQuery = query.Modify(modifiedQueryOptions);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetEntityIds(modifiedQuery);
                }
            );
        }

        private Task TestGetEntityIds(IFactQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<IFactQuery, IFactQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseEntityIds : expectedObjects.TrueEntityIds).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQueryOptions = new ModifiedQueryOptions
                    {
                        InvertFilter = invertFilter,
                        ReverseSort = reverseSort,
                        ReplaceSkip = skip,
                        ReplaceTake = take,
                    };

                    var modifiedQuery = query.Modify(modifiedQueryOptions);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetEntityIds(modifiedQuery);
                }
            );
        }

        private Task TestGetEntityIds(ILeaseQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<ILeaseQuery, ILeaseQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseEntityIds : expectedObjects.TrueEntityIds).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQueryOptions = new ModifiedQueryOptions
                    {
                        InvertFilter = invertFilter,
                        ReverseSort = reverseSort,
                        ReplaceSkip = skip,
                        ReplaceTake = take,
                    };

                    var modifiedQuery = query.Modify(modifiedQueryOptions);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetEntityIds(modifiedQuery);
                }
            );
        }

        private Task TestGetSources(ISourceQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<ISourceQuery, ISourceQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseSources : expectedObjects.TrueSources).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQueryOptions = new ModifiedQueryOptions
                    {
                        InvertFilter = invertFilter,
                        ReverseSort = reverseSort,
                        ReplaceSkip = skip,
                        ReplaceTake = take,
                    };

                    var modifiedQuery = query.Modify(modifiedQueryOptions);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetSources(modifiedQuery);
                }
            );
        }

        private Task TestGetCommands(ICommandQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<ICommandQuery, ICommandQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseCommands : expectedObjects.TrueCommands).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQueryOptions = new ModifiedQueryOptions
                    {
                        InvertFilter = invertFilter,
                        ReverseSort = reverseSort,
                        ReplaceSkip = skip,
                        ReplaceTake = take,
                    };

                    var modifiedQuery = query.Modify(modifiedQueryOptions);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetCommands(modifiedQuery);
                }
            );
        }

        private Task TestGetFacts(IFactQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<IFactQuery, IFactQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseFacts : expectedObjects.TrueFacts).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQueryOptions = new ModifiedQueryOptions
                    {
                        InvertFilter = invertFilter,
                        ReverseSort = reverseSort,
                        ReplaceSkip = skip,
                        ReplaceTake = take,
                    };

                    var modifiedQuery = query.Modify(modifiedQueryOptions);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetFacts(modifiedQuery);
                }
            );
        }

        private Task TestGetLeases(ILeaseQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<ILeaseQuery, ILeaseQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseLeases : expectedObjects.TrueLeases).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQueryOptions = new ModifiedQueryOptions
                    {
                        InvertFilter = invertFilter,
                        ReverseSort = reverseSort,
                        ReplaceSkip = skip,
                        ReplaceTake = take,
                    };

                    var modifiedQuery = query.Modify(modifiedQueryOptions);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetLeases(modifiedQuery);
                }
            );
        }

        private Task TestGetTags(ITagQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<ITagQuery, ITagQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseTags : expectedObjects.TrueTags).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQueryOptions = new ModifiedQueryOptions
                    {
                        InvertFilter = invertFilter,
                        ReverseSort = reverseSort,
                        ReplaceSkip = skip,
                        ReplaceTake = take,
                    };

                    var modifiedQuery = query.Modify(modifiedQueryOptions);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetTags(modifiedQuery);
                }
            );
        }

        private ITransaction<TransactionEntity> BuildTransaction(Guid transactionId, Guid entityId, object source, ICommand<TransactionEntity>[] commands, DateTime? timeStampOverride = null)
        {
            var transactionBuilder = _serviceProvider.GetTransactionBuilder<TransactionEntity>();

            transactionBuilder.Create(entityId, commands[0]);

            for (var i = 1; i < commands.Length; i++)
            {
                transactionBuilder.Append(entityId, commands[i]);
            }

            return transactionBuilder.Build(transactionId, source, timeStampOverride);
        }

        private Guid[] GetSortedGuids(int numberOfGuids)
        {
            return Enumerable
                .Range(1, numberOfGuids)
                .Select(_ => Guid.NewGuid())
                .OrderBy(guid => guid)
                .ToArray();
        }

        [Fact]
        public async Task GivenReadOnlyMode_WhenPuttingTransaction_ThenThrow()
        {
            // ARRANGE

            var transaction = new Transaction<TransactionEntity>
            {
                Id = Guid.NewGuid(),
                TimeStamp = DateTime.UtcNow,
                Source = new NoSource(),
                Commands = new[]
                {
                    new TransactionCommand<TransactionEntity>
                    {
                        PreviousSnapshot = default,
                        NextSnapshot = default!,
                        EntityId = Guid.NewGuid(),
                        ExpectedPreviousVersionNumber = 0,
                        Command = new DoNothing(),
                        Facts = ImmutableArray<ITransactionFact<TransactionEntity>>.Empty,
                        Leases = new TransactionMetaData<ILease>(),
                        Tags = new TransactionMetaData<ITag>(),
                    },
                }.ToImmutableArray<ITransactionCommand<TransactionEntity>>(),
            };

            await using var transactionRepository = await CreateRepository(readOnly: true);

            // ASSERT

            await Should.ThrowAsync<CannotWriteInReadOnlyModeException>(async () => await transactionRepository.PutTransaction(transaction));
        }

        [Fact]
        public async Task GivenNonUniqueTransactionIds_WhenPuttingTransactions_ThenSecondPutReturnsFalse()
        {
            // ARRANGE

            var transactionId = Guid.NewGuid();

            static ITransaction<TransactionEntity> NewTransaction(Guid transactionId)
            {
                return new Transaction<TransactionEntity>
                {
                    Id = transactionId,
                    TimeStamp = DateTime.UtcNow,
                    Source = new NoSource(),
                    Commands = new[]
                    {
                        new TransactionCommand<TransactionEntity>
                        {
                            PreviousSnapshot = default,
                            NextSnapshot = default!,
                            EntityId = Guid.NewGuid(),
                            ExpectedPreviousVersionNumber = 0,
                            Command = new DoNothing(),
                            Facts = ImmutableArray<ITransactionFact<TransactionEntity>>.Empty,
                            Leases = new TransactionMetaData<ILease>(),
                            Tags = new TransactionMetaData<ITag>(),
                        },
                    }.ToImmutableArray<ITransactionCommand<TransactionEntity>>(),
                };
            }

            await using var transactionRepository = await CreateRepository();

            // ACT

            var firstTransactionInserted = await transactionRepository.PutTransaction(NewTransaction(transactionId));
            var secondTransactionInserted = await transactionRepository.PutTransaction(NewTransaction(transactionId));

            // ASSERT

            firstTransactionInserted.ShouldBeTrue();
            secondTransactionInserted.ShouldBeFalse();
        }

        [Fact]
        public async Task GivenNonUniqueVersionNumbers_WhenInsertingCommands_ThenReturnFalse()
        {
            // ARRANGE

            var entityId = Guid.NewGuid();
            ulong previousVersionNumber = 0;

            var transaction = new Transaction<TransactionEntity>
            {
                Id = Guid.NewGuid(),
                TimeStamp = DateTime.UtcNow,
                Source = new NoSource(),
                Commands = new[]
                {
                    new TransactionCommand<TransactionEntity>
                    {
                        PreviousSnapshot = default,
                        NextSnapshot = default!,
                        EntityId = entityId,
                        ExpectedPreviousVersionNumber = previousVersionNumber,
                        Command = new DoNothing(),
                        Facts = ImmutableArray<ITransactionFact<TransactionEntity>>.Empty,
                        Leases = new TransactionMetaData<ILease>(),
                        Tags = new TransactionMetaData<ITag>(),
                    },
                    new TransactionCommand<TransactionEntity>
                    {
                        PreviousSnapshot = default,
                        NextSnapshot = default!,
                        EntityId = entityId,
                        ExpectedPreviousVersionNumber = previousVersionNumber,
                        Command = new DoNothing(),
                        Facts = ImmutableArray<ITransactionFact<TransactionEntity>>.Empty,
                        Leases = new TransactionMetaData<ILease>(),
                        Tags = new TransactionMetaData<ITag>(),
                    },
                }.ToImmutableArray<ITransactionCommand<TransactionEntity>>(),
            };

            await using var transactionRepository = await CreateRepository();

            // ACT

            var transactionInserted = await transactionRepository.PutTransaction(transaction);

            // ASSERT

            transactionInserted.ShouldBeFalse();
        }

        [Fact]
        public async Task GivenNonUniqueVersionNumbers_WhenInsertingCommands_ThenOptimisticConcurrencyExceptionIsLogged()
        {
            // ARRANGE

            const ulong previousVersionNumber = 0;

            var entityId = Guid.NewGuid();

            static ITransaction<TransactionEntity> NewTransaction(Guid entityId, ulong previousVersionNumber)
            {
                return new Transaction<TransactionEntity>
                {
                    Id = Guid.NewGuid(),
                    TimeStamp = DateTime.UtcNow,
                    Source = new NoSource(),
                    Commands = new[]
                    {
                        new TransactionCommand<TransactionEntity>
                        {
                            PreviousSnapshot = default,
                            NextSnapshot = default!,
                            EntityId = entityId,
                            ExpectedPreviousVersionNumber = previousVersionNumber,
                            Command = new DoNothing(),
                            Facts = ImmutableArray<ITransactionFact<TransactionEntity>>.Empty,
                            Leases = new TransactionMetaData<ILease>(),
                            Tags = new TransactionMetaData<ITag>(),
                        },
                    }.ToImmutableArray<ITransactionCommand<TransactionEntity>>(),
                };
            }

            var loggerMock = new Mock<ILogger>(MockBehavior.Strict);

            loggerMock
                .Setup(logger => logger.LogError(It.IsAny<OptimisticConcurrencyException>(), It.IsAny<string>()))
                .Verifiable();

            await using var transactionRepository = await CreateRepository(loggerOverride: loggerMock.Object);

            // ACT

            var firstTransactionInserted = await transactionRepository.PutTransaction(NewTransaction(entityId, previousVersionNumber));
            var secondTransactionInserted = await transactionRepository.PutTransaction(NewTransaction(entityId, previousVersionNumber));

            // ASSERT

            firstTransactionInserted.ShouldBeTrue();
            secondTransactionInserted.ShouldBeFalse();

            loggerMock.Verify();
        }

        [Fact]
        public async Task GivenNonUniqueSubversionNumbers_WhenInsertingFacts_ThenReturnFalse()
        {
            // ARRANGE

            var entityId = Guid.NewGuid();
            ulong subversionNumber = 0;

            var transaction = new Transaction<TransactionEntity>
            {
                Id = Guid.NewGuid(),
                TimeStamp = DateTime.UtcNow,
                Source = new NoSource(),
                Commands = new[]
                {
                    new TransactionCommand<TransactionEntity>
                    {
                        PreviousSnapshot = default,
                        NextSnapshot = default!,
                        EntityId = entityId,
                        ExpectedPreviousVersionNumber = 0,
                        Command = new DoNothing(),
                        Facts = new[]
                        {
                            new TransactionFact<TransactionEntity>
                            {
                                SubversionNumber = subversionNumber,
                                Fact = new NothingDone(),
                            },
                            new TransactionFact<TransactionEntity>
                            {
                                SubversionNumber = subversionNumber,
                                Fact = new NothingDone(),
                            },
                        }.ToImmutableArray<ITransactionFact<TransactionEntity>>(),
                        Leases = new TransactionMetaData<ILease>(),
                        Tags = new TransactionMetaData<ITag>(),
                    },
                }.ToImmutableArray<ITransactionCommand<TransactionEntity>>(),
            };

            await using var transactionRepository = await CreateRepository();

            // ACT

            var transactionInserted = await transactionRepository.PutTransaction(transaction);

            // ASSERT

            transactionInserted.ShouldBeFalse();
        }

        [Fact]
        public async Task GivenNonUniqueTags_WhenInsertingTagDocuments_ThenReturnTrue()
        {
            // ARRANGE

            var tag = new Tag("Foo", "Bar");

            var transaction = new Transaction<TransactionEntity>
            {
                Id = Guid.NewGuid(),
                TimeStamp = DateTime.UtcNow,
                Source = new NoSource(),
                Commands = new[]
                {
                    new TransactionCommand<TransactionEntity>
                    {
                        PreviousSnapshot = default,
                        NextSnapshot = default!,
                        EntityId = Guid.NewGuid(),
                        ExpectedPreviousVersionNumber = 0,
                        Command = new DoNothing(),
                        Facts = ImmutableArray<ITransactionFact<TransactionEntity>>.Empty,
                        Leases = new TransactionMetaData<ILease>(),
                        Tags = new TransactionMetaData<ITag>
                        {
                            Insert = new[] { tag }.ToImmutableArray<ITag>(),
                        }
                    },
                    new TransactionCommand<TransactionEntity>
                    {
                        PreviousSnapshot = default,
                        NextSnapshot = default!,
                        EntityId = Guid.NewGuid(),
                        ExpectedPreviousVersionNumber = 0,
                        Command = new DoNothing(),
                        Facts = ImmutableArray<ITransactionFact<TransactionEntity>>.Empty,
                        Leases = new TransactionMetaData<ILease>(),
                        Tags = new TransactionMetaData<ITag>
                        {
                            Insert = new[] { tag }.ToImmutableArray<ITag>(),
                        }
                    },
                }.ToImmutableArray<ITransactionCommand<TransactionEntity>>(),
            };

            await using var transactionRepository = await CreateRepository();

            // ACT

            var transactionInserted = await transactionRepository.PutTransaction(transaction);

            // ASSERT

            transactionInserted.ShouldBeTrue();
        }

        [Fact]
        public async Task GivenNonUniqueLeases_WhenInsertingLeaseDocuments_ThenReturnFalse()
        {
            // ARRANGE

            var lease = new Lease("Foo", "Bar", "Baz");

            var transaction = new Transaction<TransactionEntity>
            {
                Id = Guid.NewGuid(),
                TimeStamp = DateTime.UtcNow,
                Source = new NoSource(),
                Commands = new[]
                {
                    new TransactionCommand<TransactionEntity>
                    {
                        PreviousSnapshot = default,
                        NextSnapshot = default!,
                        EntityId = Guid.NewGuid(),
                        ExpectedPreviousVersionNumber = 0,
                        Command = new DoNothing(),
                        Facts = ImmutableArray<ITransactionFact<TransactionEntity>>.Empty,
                        Leases = new TransactionMetaData<ILease>
                        {
                            Insert = new[] { lease }.ToImmutableArray<ILease>(),
                        },
                        Tags = new TransactionMetaData<ITag>(),
                    },
                    new TransactionCommand<TransactionEntity>
                    {
                        PreviousSnapshot = default,
                        NextSnapshot = default!,
                        EntityId = Guid.NewGuid(),
                        ExpectedPreviousVersionNumber = 0,
                        Command = new DoNothing(),
                        Facts = ImmutableArray<ITransactionFact<TransactionEntity>>.Empty,
                        Leases = new TransactionMetaData<ILease>
                        {
                            Insert = new[] { lease }.ToImmutableArray<ILease>(),
                        },
                        Tags = new TransactionMetaData<ITag>(),
                    },
                }.ToImmutableArray<ITransactionCommand<TransactionEntity>>(),
            };

            await using var transactionRepository = await CreateRepository();

            // ACT

            var transactionInserted = await transactionRepository.PutTransaction(transaction);

            // ASSERT

            transactionInserted.ShouldBeFalse();
        }

        [Fact]
        public async Task GivenEntityInserted_WhenGettingEntity_ThenReturnEntity()
        {
            // ARRANGE

            var expectedEntity = new TransactionEntity
            {
                VersionNumber = 1,
            };

            var entityId = Guid.NewGuid();

            await using var transactionRepository = await CreateRepository();

            var entityRepository = new EntityRepository<TransactionEntity>(_serviceProvider, transactionRepository);

            var transaction = BuildTransaction(Guid.NewGuid(), entityId, new NoSource(), new[] { new DoNothing() });

            await transactionRepository.PutTransaction(transaction);

            // ACT

            var actualEntity = await entityRepository.Get(entityId);

            // ASSERT

            actualEntity.ShouldBeEquivalentTo(expectedEntity);
        }

        [Fact]
        public async Task GivenEntityInsertedWithTags_WhenRemovingAllTags_ThenFinalEntityHasNoTags()
        {
            // ARRANGE

            var transactionBuilder = _serviceProvider.GetTransactionBuilder<TransactionEntity>();

            var expectedInitialTags = new[]
            {
                new Tag("Foo", "Bar"),
            }.ToImmutableArray<ITag>();

            var entityId = Guid.NewGuid();

            await using var transactionRepository = await CreateRepository();

            var initialTransaction = transactionBuilder
                .Create(entityId, new AddTag("Foo", "Bar"))
                .Build(Guid.NewGuid(), new NoSource());

            await transactionRepository.PutTransaction(initialTransaction);

            var tagQuery = new DeleteTagsQuery(entityId, expectedInitialTags);

            // ACT

            var actualInitialTags = await transactionRepository.GetTags(tagQuery);

            var finalTransaction = transactionBuilder
                .Append(entityId, new RemoveAllTags())
                .Build(Guid.NewGuid(), new NoSource());

            await transactionRepository.PutTransaction(finalTransaction);

            var actualFinalTags = await transactionRepository.GetTags(tagQuery);

            // ASSERT

            expectedInitialTags.SequenceEqual(actualInitialTags).ShouldBeTrue();

            actualFinalTags.ShouldBeEmpty();
        }

        [Fact]
        public async Task GivenEntityInsertedWithLeases_WhenRemovingAllLeases_ThenFinalEntityHasNoLeases()
        {
            // ARRANGE

            var transactionBuilder = _serviceProvider.GetTransactionBuilder<TransactionEntity>();

            var expectedInitialLeases = new[]
            {
                new Lease("Foo", "Bar", "Baz"),
            }.ToImmutableArray<ILease>();

            var entityId = Guid.NewGuid();

            await using var transactionRepository = await CreateRepository();

            var initialTransaction = transactionBuilder
                .Create(entityId, new AddLease("Foo", "Bar", "Baz"))
                .Build(Guid.NewGuid(), new NoSource());

            await transactionRepository.PutTransaction(initialTransaction);

            var leaseQuery = new DeleteLeasesQuery(entityId, expectedInitialLeases);

            // ACT

            var actualInitialLeases = await transactionRepository.GetLeases(leaseQuery);

            var finalTransaction = transactionBuilder
                .Append(entityId, new RemoveAllLeases())
                .Build(Guid.NewGuid(), new NoSource());

            await transactionRepository.PutTransaction(finalTransaction);

            var actualFinalLeases = await transactionRepository.GetLeases(leaseQuery);

            // ASSERT

            actualInitialLeases.SequenceEqual(expectedInitialLeases).ShouldBeTrue();

            actualFinalLeases.ShouldBeEmpty();
        }

        [Fact]
        public async Task GivenTransactionCreatesEntity_WhenQueryingForVersionOne_ThenReturnTheExpectedCommand()
        {
            // ARRANGE

            var expectedCommand = new Count(1);

            var transaction = _serviceProvider
                .GetTransactionBuilder<TransactionEntity>()
                .Create(Guid.NewGuid(), expectedCommand)
                .Build(Guid.NewGuid(), new NoSource());

            var versionOneCommandQuery = new EntityVersionNumberQuery(1, 1);

            using var transactionRepository = await CreateRepository();

            // ACT

            await transactionRepository.PutTransaction(transaction);

            var newCommands = await transactionRepository.GetCommands(versionOneCommandQuery);

            // ASSERT

            transaction.Commands.Length.ShouldBe(1);

            transaction.Commands[0].ExpectedPreviousVersionNumber.ShouldBe(default);

            newCommands.Length.ShouldBe(1);

            newCommands[0].ShouldBeEquivalentTo(expectedCommand);
        }

        [Fact]
        public async Task GivenTransactionAppendsEntityWithOneVersion_WhenQueryingForVersionTwo_ThenReturnExpectedCommand()
        {
            // ARRANGE

            var expectedCommand = new Count(2);

            var entityId = Guid.NewGuid();

            var transactionBuilder = _serviceProvider.GetTransactionBuilder<TransactionEntity>();

            var firstTransaction = transactionBuilder
                .Create(entityId, new Count(1))
                .Build(Guid.NewGuid(), new NoSource());

            var secondTransaction = transactionBuilder
                .Append(entityId, expectedCommand)
                .Build(Guid.NewGuid(), new NoSource());

            var versionTwoCommandQuery = new EntityVersionNumberQuery(2, 2);

            using var transactionRepository = await CreateRepository();

            await transactionRepository.PutTransaction(firstTransaction);

            // ACT

            await transactionRepository.PutTransaction(secondTransaction);

            var newCommands = await transactionRepository.GetCommands(versionTwoCommandQuery);

            // ASSERT

            secondTransaction.Commands.Length.ShouldBe(1);

            secondTransaction.Commands[0].ExpectedPreviousVersionNumber.ShouldBe(1ul);

            newCommands.Length.ShouldBe(1);

            newCommands[0].ShouldBeEquivalentTo(expectedCommand);
        }

        [Theory]
        [InlineData(60, 20, 30)]
        public async Task GivenTransactionAlreadyInserted_WhenQueryingByTransactionTimeStamp_ThenReturnExpectedObjects(int timeSpanInMinutes, int gteInMinutes, int lteInMinutes)
        {
            var originTimeStamp = DateTime.UnixEpoch;

            var transactions = new List<ITransaction<TransactionEntity>>();
            var expectedObjects = new ExpectedObjects();

            var transactionIds = GetSortedGuids(timeSpanInMinutes);
            var entityIds = GetSortedGuids(timeSpanInMinutes);

            DateTime? gte = null;
            DateTime? lte = null;

            for (var i = 1; i <= timeSpanInMinutes; i++)
            {
                var currentTransactionId = transactionIds[i - 1];
                var currentEntityId = entityIds[i - 1];

                var currentTimeStamp = originTimeStamp.AddMinutes(i);

                var source = new Counter(i);

                var commands = new ICommand<TransactionEntity>[]
                {
                    new Count(i),
                };

                var facts = new IFact<TransactionEntity>[]
                {
                    new Counted(i),
                    _serviceProvider.GetVersionNumberFact<TransactionEntity>(1),
                };

                var leases = new[]
                {
                    new CountLease(i),
                };

                var tags = new[]
                {
                    new CountTag(i),
                };

                expectedObjects.Add(gteInMinutes <= i && i <= lteInMinutes, currentTransactionId, currentEntityId, source, commands, facts, leases, tags);

                if (i == lteInMinutes)
                {
                    lte = currentTimeStamp;
                }
                else if (i == gteInMinutes)
                {
                    gte = currentTimeStamp;
                }

                var transaction = BuildTransaction(currentTransactionId, currentEntityId, source, commands, currentTimeStamp);

                transactions.Add(transaction);
            }

            gte.ShouldNotBeNull();
            lte.ShouldNotBeNull();

            var query = new TransactionTimeStampQuery(gte!.Value, lte!.Value);

            await TestGetTransactionIds(query as ISourceQuery, transactions, expectedObjects);
            await TestGetTransactionIds(query as ICommandQuery, transactions, expectedObjects);
            await TestGetTransactionIds(query as IFactQuery, transactions, expectedObjects);
            await TestGetTransactionIds(query as ILeaseQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as ISourceQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as ICommandQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as IFactQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as ILeaseQuery, transactions, expectedObjects);
            await TestGetSources(query, transactions, expectedObjects);
            await TestGetCommands(query, transactions, expectedObjects);
            await TestGetFacts(query, transactions, expectedObjects);
            await TestGetLeases(query, transactions, expectedObjects);
            await TestGetTags(query, transactions, expectedObjects);
        }

        [Theory]
        [InlineData(10, 5)]
        public async Task GivenTransactionAlreadyInserted_WhenQueryingByTransactionId_ThenReturnExpectedObjects(int numberOfTransactionIds, int whichTransactionId)
        {
            var transactions = new List<ITransaction<TransactionEntity>>();
            var expectedObjects = new ExpectedObjects();

            Guid? transactionId = null;

            var transactionIds = GetSortedGuids(numberOfTransactionIds);
            var entityIds = GetSortedGuids(numberOfTransactionIds);

            for (var i = 1; i <= numberOfTransactionIds; i++)
            {
                var currentTransactionId = transactionIds[i - 1];
                var currentEntityId = entityIds[i - 1];

                var source = new Counter(i);

                var commands = new ICommand<TransactionEntity>[]
                {
                    new Count(i),
                };

                var facts = new IFact<TransactionEntity>[]
                {
                    new Counted(i),
                    _serviceProvider.GetVersionNumberFact<TransactionEntity>(1),
                };

                var leases = new[]
                {
                    new CountLease(i),
                };

                var tags = new[]
                {
                    new CountTag(i),
                };

                expectedObjects.Add(i == whichTransactionId, currentTransactionId, currentEntityId, source, commands, facts, leases, tags);

                if (i == whichTransactionId)
                {
                    transactionId = currentTransactionId;
                }

                var transaction = BuildTransaction(currentTransactionId, currentEntityId, source, commands);

                transactions.Add(transaction);
            }

            transactionId.ShouldNotBeNull();

            var query = new TransactionIdQuery(transactionId!.Value);

            await TestGetTransactionIds(query as ISourceQuery, transactions, expectedObjects);
            await TestGetTransactionIds(query as ICommandQuery, transactions, expectedObjects);
            await TestGetTransactionIds(query as IFactQuery, transactions, expectedObjects);
            await TestGetTransactionIds(query as ILeaseQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as ISourceQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as ICommandQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as IFactQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as ILeaseQuery, transactions, expectedObjects);
            await TestGetSources(query, transactions, expectedObjects);
            await TestGetCommands(query, transactions, expectedObjects);
            await TestGetFacts(query, transactions, expectedObjects);
            await TestGetLeases(query, transactions, expectedObjects);
            await TestGetTags(query, transactions, expectedObjects);
        }

        [Theory]
        [InlineData(10, 5)]
        public async Task GivenTransactionAlreadyInserted_WhenQueryingByEntityId_ThenReturnExpectedObjects(int numberOfEntityIds, int whichEntityId)
        {
            var transactions = new List<ITransaction<TransactionEntity>>();
            var expectedObjects = new ExpectedObjects();

            Guid? entityId = null;

            var transactionIds = GetSortedGuids(numberOfEntityIds);
            var entityIds = GetSortedGuids(numberOfEntityIds);

            for (var i = 1; i <= numberOfEntityIds; i++)
            {
                var currentTransactionId = transactionIds[i - 1];
                var currentEntityId = entityIds[i - 1];

                var source = new Counter(i);

                var commands = new ICommand<TransactionEntity>[]
                {
                    new Count(i),
                };

                var facts = new IFact<TransactionEntity>[]
                {
                    new Counted(i),
                    _serviceProvider.GetVersionNumberFact<TransactionEntity>(1),
                };

                var leases = new[]
                {
                    new CountLease(i),
                };

                var tags = new[]
                {
                    new CountTag(i),
                };

                expectedObjects.Add(i == whichEntityId, currentTransactionId, currentEntityId, source, commands, facts, leases, tags);

                if (i == whichEntityId)
                {
                    entityId = currentEntityId;
                }

                var transaction = BuildTransaction(currentTransactionId, currentEntityId, source, commands);

                transactions.Add(transaction);
            }

            entityId.ShouldNotBeNull();

            var query = new EntityIdQuery(entityId!.Value);

            await TestGetTransactionIds(query as ISourceQuery, transactions, expectedObjects);
            await TestGetTransactionIds(query as ICommandQuery, transactions, expectedObjects);
            await TestGetTransactionIds(query as IFactQuery, transactions, expectedObjects);
            await TestGetTransactionIds(query as ILeaseQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as ISourceQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as ICommandQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as IFactQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as ILeaseQuery, transactions, expectedObjects);
            await TestGetSources(query, transactions, expectedObjects);
            await TestGetCommands(query, transactions, expectedObjects);
            await TestGetFacts(query, transactions, expectedObjects);
            await TestGetLeases(query, transactions, expectedObjects);
            await TestGetTags(query, transactions, expectedObjects);
        }

        [Theory]
        [InlineData(20, 5, 15)]
        public async Task GivenTransactionAlreadyInserted_WhenQueryingByEntityVersionNumber_ThenReturnExpectedObjects(int numberOfVersionNumbers, int gteAsInt, int lteAsInt)
        {
            var commands = new List<ICommand<TransactionEntity>>();
            var expectedObjects = new ExpectedObjects();

            for (var i = 1; i <= numberOfVersionNumbers; i++)
            {
                var command = new Count(i);

                var facts = new[]
                {
                    new Counted(i),
                    _serviceProvider.GetVersionNumberFact<TransactionEntity>((ulong)i),
                };

                var leases = new[]
                {
                    new CountLease(i),
                };

                var tags = new[]
                {
                    new CountTag(i),
                };

                commands.Add(command);

                expectedObjects.Add(gteAsInt <= i && i <= lteAsInt, default, default, default!, new[] { command }, facts, leases, tags);
            }

            var transaction = BuildTransaction(Guid.NewGuid(), Guid.NewGuid(), new NoSource(), commands.ToArray());

            var transactions = new List<ITransaction<TransactionEntity>> { transaction };

            var query = new EntityVersionNumberQuery((ulong)gteAsInt, (ulong)lteAsInt);

            await TestGetCommands(query, transactions, expectedObjects);
            await TestGetFacts(query, transactions, expectedObjects);
            await TestGetLeases(query, transactions, expectedObjects);
            await TestGetTags(query, transactions, expectedObjects);
        }

        [Theory]
        [InlineData(20, 5, 15)]
        public async Task GivenTransactionAlreadyInserted_WhenQueryingByData_ThenReturnExpectedObjects(int countTo, int gte, int lte)
        {
            var transactions = new List<ITransaction<TransactionEntity>>();
            var expectedObjects = new ExpectedObjects();

            var transactionIds = GetSortedGuids(countTo);
            var entityIds = GetSortedGuids(countTo);

            for (var i = 1; i <= countTo; i++)
            {
                var currentTransactionId = transactionIds[i - 1];
                var currentEntityId = entityIds[i - 1];

                var source = new Counter(i);

                var commands = new ICommand<TransactionEntity>[]
                {
                    new Count(i),
                };

                var facts = new IFact<TransactionEntity>[]
                {
                    new Counted(i),
                };

                var leases = new[]
                {
                    new CountLease(i),
                };

                var tags = new[]
                {
                    new CountTag(i),
                };

                expectedObjects.Add(gte <= i && i <= lte, currentTransactionId, currentEntityId, source, commands, facts, leases, tags);

                var transaction = BuildTransaction(currentTransactionId, currentEntityId, source, commands);

                transactions.Add(transaction);
            }

            ISourceQuery FilterSources(ISourceQuery sourceQuery)
            {
                return sourceQuery.Filter(new CountFilter());
            }

            ICommandQuery FilterCommands(ICommandQuery commandQuery)
            {
                return commandQuery.Filter(new CountFilter());
            }

            IFactQuery FilterFacts(IFactQuery factQuery)
            {
                return factQuery.Filter(new CountFilter());
            }

            ILeaseQuery FilterLeases(ILeaseQuery leaseQuery)
            {
                return leaseQuery.Filter(new CountFilter());
            }

            ITagQuery FilterTags(ITagQuery tagQuery)
            {
                return tagQuery.Filter(new CountFilter());
            }

            var query = new CountQuery<TransactionEntity>(gte, lte);

            await TestGetTransactionIds(query, transactions, expectedObjects, FilterSources);
            await TestGetTransactionIds(query, transactions, expectedObjects, FilterCommands);
            await TestGetTransactionIds(query, transactions, expectedObjects, FilterFacts);
            await TestGetTransactionIds(query, transactions, expectedObjects, FilterLeases);
            await TestGetEntityIds(query, transactions, expectedObjects, FilterSources);
            await TestGetEntityIds(query, transactions, expectedObjects, FilterCommands);
            await TestGetEntityIds(query, transactions, expectedObjects, FilterFacts);
            await TestGetEntityIds(query, transactions, expectedObjects, FilterLeases);
            await TestGetSources(query, transactions, expectedObjects, FilterSources);
            await TestGetCommands(query, transactions, expectedObjects, FilterCommands);
            await TestGetFacts(query, transactions, expectedObjects, FilterFacts);
            await TestGetLeases(query, transactions, expectedObjects, FilterLeases);
            await TestGetTags(query, transactions, expectedObjects, FilterTags);
        }
    }
}
