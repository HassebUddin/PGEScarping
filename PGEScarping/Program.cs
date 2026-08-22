using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PGEScarping.Data;
using PGEScarping.Helpers;
using PGEScarping.Interfaces;
using PGEScarping.Options;
using PGEScarping.Repositories;
using PGEScarping.Services;

namespace PGEScarping;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        var appLogFile = new AppLogFile();

        // .NET WinForms apps terminate the whole process on an unhandled UI-thread exception by
        // default (unlike classic .NET Framework, which showed a Continue/Quit dialog). Catching it
        // here keeps the app alive and puts the real error somewhere visible instead of a silent exit.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
        {
            appLogFile.Append("UNHANDLED UI EXCEPTION:" + Environment.NewLine + e.Exception);
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nDetails were written to:\n{appLogFile.FilePath}",
                "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            appLogFile.Append("FATAL UNHANDLED EXCEPTION:" + Environment.NewLine + e.ExceptionObject);
        };

        ApplicationConfiguration.Initialize();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton(appLogFile);
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.AddProvider(new FileLoggerProvider(appLogFile));
        });

        services.AddDbContext<TechnoDevContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("TechnoDevDbConnection")
                ?? throw new InvalidOperationException("TechnoDevDbConnection is required in appsettings.json");
            // A pinned version avoids connecting to the DB at app startup (ServerVersion.AutoDetect
            // would do that eagerly); the first real connection happens lazily when a module runs.
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 33)),
                mySqlOptions => mySqlOptions.EnableRetryOnFailure());
        });

        services.Configure<ScrapingOptions>(configuration.GetSection(ScrapingOptions.SectionName));
        services.Configure<EmailInboxOptions>(configuration.GetSection(EmailInboxOptions.SectionName));
        services.AddScoped<IScrapingWebsiteRepository, ScrapingWebsiteRepository>();
        services.AddScoped<IScrapingModule, PgeScrapingService>();
        services.AddTransient<Form1>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Application.Run(scope.ServiceProvider.GetRequiredService<Form1>());
    }
}
