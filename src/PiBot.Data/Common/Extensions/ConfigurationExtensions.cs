using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PiBot.Data.Common.Extensions;

public static class ConfigurationExtensions
{
    public static IHostApplicationBuilder AddPg<TInterface, TContext, TConfig>(this IHostApplicationBuilder builder)
        where TInterface : class
        where TContext : PgContextBase<TConfig>, TInterface
        where TConfig : PgConfigBase
    {
        var section = builder.Configuration.GetSection(typeof(TConfig).Name);
        if (!section.Exists())
        {
            throw new InvalidOperationException($"Configuration section is missing: {typeof(TConfig).Name}");
        }

        builder.Services.Configure<TConfig>(section);
        builder.Services.AddDbContext<TInterface, TContext>();

        return builder;
    }

    public static IApplicationBuilder UsePg<TContext, TConfig>(this IApplicationBuilder app)
        where TContext : PgContextBase<TConfig>
        where TConfig : PgConfigBase
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        context.Database.Migrate();
        return app;
    }
}