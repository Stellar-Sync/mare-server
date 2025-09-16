using Microsoft.AspNetCore.SignalR;
using StellarSync.API.SignalR;
using StellarSyncServer.Hubs;

namespace StellarSyncStaticFilesServer.Services;

public class MainClientReadyMessageService : IClientReadyMessageService
{
    private readonly ILogger<MainClientReadyMessageService> _logger;
    private readonly IHubContext<StellarHub> _stellarHub;

    public MainClientReadyMessageService(ILogger<MainClientReadyMessageService> logger, IHubContext<StellarHub> stellarHub)
    {
        _logger = logger;
        _stellarHub = stellarHub;
    }

    public async Task SendDownloadReady(string uid, Guid requestId)
    {
        _logger.LogInformation("Sending Client Ready for {uid}:{requestId} to SignalR", uid, requestId);
        await _stellarHub.Clients.User(uid).SendAsync(nameof(IStellarHub.Client_DownloadReady), requestId).ConfigureAwait(false);
    }
}
