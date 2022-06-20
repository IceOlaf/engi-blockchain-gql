namespace Engi.Substrate.Server;

public class Program
{
    public static void Main(string[] args)
    {
        System.Diagnostics.Debugger.Launch();
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                    .UseSentry()
                    .UseStartup<Startup>();
            }).Build();
        
        host.Run();
    }
}