using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker.Extensions;
using Repositories.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Services.Graph;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Common.Models;
using Microsoft.Extensions.Configuration;
using AutoMapper;
using Microsoft.Graph.Models;
using Shared;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, builder) => {
        builder
            .AddJsonFile(Path.Combine(context.HostingEnvironment.ContentRootPath, "appsettings.json"), optional: true, reloadOnChange: false)
            .AddJsonFile(Path.Combine(context.HostingEnvironment.ContentRootPath, "appsettings.local.json"), optional: true, reloadOnChange: false)
            .AddJsonFile(Path.Combine(context.HostingEnvironment.ContentRootPath, $"appsettings.{context.HostingEnvironment.EnvironmentName}.json"), optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
        ;
        var builtConfig = builder.Build();
        var keyVaultEndpoint = builtConfig["AzureKeyVaultEndpoint"];

        if (!string.IsNullOrEmpty(keyVaultEndpoint))
        {

            var config = builder.AddAzureKeyVault(new Uri(keyVaultEndpoint), new DefaultAzureCredential(), new AzureKeyVaultConfigurationOptions { ReloadInterval = TimeSpan.FromHours(24) })
                .Build();

            //var kvSecret = config["kvSecretName"];

        }
    })
    .ConfigureServices(s => {
        s.AddHttpClient();

        
        s.AddApplicationInsightsTelemetryWorkerService(
            opts => {
                opts.EnablePerformanceCounterCollectionModule = false;
            });
        s.ConfigureFunctionsApplicationInsights();

        s.AddScoped<IGraphShadowWriter, GraphShadowWriter>();
        s.AddScoped<IGraphService, GraphService>();

        s.AddOptions<GraphSettings>()
        .Configure<IConfiguration>((settings, configuration) =>
        {
            var c = configuration.GetSection("GraphSettings");
            configuration.GetSection("GraphSettings").Bind(settings);
        });

        s.AddOptions<AppSettings>()
        .Configure<IConfiguration>((settings, configuration) =>
        {
            var c = configuration.GetSection("AppSettings");
            configuration.GetSection("AppSettings").Bind(settings);
        });

        s.AddOptions<CosmosSettings>()
        .Configure<IConfiguration>((settings, configuration) =>
        {
            var c = configuration.GetSection("CosmosSettings");
            configuration.GetSection("CosmosSettings").Bind(settings);
        });

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<User, UserEntity>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.OdataType, opt => opt.MapFrom(src => src.OdataType))
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.DisplayName))
            .ForMember(dest => dest.UserPrincipalName, opt => opt.MapFrom(src => src.UserPrincipalName))
            .ForMember(dest => dest.GivenName, opt => opt.MapFrom(src => src.GivenName))
            .ForMember(dest => dest.SurName, opt => opt.MapFrom(src => src.Surname))
            .ForMember(dest => dest.MobilePhone, opt => opt.MapFrom(src => src.MobilePhone))
            .ReverseMap();

            cfg.CreateMap<Group, GroupEntity>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.OdataType, opt => opt.MapFrom(src => src.OdataType))
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.DisplayName))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.AdditionalData, opt => opt.MapFrom(src => src.AdditionalData))
            .ForMember(dest => dest.Members, opt => opt.MapFrom(src => src.Members))
            .ReverseMap();
        });
        s.AddSingleton(mapperConfig.CreateMapper());

        s.Configure<LoggerFilterOptions>(options =>
        {
            LoggerFilterRule toRemove = options.Rules.FirstOrDefault(rule => rule.ProviderName=="Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider")!;
            if (toRemove is not null)
            {
                options.Rules.Remove(toRemove);
            }
        });
    })
    .Build();



host.Run();
