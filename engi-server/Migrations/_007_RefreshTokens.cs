using Raven.Migrations;

namespace Engi.Substrate.Server.Migrations;

[Migration(7)]
public class _007_RefreshTokens : Migration
{
    public override void Up()
    {
        PatchCollection(@"
from Users update {
    this.Tokens = this.Tokens.filter((item) => item['Type'] !== 'RefreshToken')
}
");
    }
}
