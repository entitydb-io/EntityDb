using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EntityDb.Common.Sources.Processors.Queues;

internal class TestModeSourceProcessorQueue : ISourceProcessorQueue
{
    private readonly ILogger<TestModeSourceProcessorQueue> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public TestModeSourceProcessorQueue(ILogger<TestModeSourceProcessorQueue> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    private async Task Process(ISourceProcessorQueueItem item, CancellationToken cancellationToken)
    {
        await using var serviceScope = _serviceScopeFactory.CreateAsyncScope();

        using var logScope = _logger.BeginScope(new KeyValuePair<string, object>[]
        {
            new("SourceProcessorType", item.SourceProcessorType.Name),
            new("SourceId", item.Source.Id.Value),
        });

        try
        {
            var sourceProcessor = (ISourceProcessor)serviceScope.ServiceProvider.GetRequiredService(item.SourceProcessorType);

            _logger.LogDebug("Started processing source");

            await sourceProcessor.Process(item.Source, cancellationToken);

            _logger.LogDebug("Finished processing source");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred while processing source");
        }
    }

    public void Enqueue(ISourceProcessorQueueItem item)
    {
        Task.Run(() => Process(item, default)).Wait();
    }
}
