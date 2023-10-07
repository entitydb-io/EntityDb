using EntityDb.Abstractions.Sources;
using EntityDb.Common.Sources.Processors;
using EntityDb.Common.Sources.Processors.Queues;

namespace EntityDb.Common.Extensions;

/// <summary>
///     Extensions for <see cref="ISourceProcessorQueue"/>.
/// </summary>
public static class SourceProcessorQueueExtensions
{
    /// <summary>
    ///     Adds a transaction and the corresponding processor to the queue.
    /// </summary>
    /// <typeparam name="TSourceProcessor">The type of the <see cref="ISourceProcessor"/></typeparam>
    /// <param name="sourceProcessorQueue">The transaction processor queue</param>
    /// <param name="source">The transaction to process</param>
    public static void Enqueue<TSourceProcessor>(this ISourceProcessorQueue sourceProcessorQueue, ISource source) where TSourceProcessor : ISourceProcessor
    {
        sourceProcessorQueue.Enqueue(new SourceProcessorQueueItem
        {
            SourceProcessorType = typeof(TSourceProcessor),
            Source = source,
        });
    }
}
