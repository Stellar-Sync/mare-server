using StellarSyncShared.Utils;
using Microsoft.AspNetCore.Mvc;

namespace StellarSyncStaticFilesServer.Controllers;

public class ControllerBase : Controller
{
    protected ILogger _logger;

    public ControllerBase(ILogger logger)
    {
        _logger = logger;
    }

    protected string StellarUser => HttpContext.User.Claims.First(f => string.Equals(f.Type, StellarClaimTypes.Uid, StringComparison.Ordinal)).Value;
    protected string Continent => HttpContext.User.Claims.FirstOrDefault(f => string.Equals(f.Type, StellarClaimTypes.Continent, StringComparison.Ordinal))?.Value ?? "*";
    protected bool IsPriority => !string.IsNullOrEmpty(HttpContext.User.Claims.FirstOrDefault(f => string.Equals(f.Type, StellarClaimTypes.Alias, StringComparison.Ordinal))?.Value ?? string.Empty);
}
