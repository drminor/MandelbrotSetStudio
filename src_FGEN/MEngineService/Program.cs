using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace MEngineService
{
	public class Program
	{
		private const string MONGO_DB_SERVER = "desktop-bau7fe6";
		private const int MONGO_DB_PORT = 27017;

		public static void Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();

			Console.WriteLine($"Service is started. Server: {MONGO_DB_SERVER}, Port: {MONGO_DB_PORT}.");
		}

		// Additional configuration is required to successfully run gRPC on macOS.
		// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			return Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>()
					.ConfigureLogging(
						loggingBuilder => loggingBuilder
						.ClearProviders());
						//.SetMinimumLevel(LogLevel.Warning));
				});
		}
	}
}


