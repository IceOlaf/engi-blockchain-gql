using Engi.Substrate.Server.Indexing;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Server;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.SystemReactive;
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
                    var logger = options.RequestServices!.GetRequiredService<ILogger<Startup>>();

                    options.EnableMetrics = Environment.IsDevelopment();
                    options.UnhandledExceptionDelegate =
                        ctx => logger.LogError(ctx.OriginalException, "Error occurred: {error}", ctx.OriginalException.Message);
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

                http.BaseAddress = new Uri(options.HttpUrl);
            })
            .AddTransientHttpErrorPolicy(x => x.WaitAndRetryAsync(3, @try => TimeSpan.FromSeconds(@try)));
            services.AddTransient(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                return new SubstrateClient(httpClientFactory);
            });

            services.AddRaven(Configuration.GetRequiredSection("Raven"));

            services.AddSingleton<IChainObserver, NewHeadChainObserver>();
            services.AddSingleton<IChainObserver, ChainSnapshotObserver>();
            services.AddHostedService<ChainObserverBackgroundService>();

            services.AddHostedService<IndexingBackgroundService>();
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

            app.UseDefaultFiles();
            app.UseStaticFiles();
        }
    }
}