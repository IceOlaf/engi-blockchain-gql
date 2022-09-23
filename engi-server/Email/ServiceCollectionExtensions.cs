using RazorLight;
using RazorLight.Extensions;
using SendGrid;

namespace Engi.Substrate.Server.Email;

public static class ServiceCollectionExtensions
{
    public static void AddSendgrid(
        this IServiceCollection services,
        IConfigurationSection section)
    {
        var options = section.Get<SendgridOptions>();

        services.AddTransient(_ => new SendGridClient(options.ApiKey));
    }

    public static void AddRazorLight(
        this IServiceCollection services,
        IWebHostEnvironment environment)
    {
        var engine = new RazorLightEngineBuilder()
            .UseFileSystemProject($"{environment.ContentRootPath}/Email/Templates")
            .UseMemoryCachingProvider()
            .EnableDebugMode(environment.IsDevelopment())
            .Build();

        services.AddRazorLight(() => engine);
        services.AddSingleton(engine.Handler.Compiler);
        services.AddSingleton(engine.Handler.FactoryProvider);
        services.AddSingleton(engine.Handler.Cache);
    }
}