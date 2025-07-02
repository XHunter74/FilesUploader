using Microsoft.Extensions.Configuration;
using Serilog;

namespace xhunter74.FilesUploader.Extensions;

/// <summary>
/// Provides utility methods for configuring Serilog logger from application configuration files.
/// </summary>
public static class LoggerConfigurationUtils
{
    /// <summary>
    /// Configures and returns a Serilog <see cref="LoggerConfiguration"/> using appsettings files.
    /// </summary>
    /// <returns>A configured <see cref="LoggerConfiguration"/> instance.</returns>
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
