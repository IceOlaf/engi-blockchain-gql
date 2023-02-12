using Raven.Client.Documents.Operations.Indexes;
using Raven.Migrations;

namespace Engi.Substrate.Server.Migrations;

[Migration(3)]
public class _003_RemoveEventIndex : Migration
{
    public override void Up()
    {
        DocumentStore.Maintenance.Send(new DeleteIndexOperation("EventIndex"));
    }
}
