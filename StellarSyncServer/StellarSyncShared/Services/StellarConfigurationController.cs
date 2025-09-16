using StellarSyncShared.Utils.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StellarSyncShared.Services;

[Route("configuration/[controller]")]
[Authorize(Policy = "Internal")]
public class StellarConfigurationController<T> : Controller where T : class, IStellarConfiguration
{
    private readonly ILogger<StellarConfigurationController<T>> _logger;
    private IOptionsMonitor<T> _config;

    public StellarConfigurationController(IOptionsMonitor<T> config, ILogger<StellarConfigurationController<T>> logger)
    {
        _config = config;
        _logger = logger;
    }

    [HttpGet("GetConfigurationEntry")]
    [Authorize(Policy = "Internal")]
    public IActionResult GetConfigurationEntry(string key, string defaultValue)
    {
        var result = _config.CurrentValue.SerializeValue(key, defaultValue);
        _logger.LogInformation("Requested " + key + ", returning:" + result);
        return Ok(result);
    }
}

#pragma warning disable MA0048 // File name must match type name
public class StellarStaticFilesServerConfigurationController : StellarConfigurationController<StaticFilesServerConfiguration>
{
    public StellarStaticFilesServerConfigurationController(IOptionsMonitor<StaticFilesServerConfiguration> config, ILogger<StellarStaticFilesServerConfigurationController> logger) : base(config, logger)
    {
    }
}

public class StellarBaseConfigurationController : StellarConfigurationController<StellarConfigurationBase>
{
    public StellarBaseConfigurationController(IOptionsMonitor<StellarConfigurationBase> config, ILogger<StellarBaseConfigurationController> logger) : base(config, logger)
    {
    }
}

public class StellarServerConfigurationController : StellarConfigurationController<ServerConfiguration>
{
    public StellarServerConfigurationController(IOptionsMonitor<ServerConfiguration> config, ILogger<StellarServerConfigurationController> logger) : base(config, logger)
    {
    }
}

public class StellarServicesConfigurationController : StellarConfigurationController<ServicesConfiguration>
{
    public StellarServicesConfigurationController(IOptionsMonitor<ServicesConfiguration> config, ILogger<StellarServicesConfigurationController> logger) : base(config, logger)
    {
    }
}
#pragma warning restore MA0048 // File name must match type name
