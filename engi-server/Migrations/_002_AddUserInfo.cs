using Raven.Migrations;

namespace Engi.Substrate.Server.Migrations;

[Migration(2)]
public class _002_AddUserInfo : Migration
{
    public override void Up()
    {
        PatchCollection(@"
from Users
update {
    if(!this.EmailSettings) {
        this.EmailSettings = {
            WeeklyNewsletter: true,
            JobAlerts: true,
            TechnicalUpdates: true
        }
    }
}
");
    }
}
