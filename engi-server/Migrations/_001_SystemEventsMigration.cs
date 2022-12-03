using Raven.Migrations;

namespace Engi.Substrate.Server.Migrations;

[Migration(1)]
public class _001_SystemEventsMigration : Migration
{
    public override void Up()
    {
        PatchCollection(
@"
from ExpandedBlocks
where IndexedOn != null
update {
    if(!this.Events) {
        this.Events = []
    }
}
");
    }
}
