using Raven.Client.Documents.Operations.Indexes;
using Raven.Migrations;

namespace Engi.Substrate.Server.Migrations;

[Migration(4)]
public class _004_LanguageTechnologies : Migration
{
    public override void Up()
    {
        this.PatchCollection(@"
            from Jobs as j
            update {
                j.Technologies = [j.Language];
                delete j.Language;
            }
        ");
    }
}
