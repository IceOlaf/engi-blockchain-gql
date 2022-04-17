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
            services.AddHttpClient<SubstrateClient>(http =>
            {
                var options = new SubstrateClientOptions();
                Configuration.GetSection("Substrate").Bind(options);
                http.BaseAddress = new Uri(options.Url!);
            })
            .AddTransientHttpErrorPolicy(x => x.WaitAndRetryAsync(3, @try => TimeSpan.FromSeconds(@try)));
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
