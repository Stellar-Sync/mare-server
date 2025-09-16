using StellarSync.API.Routes;
using StellarSyncStaticFilesServer.Services;
using StellarSyncStaticFilesServer.Utils;
using Microsoft.AspNetCore.Mvc;

namespace StellarSyncStaticFilesServer.Controllers;

[Route(StellarFiles.Cache)]
public class CacheController : ControllerBase
{
    private readonly RequestFileStreamResultFactory _requestFileStreamResultFactory;
    private readonly CachedFileProvider _cachedFileProvider;
    private readonly RequestQueueService _requestQueue;
    private readonly FileStatisticsService _fileStatisticsService;

    public CacheController(ILogger<CacheController> logger, RequestFileStreamResultFactory requestFileStreamResultFactory,
        CachedFileProvider cachedFileProvider, RequestQueueService requestQueue, FileStatisticsService fileStatisticsService) : base(logger)
    {
        _requestFileStreamResultFactory = requestFileStreamResultFactory;
        _cachedFileProvider = cachedFileProvider;
        _requestQueue = requestQueue;
        _fileStatisticsService = fileStatisticsService;
    }

    [HttpGet(StellarFiles.Cache_Get)]
    public async Task<IActionResult> GetFiles(Guid requestId)
    {
        _logger.LogDebug($"GetFile:{StellarUser}:{requestId}");

        if (!_requestQueue.IsActiveProcessing(requestId, StellarUser, out var request)) return BadRequest();

        _requestQueue.ActivateRequest(requestId);

        Response.ContentType = "application/octet-stream";

        long requestSize = 0;
        List<BlockFileDataSubstream> substreams = new();

        foreach (var fileHash in request.FileIds)
        {
            var fs = await _cachedFileProvider.DownloadAndGetLocalFileInfo(fileHash).ConfigureAwait(false);
            if (fs == null) continue;

            substreams.Add(new(fs));

            requestSize += fs.Length;
        }

        _fileStatisticsService.LogRequest(requestSize);

        return _requestFileStreamResultFactory.Create(requestId, new BlockFileDataStream(substreams));
    }
}
