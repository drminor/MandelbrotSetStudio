using MEngineService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc.Server;
using Serilog;
using Serilog.Events;

namespace MEngineService
{
	public class Startup
	{
		public Startup()
		{
			var x = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.MinimumLevel.Override("Microsoft", LogEventLevel.Error)
			.MinimumLevel.Override("System", LogEventLevel.Error)
			.Enrich.FromLogContext()
			.WriteTo.File(@"C:\_MandelbrotMEngineServiceLogs\log.txt", rollingInterval: RollingInterval.Day)
			.CreateLogger();
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			//services.AddGrpc();
			services.AddCodeFirstGrpc();
			services.AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Error)).BuildServiceProvider();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
						
			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapGrpcService<MapSectionService>();
			});
		}
	}
}
