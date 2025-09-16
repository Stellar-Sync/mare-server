using Microsoft.AspNetCore.SignalR;

namespace StellarSyncShared.Utils;

public class IdBasedUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext context)
    {
        return context.User!.Claims.SingleOrDefault(c => string.Equals(c.Type, StellarClaimTypes.Uid, StringComparison.Ordinal))?.Value;
    }
}
