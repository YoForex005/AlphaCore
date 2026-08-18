using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TraderIntelligence.Application.Contracts;
using TraderIntelligence.Application.Dashboard;
using TraderIntelligence.Application.Ingestion;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Scoring;
using TraderIntelligence.Infrastructure.Dashboard;
using TraderIntelligence.Infrastructure.Persistence;
using TraderIntelligence.Mt5.Connectors;

namespace TraderIntelligence.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTraderIntelligence(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("TraderIntelligence")
                         ?? configuration["DATABASE_URL"];

        if (string.IsNullOrWhiteSpace(connection) || connection.Contains("<SECRET>", StringComparison.Ordinal))
        {
            services.AddDbContext<TraderDbContext>(o => o.UseInMemoryDatabase("trader-intelligence"));
        }
        else
        {
            services.AddDbContext<TraderDbContext>(o => o.UseNpgsql(connection));
        }

        var (achiever, starwave) = DemoBrokerFactory.CreateDefault();
        services.AddSingleton<IMt5BrokerConnector>(achiever);
        services.AddSingleton<IMt5BrokerConnector>(starwave);
        services.AddSingleton<IBrokerRegistry>(sp => new BrokerRegistry(sp.GetServices<IMt5BrokerConnector>()));

        services.AddScoped<ITradingStore, EfTradingStore>();
        services.AddScoped<IDashboardQueries, EfDashboardQueries>();
        services.AddSingleton<TradeReconstructor>();
        services.AddSingleton<BaselineScorer>();
        services.AddScoped<DealIngestionService>();
        services.AddScoped<ReconstructionScoringService>();
        return services;
    }
}
