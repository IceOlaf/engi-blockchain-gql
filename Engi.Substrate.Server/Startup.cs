using GraphQL;
using GraphQL.Execution;
using GraphQL.Server;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.SystemReactive;
using Microsoft.Extensions.Options;
using Polly;

namespace Engi.Substrate.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks();

            services.Configure<SubstrateClientOptions>(Configuration.GetRequiredSection("Substrate"));

            GraphQL.MicrosoftDI.GraphQLBuilderExtensions.AddGraphQL(services)
                .AddSubscriptionDocumentExecuter()
                .AddServer(true)
                .AddSchema<EngiRootSchema>()
                .ConfigureExecution(options =>
                {
                    options.EnableMetrics = Environment.IsDevelopment();
                    var logger = options.RequestServices!.GetRequiredService<ILogger<Startup>>();
                    options.UnhandledExceptionDelegate = ctx => logger.LogError("{Error} occurred", ctx.OriginalException.Message);
                })
                .AddSystemTextJson()
                .Configure<ErrorInfoProviderOptions>(opt => opt.ExposeExceptionStackTrace = Environment.IsDevelopment())
                .AddWebSockets()
                .AddGraphTypes(typeof(EngiRootSchema).Assembly);

            services.AddHttpClient();
            services.AddHttpClient(nameof(SubstrateClient), http =>
            {
                var options = Configuration
                    .GetRequiredSection("Substrate")
                    .Get<SubstrateClientOptions>();

                http.BaseAddress = options.HttpsUri;
            })
            .AddTransientHttpErrorPolicy(x => x.WaitAndRetryAsync(3, @try => TimeSpan.FromSeconds(@try)));
            services.AddTransient(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                return new SubstrateClient(httpClientFactory);
            });

            services.AddSingleton(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<SubstrateClientOptions>>();
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var logger = serviceProvider.GetRequiredService<ILogger<ChainNewHeadSubscriber>>();

                IObservable<Header> subscriber = new ChainNewHeadSubscriber(
                    options.Value.WssUri, httpClientFactory, logger);

                return subscriber;
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHttpsRedirection();
            }

            app.UseHealthChecks("/api/health");

            app.UseWebSockets();

            app.UseGraphQLWebSockets<EngiRootSchema>();
            app.UseGraphQL<EngiRootSchema, GraphQLHttpMiddleware<EngiRootSchema>>();

            app.UseGraphQLPlayground();
            app.UseGraphQLGraphiQL();
            app.UseGraphQLAltair();
            app.UseGraphQLVoyager();
        }
    }
}
