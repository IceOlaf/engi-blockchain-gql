using GraphQL.Types;
using Sentry;

namespace Engi.Substrate.Server.Schemas;

public class EngiHealthQuery : ObjectGraphType
{
    public EngiHealthQuery(IServiceProvider serviceProvider)
    {
        FieldAsync<EngiHealthType>("health", resolve: async _ =>
        {
            using var scope = serviceProvider.CreateScope();

            var substrate = scope.ServiceProvider.GetRequiredService<SubstrateClient>();
            var sentry = scope.ServiceProvider.GetRequiredService<IHub>();

            try
            {
                var result = await Task.WhenAll(
                    substrate.RpcAsync<string>("system_chain"),
                    substrate.RpcAsync<string>("system_name"),
                    substrate.RpcAsync<string>("system_version")
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
        });
    }
}