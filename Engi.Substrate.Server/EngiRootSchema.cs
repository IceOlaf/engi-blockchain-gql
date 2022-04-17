using Engi.Substrate.Pallets;
using Engi.Substrate.Server.Types;
using GraphQL;
using GraphQL.Types;
using Sentry;

namespace Engi.Substrate.Server;

public class EngiRootSchema : Schema
{
    public EngiRootSchema(IServiceProvider serviceProvider)
    {
        RegisterTypeMapping(typeof(AccountData), typeof(AccountDataGraphType));

        Query = new SchemaQuery(serviceProvider);
    }

    public class SchemaQuery : ObjectGraphType
    {
        private readonly IServiceProvider serviceProvider;

        public SchemaQuery(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            
            FieldAsync<EngiHealthGraphType>("health", resolve: async _ => await GetHealthAsync());
            
            FieldAsync<AccountInfoGraphType>("account",
                arguments: new QueryArguments(
                    new QueryArgument<IdGraphType> { Name = "id" }
                ),
                resolve: async context => await GetAccountAsync(context));
        }

        private async Task<EngiHealth> GetHealthAsync()
        {
            using var scope = serviceProvider.CreateScope();

            var substrate = scope.ServiceProvider.GetRequiredService<SubstrateClient>();
            var sentry = scope.ServiceProvider.GetRequiredService<IHub>();

            try
            {
                var result = await Task.WhenAll(
                    substrate.GetSystemChainAsync(),
                    substrate.GetSystemNameAsync(),
                    substrate.GetSystemVersionAsync()
                );

                return new EngiHealth
                {
                    Chain = result[0],
                    NodeName = result[1],
                    Version = result[2],
                    Status = EngiHealthStatus.Online
                };
            }
            catch (Exception ex)
            {
                sentry.CaptureException(ex);

                return new EngiHealth
                {
                    Status = EngiHealthStatus.Offline
                };
            }
        }

        private async Task<AccountInfo> GetAccountAsync(IResolveFieldContext<object> context)
        {
            using var scope = serviceProvider.CreateScope();

            var substrate = scope.ServiceProvider.GetRequiredService<SubstrateClient>();

            string? accountId = context.GetArgument<string>("id");

            return await substrate.GetSystemAccountAsync(accountId);
        }
    }


}