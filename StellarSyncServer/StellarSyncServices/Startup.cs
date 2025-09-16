using StellarSyncServices.Discord;
using StellarSyncShared.Data;
using StellarSyncShared.Metrics;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using StellarSyncShared.Utils;
using StellarSyncShared.Services;
using StackExchange.Redis;
using StellarSyncShared.Utils.Configuration;

namespace StellarSyncServices;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var config = app.ApplicationServices.GetRequiredService<IConfigurationService<StellarConfigurationBase>>();

        var metricServer = new KestrelMetricServer(config.GetValueOrDefault<int>(nameof(StellarConfigurationBase.MetricsPort), 4982));
        metricServer.Start();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var stellarConfig = Configuration.GetSection("StellarSync");

        services.AddDbContextPool<StellarDbContext>(options =>
        {
            options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), builder =>
            {
                builder.MigrationsHistoryTable("_efmigrationshistory", "public");
            }).UseSnakeCaseNamingConvention();
            options.EnableThreadSafetyChecks(false);
        }, Configuration.GetValue(nameof(StellarConfigurationBase.DbContextPoolSize), 1024));
        services.AddDbContextFactory<StellarDbContext>(options =>
        {
            options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), builder =>
            {
                builder.MigrationsHistoryTable("_efmigrationshistory", "public");
                builder.MigrationsAssembly("StellarSyncShared");
            }).UseSnakeCaseNamingConvention();
            options.EnableThreadSafetyChecks(false);
        });

        services.AddSingleton(m => new StellarMetrics(m.GetService<ILogger<StellarMetrics>>(), new List<string> { },
        new List<string> { }));

        var redis = stellarConfig.GetValue(nameof(ServerConfiguration.RedisConnectionString), string.Empty);
        var options = ConfigurationOptions.Parse(redis);
        options.ClientName = "Stellar";
        options.ChannelPrefix = "UserData";
        ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(options);
        services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);

        services.Configure<ServicesConfiguration>(Configuration.GetRequiredSection("StellarSync"));
        services.Configure<ServerConfiguration>(Configuration.GetRequiredSection("StellarSync"));
        services.Configure<StellarConfigurationBase>(Configuration.GetRequiredSection("StellarSync"));
        services.AddSingleton(Configuration);
        services.AddSingleton<ServerTokenGenerator>();
        services.AddSingleton<DiscordBotServices>();
        services.AddHostedService<DiscordBot>();
        services.AddSingleton<IConfigurationService<ServicesConfiguration>, StellarConfigurationServiceServer<ServicesConfiguration>>();
        services.AddSingleton<IConfigurationService<ServerConfiguration>, StellarConfigurationServiceClient<ServerConfiguration>>();
        services.AddSingleton<IConfigurationService<StellarConfigurationBase>, StellarConfigurationServiceClient<StellarConfigurationBase>>();

        services.AddHostedService(p => (StellarConfigurationServiceClient<StellarConfigurationBase>)p.GetService<IConfigurationService<StellarConfigurationBase>>());
        services.AddHostedService(p => (StellarConfigurationServiceClient<ServerConfiguration>)p.GetService<IConfigurationService<ServerConfiguration>>());
    }
}