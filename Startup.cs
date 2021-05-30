using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using LetsChess_Backend.Logic;
using LetsChess_Backend.WSClients;
using LetsChess_Backend.WSHub;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace LetsChess_Backend
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }
		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddCors(options =>
			{
				options.AddDefaultPolicy(options =>
				{
					var allowedHosts = Configuration.GetSection("AllowedHosts").Get<string[]>();
					options.AllowCredentials()
					.WithOrigins(allowedHosts)
					.AllowAnyMethod().AllowAnyHeader();
				});
			});
			var ocbuilder = services.AddOcelot(Configuration);
			services.AddOcelotPlaceholderSupport(Configuration);

			services.AddControllers();

			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "LetsChess_Backend", Version = "v1" });
			});
			services.Configure<Credentials>(Configuration.GetSection("MQCredentials"));
			services.Configure<AuthSettings>(Configuration.GetSection("Authentication:Google"));
			services.Configure<ServiceEndpoints>(Configuration.GetSection("ServiceEndpoints"));
			services.AddAuthorization().AddAuthentication().AddGoogleOpenIdConnect(o =>
			{
				IConfigurationSection googleAuthNSection = Configuration.GetSection("Authentication:Google");
				o.ClientId = googleAuthNSection["ClientId"];
				o.ClientSecret = googleAuthNSection["ClientSecret"];
			});
			services.AddSignalR(c => c.EnableDetailedErrors = true);
			services.AddSingleton<MatchmakingClient>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public async void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseSwagger();
				app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LetsChess_Backend v1"));
			}
			app.UseCors();

			//app.UseHttpsRedirection();
			
			app.UseRouting();

			app.UseAuthentication();
			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
				endpoints.MapHub<MatchHub>("/hub");
			});
			await app.UseOcelot();

			//initialize it
			app.ApplicationServices.GetService<MatchmakingClient>();
		}
	}
}
