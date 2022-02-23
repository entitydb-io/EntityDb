using Bogus;
using EntityDb.Abstractions.Transactions.Steps;
using EntityDb.Common.Extensions;
using EntityDb.Common.Tests.Implementations.Entities;
using EntityDb.Common.Transactions.Steps;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace EntityDb.Common.Tests.Implementations.Seeders
{
    public static class TransactionStepSeeder
    {
        public static ImmutableArray<ITransactionStep<TransactionEntity>> Create
        (
            int generateCount,
            int repeatCount,
            ulong? previousEntityVersionNumber = null,
            bool wellBehavedNextEntityVersionNumber = true,
            Guid? entityId = null,
            bool insertLease = false,
            bool deleteLease = false,
            bool insertTag = false,
            bool deleteTag = false
        )
        {
            var faker = new Faker<GeneralTransactionStep<TransactionEntity>>()
                .RuleFor
                (
                    transactionStep => transactionStep.PreviousEntityVersionNumber,
                    () => previousEntityVersionNumber ?? default
                )
                .RuleFor
                (
                    transactionStep => transactionStep.PreviousEntitySnapshot,
                    (_, transactionStep) => new TransactionEntity().Reduce(Enumerable.Repeat(CommandSeeder.Create(),
                        (int)transactionStep.PreviousEntityVersionNumber))
                )
                .RuleFor
                (
                    transactionStep => transactionStep.Command,
                    () => CommandSeeder.Create()
                )
                .RuleFor
                (
                    transactionStep => transactionStep.NextEntityVersionNumber,
                    (_, transactionStep) => wellBehavedNextEntityVersionNumber
                        ? transactionStep.PreviousEntityVersionNumber + 1
                        : transactionStep.PreviousEntityVersionNumber
                )
                .RuleFor
                (
                    transactionStep => transactionStep.NextEntitySnapshot,
                    (_, transactionStep) => transactionStep.PreviousEntitySnapshot.Reduce(transactionStep.Command)
                )
                .RuleFor
                (
                    transactionStep => transactionStep.EntityId,
                    () => entityId ?? Guid.NewGuid()
                )
                .RuleFor
                (
                    transactionStep => transactionStep.Leases,
                    () => TransactionMetaDataSeeder.ForLease(insertLease, deleteLease)
                )
                .RuleFor
                (
                    transactionStep => transactionStep.Tags,
                    () => TransactionMetaDataSeeder.ForTag(insertTag, deleteTag)
                );

            return Enumerable
                .Repeat(faker.Generate(generateCount), repeatCount)
                .SelectMany(x => x)
                .ToImmutableArray<ITransactionStep<TransactionEntity>>();
        }
    }
}
