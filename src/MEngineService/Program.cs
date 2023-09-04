using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MEngineService
{
	public class Program
	{
		public static void Main(string[] args)
		{
			//CreateHostBuilder(args).Build().Run();

			var iHost =	CreateHostBuilder(args).Build();

			iHost.Run();
		}

		// Additional configuration is required to successfully run gRPC on macOS.
		// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			return Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					_ = webBuilder.UseStartup<Startup>()
					.ConfigureLogging(
						loggingBuilder => loggingBuilder
						.ClearProviders()
						);
				});
		}
	}
}

