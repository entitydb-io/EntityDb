﻿using EntityDb.Abstractions.ValueObjects;
using EntityDb.Common.Transactions.Builders;
using System;

namespace EntityDb.Common.Exceptions;

/// <summary>
///     The exception that is thrown when an actor passes an entity id to
///     <see cref="TransactionBuilder{TEntity}.Load(Id, TEntity)" />
///     with an entity id that loads with a version number of zero.
/// </summary>
public sealed class EntityNotCreatedException : Exception
{
}
