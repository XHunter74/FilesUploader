using Microsoft.Extensions.Configuration;
using Serilog;

namespace xhunter74.FilesUploader.Extensions;

public static class LoggerConfigurationUtils
{
    public static LoggerConfiguration ConfigureLogger()
    {
        var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
                        .Build();

        var logConfig = new LoggerConfiguration()
            .ReadFrom
            .Configuration(configuration);

        return logConfig;
    }
}
