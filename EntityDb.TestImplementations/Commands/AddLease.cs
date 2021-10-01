﻿using EntityDb.Abstractions.Commands;
using EntityDb.Abstractions.Facts;
using EntityDb.TestImplementations.Entities;
using EntityDb.TestImplementations.Facts;
using System.Collections.Generic;

namespace EntityDb.TestImplementations.Commands
{
    public record AddLease(string LeaseScope, string LeaseLabel, string LeaseValue) : ICommand<TransactionEntity>
    {
        public IEnumerable<IFact<TransactionEntity>> Execute(TransactionEntity entity)
        {
            yield return new LeaseAdded(LeaseScope, LeaseLabel, LeaseValue);
        }
    }
}
