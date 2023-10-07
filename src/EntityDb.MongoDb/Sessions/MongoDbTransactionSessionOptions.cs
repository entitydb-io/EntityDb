using EntityDb.Abstractions.Transactions;
using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;

namespace EntityDb.MongoDb.Sessions;

/// <summary>
///     Configuration options for the MongoDb implementation of <see cref="ITransactionRepository"/>.
/// </summary>
public sealed class MongoDbTransactionSessionOptions
{
    /// <summary>
    ///     A connection string that is compatible with <see cref="MongoClient"/>
    /// </summary>
    public string ConnectionString { get; set; } = default!;

    /// <summary>
    ///     The name of the database that contains the collections (AgentSignatures, Commands, Tags, Leases)
    /// </summary>
    public string DatabaseName { get; set; } = default!;

    /// <summary>
    ///     If <c>true</c>, indicates the agent only intends to execute queries.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    ///     If <c>true</c>, indicates the agent can tolerate replication lag for queries.
    /// </summary>
    public bool SecondaryPreferred { get; set; }

    /// <summary>
    ///     Determines how long to wait before a command should be automatically aborted.
    /// </summary>
    public TimeSpan? WriteTimeout { get; set; }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage(Justification = "This is only overridden to make test names better.")]
    public override string ToString()
    {
        return $"{nameof(MongoDbTransactionSessionOptions)}";
    }
}
