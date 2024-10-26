using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PiBot.Core.CommandHandlers;

namespace PiBot.Core.Configuration;

public static class ConfigurationExtensions
{
    public static IHostApplicationBuilder AddCore(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<GrowHandler>();
        builder.Services.AddScoped<LeaderboardHandler>();
        builder.Services.AddScoped<MegaprikolHandler>();

        return builder;
    }
}