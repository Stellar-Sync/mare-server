using StellarSyncShared.Metrics;
using StellarSyncShared.Services;
using StellarSyncShared.Utils.Configuration;
using StellarSyncStaticFilesServer.Services;

namespace StellarSyncStaticFilesServer.Utils;

public class RequestFileStreamResultFactory
{
    private readonly StellarMetrics _metrics;
    private readonly RequestQueueService _requestQueueService;
    private readonly IConfigurationService<StaticFilesServerConfiguration> _configurationService;

    public RequestFileStreamResultFactory(StellarMetrics metrics, RequestQueueService requestQueueService, IConfigurationService<StaticFilesServerConfiguration> configurationService)
    {
        _metrics = metrics;
        _requestQueueService = requestQueueService;
        _configurationService = configurationService;
    }

    public RequestFileStreamResult Create(Guid requestId, Stream stream)
    {
        return new RequestFileStreamResult(requestId, _requestQueueService,
            _metrics, stream, "application/octet-stream");
    }
}