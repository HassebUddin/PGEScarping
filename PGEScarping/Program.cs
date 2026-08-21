using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PGEScarping.Data;
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
        ApplicationConfiguration.Initialize();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());

        services.AddDbContext<TechnoDevContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("TechnoDevDbConnection")
                ?? throw new InvalidOperationException("TechnoDevDbConnection is required in appsettings.json");
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        });

        services.Configure<ScrapingOptions>(configuration.GetSection(ScrapingOptions.SectionName));
        services.AddScoped<IScrapingWebsiteRepository, ScrapingWebsiteRepository>();
        services.AddScoped<IScrapingModule, PgeScrapingService>();
        services.AddTransient<Form1>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Application.Run(scope.ServiceProvider.GetRequiredService<Form1>());
    }
}
