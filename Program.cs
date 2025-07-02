using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System.Reflection;
using xhunter74.CollectionManager.API.Settings;
using xhunter74.FilesUploader.Extensions;
using xhunter74.FilesUploader.Services;


namespace xhunter74.FilesUploader;

public class Program
{
    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args);
        return builder
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                config.SetBasePath(hostContext.HostingEnvironment.ContentRootPath)
                    .AddJsonFile("appsettings.json", true, true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                        true, true)
                    .AddEnvironmentVariables();
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddOptions<AppSettings>()
                    .Bind(hostContext.Configuration.GetSection(AppSettings.ConfigSection))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services.AddHostedService<AppBackgroundService>();
                services.AddScoped<FilesService>();
                services.AddScoped(x =>
                {
                    var logger = x.GetRequiredService<ILogger<AzureStorageService>>();
                    var settings = x.GetRequiredService<IOptions<AppSettings>>().Value;
                    return new AzureStorageService(logger, settings.AzureConnectionString);
                });
            })
            .UseSerilog();
    }

    private static async Task RunAsync(string[] args, LoggerConfiguration logConfig)
    {
        Log.Logger = logConfig.CreateLogger();
        try
        {
            Log.Information("====================================================================");
            Log.Information($"FilesUploader Starts. Version: {Assembly.GetEntryAssembly()?.GetName().Version}");
            await CreateHostBuilder(args)
            .Build()
            .RunAsync();
        }
        catch (Exception e)
        {
            Log.Fatal(e, "FilesUploader terminated unexpectedly");
        }
        finally
        {
            Log.Information("====================================================================\r\n");
            await Log.CloseAndFlushAsync();
        }
    }

    public static async Task Main(string[] args)
    {
        try
        {
            var logConfig = LoggerConfigurationUtils.ConfigureLogger();
            await RunAsync(args, logConfig);
        }
        catch (Exception ex)
        {
            Log.Fatal($"Failed to start {Assembly.GetEntryAssembly().GetName().Name}", ex);
            throw;
        }
    }
}
