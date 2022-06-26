namespace Engi.Substrate.Server;

public class Program
{
    public static void Main(string[] args)
    {
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