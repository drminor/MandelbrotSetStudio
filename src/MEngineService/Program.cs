using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace MEngineService
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var iHost = CreateHostBuilder(args).Build();
			iHost.Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			var hostBuilder = Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					_ = webBuilder.UseStartup<Startup>()
					.ConfigureLogging(
						loggingBuilder => loggingBuilder
						.ClearProviders()
						);

					//webBuilder.ConfigureKestrel(options =>
					//{
					//	options.Listen(IPAddress.Any, 5001, listenOptions =>
					//	{
					//		listenOptions.Protocols = HttpProtocols.Http2;
					//		//listenOptions.UseHttps("C:\\My.pfx", "passw");
					//	});

					//});
				});

			//.ConfigureWebHost(webHostBuilder =>
			//{
			//	_ = webHostBuilder.ConfigureKestrel(options =>
			//	{
			//		options.Listen(IPAddress.Any, 5000, listenOptions =>
			//		{
			//			listenOptions.Protocols = HttpProtocols.Http2;
			//			//listenOptions.UseHttps("C:\\My.pfx", "passw");
			//		});
			//	});
			//});

			OutputBanner(hostBuilder);

			return hostBuilder;
		}

		private static void OutputBanner(IHostBuilder hostBuilder)
		{
			hostBuilder.ConfigureServices((hostContext, services) =>
			{
				var cmdLineUrl = hostContext.Configuration["urls"];
				var applicationUrl = hostContext.Configuration["ASPNETCORE_URLS"];

				var endpoint = cmdLineUrl ?? applicationUrl;

				Console.WriteLine($"MEngineService Started.\n\n" +
					$"Listening at address: {endpoint}.");
			});

		}
	}
}


