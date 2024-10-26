namespace PiBot.Telegram.Modules.Configuration;

internal static class ConfigurationExtensions
{
    internal static IServiceCollection AddModules(this IServiceCollection services)
    {
        var moduleTypes = typeof(ConfigurationExtensions).Assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(ModuleBase))).ToArray();
        foreach (var moduleType in moduleTypes)
        {
            if (Activator.CreateInstance(moduleType, true) is not ModuleBase module)
                throw new InvalidOperationException($"Module {moduleType.FullName} could not be instantiated");

            services.AddKeyedScoped(typeof(ModuleBase), module.CommandName, moduleType);
            foreach (var commandAlias in module.Aliases)
                services.AddKeyedScoped(typeof(ModuleBase), commandAlias, moduleType);
        }

        return services;
    }
}