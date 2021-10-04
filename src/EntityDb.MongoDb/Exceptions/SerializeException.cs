﻿using System;

namespace EntityDb.MongoDb.Exceptions
{
    /// <summary>
    ///     The exception that is thrown when an object envelope cannot be serialized. Possible objects include: sources,
    ///     commands, facts, and leases.
    /// </summary>
    public sealed class SerializeException : Exception
    {
    }
}
