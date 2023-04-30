using Raven.Migrations;

namespace Engi.Substrate.Server.Migrations;

[Migration(6)]
public class _006_MoveExpandedBlockNamespace : Migration
{
    public override void Up()
    {
        PatchCollection(@"
from ExpandedBlocks update {
    this['@metadata']['Raven-Clr-Type'] = 'Engi.Substrate.Indexing.ExpandedBlock, Engi.Substrate'
}
");
    }
}
