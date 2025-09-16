using Microsoft.EntityFrameworkCore;
using StellarSyncServer.Hubs;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using AspNetCoreRateLimit;
using StellarSyncShared.Data;
using StellarSyncShared.Metrics;
using StellarSyncServer.Services;
using StellarSyncShared.Utils;
using StellarSyncShared.Services;
using Prometheus;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Configuration;
using System.Net;
using StackExchange.Redis.Extensions.System.Text.Json;
using StellarSync.API.SignalR;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Mvc.Controllers;
using StellarSyncServer.Controllers;
using StellarSyncShared.RequirementHandlers;
using StellarSyncShared.Utils.Configuration;

namespace StellarSyncServer;

public class Startup
{
    private readonly ILogger<Startup> _logger;

    public Startup(IConfiguration configuration, ILogger<Startup> logger)
    {
        Configuration = configuration;
        _logger = logger;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddTransient(_ => Configuration);

        var stellarConfig = Configuration.GetRequiredSection("StellarSync");

        // configure metrics
        ConfigureMetrics(services);

        // configure database
        ConfigureDatabase(services, stellarConfig);

        // configure authentication and authorization
        ConfigureAuthorization(services);

        // configure rate limiting
        ConfigureIpRateLimiting(services);

        // configure SignalR
        ConfigureSignalR(services, stellarConfig);

        // configure stellar specific services
        ConfigureStellarServices(services, stellarConfig);

        services.AddHealthChecks();
        services.AddControllers().ConfigureApplicationPartManager(a =>
        {
            a.FeatureProviders.Remove(a.FeatureProviders.OfType<ControllerFeatureProvider>().First());
            if (stellarConfig.GetValue<Uri>(nameof(ServerConfiguration.MainServerAddress), defaultValue: null) == null)
            {
                a.FeatureProviders.Add(new AllowedControllersFeatureProvider(typeof(StellarServerConfigurationController), typeof(StellarBaseConfigurationController), typeof(ClientMessageController)));
            }
            else
            {
                a.FeatureProviders.Add(new AllowedControllersFeatureProvider());
            }
        });
    }

    private void ConfigureStellarServices(IServiceCollection services, IConfigurationSection stellarConfig)
    {
        bool isMainServer = stellarConfig.GetValue<Uri>(nameof(ServerConfiguration.MainServerAddress), defaultValue: null) == null;

        services.Configure<ServerConfiguration>(Configuration.GetRequiredSection("StellarSync"));
        services.Configure<StellarConfigurationBase>(Configuration.GetRequiredSection("StellarSync"));

        services.AddSingleton<ServerTokenGenerator>();
        services.AddSingleton<SystemInfoService>();
        services.AddSingleton<OnlineSyncedPairCacheService>();
        services.AddHostedService(provider => provider.GetService<SystemInfoService>());
        // configure services based on main server status
        ConfigureServicesBasedOnShardType(services, stellarConfig, isMainServer);

        services.AddSingleton(s => new StellarCensus(s.GetRequiredService<ILogger<StellarCensus>>()));
        services.AddHostedService(p => p.GetRequiredService<StellarCensus>());

        if (isMainServer)
        {
            services.AddSingleton<UserCleanupService>();
            services.AddHostedService(provider => provider.GetService<UserCleanupService>());
            services.AddSingleton<CharaDataCleanupService>();
            services.AddHostedService(provider => provider.GetService<CharaDataCleanupService>());
            services.AddHostedService<ClientPairPermissionsCleanupService>();
        }

        services.AddSingleton<GPoseLobbyDistributionService>();
        services.AddHostedService(provider => provider.GetService<GPoseLobbyDistributionService>());
    }

    private static void ConfigureSignalR(IServiceCollection services, IConfigurationSection stellarConfig)
    {
        services.AddSingleton<IUserIdProvider, IdBasedUserIdProvider>();
        services.AddSingleton<ConcurrencyFilter>();

        var signalRServiceBuilder = services.AddSignalR(hubOptions =>
        {
            hubOptions.MaximumReceiveMessageSize = long.MaxValue;
            hubOptions.EnableDetailedErrors = true;
            hubOptions.MaximumParallelInvocationsPerClient = 10;
            hubOptions.StreamBufferCapacity = 200;

            hubOptions.AddFilter<SignalRLimitFilter>();
            hubOptions.AddFilter<ConcurrencyFilter>();
        }).AddMessagePackProtocol(opt =>
        {
            var resolver = CompositeResolver.Create(StandardResolverAllowPrivate.Instance,
                BuiltinResolver.Instance,
                AttributeFormatterResolver.Instance,
                // replace enum resolver
                DynamicEnumAsStringResolver.Instance,
                DynamicGenericResolver.Instance,
                DynamicUnionResolver.Instance,
                DynamicObjectResolver.Instance,
                PrimitiveObjectResolver.Instance,
                // final fallback(last priority)
                StandardResolver.Instance);

            opt.SerializerOptions = MessagePackSerializerOptions.Standard
                .WithCompression(MessagePackCompression.Lz4Block)
                .WithResolver(resolver);
        });


        // configure redis for SignalR
        var redisConnection = stellarConfig.GetValue(nameof(ServerConfiguration.RedisConnectionString), string.Empty);
        signalRServiceBuilder.AddStackExchangeRedis(redisConnection, options => { });

        var options = ConfigurationOptions.Parse(redisConnection);

        var endpoint = options.EndPoints[0];
        string address = "";
        int port = 0;
        if (endpoint is DnsEndPoint dnsEndPoint) { address = dnsEndPoint.Host; port = dnsEndPoint.Port; }
        if (endpoint is IPEndPoint ipEndPoint) { address = ipEndPoint.Address.ToString(); port = ipEndPoint.Port; }
        var redisConfiguration = new RedisConfiguration()
        {
            AbortOnConnectFail = true,
            KeyPrefix = "",
            Hosts = new RedisHost[]
            {
                new RedisHost(){ Host = address, Port = port },
            },
            AllowAdmin = true,
            ConnectTimeout = options.ConnectTimeout,
            Database = 0,
            Ssl = false,
            Password = options.Password,
            ServerEnumerationStrategy = new ServerEnumerationStrategy()
            {
                Mode = ServerEnumerationStrategy.ModeOptions.All,
                TargetRole = ServerEnumerationStrategy.TargetRoleOptions.Any,
                UnreachableServerAction = ServerEnumerationStrategy.UnreachableServerActionOptions.Throw,
            },
            MaxValueLength = 1024,
            PoolSize = stellarConfig.GetValue(nameof(ServerConfiguration.RedisPool), 50),
            SyncTimeout = options.SyncTimeout,
        };

        services.AddStackExchangeRedisExtensions<SystemTextJsonSerializer>(redisConfiguration);
    }

    private void ConfigureIpRateLimiting(IServiceCollection services)
    {
        services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
        services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddMemoryCache();
        services.AddInMemoryRateLimiting();
    }

    private static void ConfigureAuthorization(IServiceCollection services)
    {
        services.AddTransient<IAuthorizationHandler, UserRequirementHandler>();
        services.AddTransient<IAuthorizationHandler, ValidTokenRequirementHandler>();
        services.AddTransient<IAuthorizationHandler, ValidTokenHubRequirementHandler>();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IConfigurationService<StellarConfigurationBase>>((options, config) =>
            {
                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = false,
                    ValidateLifetime = true,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(config.GetValue<string>(nameof(StellarConfigurationBase.Jwt)))),
                };
            });

        services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer();

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser().Build();
            options.AddPolicy("Authenticated", policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new ValidTokenRequirement());
            });
            options.AddPolicy("Identified", policy =>
            {
                policy.AddRequirements(new UserRequirement(UserRequirements.Identified));
                policy.AddRequirements(new ValidTokenRequirement());

            });
            options.AddPolicy("Admin", policy =>
            {
                policy.AddRequirements(new UserRequirement(UserRequirements.Identified | UserRequirements.Administrator));
                policy.AddRequirements(new ValidTokenRequirement());

            });
            options.AddPolicy("Moderator", policy =>
            {
                policy.AddRequirements(new UserRequirement(UserRequirements.Identified | UserRequirements.Moderator | UserRequirements.Administrator));
                policy.AddRequirements(new ValidTokenRequirement());
            });
            options.AddPolicy("Internal", new AuthorizationPolicyBuilder().RequireClaim(StellarClaimTypes.Internal, "true").Build());
        });
    }

    private void ConfigureDatabase(IServiceCollection services, IConfigurationSection stellarConfig)
    {
        services.AddDbContextPool<StellarDbContext>(options =>
        {
            options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), builder =>
            {
                builder.MigrationsHistoryTable("_efmigrationshistory", "public");
                builder.MigrationsAssembly("StellarSyncShared");
            }).UseSnakeCaseNamingConvention();
            options.EnableThreadSafetyChecks(false);
        }, stellarConfig.GetValue(nameof(StellarConfigurationBase.DbContextPoolSize), 1024));
        services.AddDbContextFactory<StellarDbContext>(options =>
        {
            options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), builder =>
            {
                builder.MigrationsHistoryTable("_efmigrationshistory", "public");
                builder.MigrationsAssembly("StellarSyncShared");
            }).UseSnakeCaseNamingConvention();
            options.EnableThreadSafetyChecks(false);
        });
    }

    private static void ConfigureMetrics(IServiceCollection services)
    {
        services.AddSingleton<StellarMetrics>(m => new StellarMetrics(m.GetService<ILogger<StellarMetrics>>(), new List<string>
        {
            MetricsAPI.CounterInitializedConnections,
            MetricsAPI.CounterUserPushData,
            MetricsAPI.CounterUserPushDataTo,
            MetricsAPI.CounterUsersRegisteredDeleted,
            MetricsAPI.CounterAuthenticationCacheHits,
            MetricsAPI.CounterAuthenticationFailures,
            MetricsAPI.CounterAuthenticationRequests,
            MetricsAPI.CounterAuthenticationSuccesses,
            MetricsAPI.CounterUserPairCacheHit,
            MetricsAPI.CounterUserPairCacheMiss,
            MetricsAPI.CounterUserPairCacheNewEntries,
            MetricsAPI.CounterUserPairCacheUpdatedEntries,
        }, new List<string>
        {
            MetricsAPI.GaugeAuthorizedConnections,
            MetricsAPI.GaugeConnections,
            MetricsAPI.GaugePairs,
            MetricsAPI.GaugePairsPaused,
            MetricsAPI.GaugeAvailableIOWorkerThreads,
            MetricsAPI.GaugeAvailableWorkerThreads,
            MetricsAPI.GaugeGroups,
            MetricsAPI.GaugeGroupPairs,
            MetricsAPI.GaugeUsersRegistered,
            MetricsAPI.GaugeAuthenticationCacheEntries,
            MetricsAPI.GaugeUserPairCacheEntries,
            MetricsAPI.GaugeUserPairCacheUsers,
            MetricsAPI.GaugeGposeLobbies,
            MetricsAPI.GaugeGposeLobbyUsers,
            MetricsAPI.GaugeHubConcurrency,
            MetricsAPI.GaugeHubQueuedConcurrency,
        }));
    }

    private static void ConfigureServicesBasedOnShardType(IServiceCollection services, IConfigurationSection stellarConfig, bool isMainServer)
    {
        if (!isMainServer)
        {
            services.AddSingleton<IConfigurationService<ServerConfiguration>, StellarConfigurationServiceClient<ServerConfiguration>>();
            services.AddSingleton<IConfigurationService<StellarConfigurationBase>, StellarConfigurationServiceClient<StellarConfigurationBase>>();

            services.AddHostedService(p => (StellarConfigurationServiceClient<ServerConfiguration>)p.GetService<IConfigurationService<ServerConfiguration>>());
            services.AddHostedService(p => (StellarConfigurationServiceClient<StellarConfigurationBase>)p.GetService<IConfigurationService<StellarConfigurationBase>>());
        }
        else
        {
            services.AddSingleton<IConfigurationService<ServerConfiguration>, StellarConfigurationServiceServer<ServerConfiguration>>();
            services.AddSingleton<IConfigurationService<StellarConfigurationBase>, StellarConfigurationServiceServer<StellarConfigurationBase>>();
        }
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
        logger.LogInformation("Running Configure");

        var config = app.ApplicationServices.GetRequiredService<IConfigurationService<StellarConfigurationBase>>();

        app.UseIpRateLimiting();

        app.UseRouting();

        app.UseWebSockets();
        app.UseHttpMetrics();

        var metricServer = new KestrelMetricServer(config.GetValueOrDefault<int>(nameof(StellarConfigurationBase.MetricsPort), 4980));
        metricServer.Start();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<StellarHub>(IStellarHub.Path, options =>
            {
                options.ApplicationMaxBufferSize = 5242880;
                options.TransportMaxBufferSize = 5242880;
                options.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling;
            });

            endpoints.MapHealthChecks("/health").AllowAnonymous();
            endpoints.MapControllers();

            foreach (var source in endpoints.DataSources.SelectMany(e => e.Endpoints).Cast<RouteEndpoint>())
            {
                if (source == null) continue;
                _logger.LogInformation("Endpoint: {url} ", source.RoutePattern.RawText);
            }
        });

    }
}
