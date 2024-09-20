using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PiBot.Data.Utils.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddPg<TContext>(this IHostApplicationBuilder builder)
        where TContext : PgContextBase
    {
        var section = builder.Configuration.GetSection(nameof(PgConfig));
        if (!section.Exists())
        {
            throw new InvalidOperationException($"Configuration section is missing: {nameof(PgConfig)}");
        }

        builder.Services.Configure<PgConfig>(section);
        builder.Services.AddDbContext<TContext>();

        return builder;
    }

    // use pg
    public static IApplicationBuilder UsePg<TContext>(this IApplicationBuilder app)
        where TContext : PgContextBase
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        context.Database.Migrate();
        return app;
    }
}