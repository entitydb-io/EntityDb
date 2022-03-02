﻿using EntityDb.Abstractions.Transactions;
using EntityDb.Common.Transactions;
using System;

namespace EntityDb.Common.Exceptions;

/// <summary>
///     The exception that is thrown when an actor passes a <see cref="ITransaction" /> to an
///     <see cref="ITransactionRepository" /> that was created with
///     <see cref="TransactionSessionOptions.ReadOnly" /> equal to <c>true</c>.
/// </summary>
public class CannotWriteInReadOnlyModeException : Exception
{
}
