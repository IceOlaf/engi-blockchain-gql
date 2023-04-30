using Raven.Migrations;

namespace Engi.Substrate.Server.Migrations;

[Migration(5)]
public class _005_JobSnapshots : Migration
{
    public override void Up()
    {
        this.PatchCollection(@"
            from JobSnapshots as j
            update {
                if (j.Language !== undefined) {
                    j.Technologies = [j.Language];
                    delete j.Language;
                }
            }
        ");
    }
}
