using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using LetsChess_Backend.Logic;
using LetsChess_Backend.WSHub;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Threading.Tasks;
using System;
using NLog.Web;
using NLog;

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

			IConfigurationSection googleAuthNSection = Configuration.GetSection("Authentication:Google");
			var clientId = googleAuthNSection["ClientId"];
			var clientSecret = googleAuthNSection["ClientSecret"];
			services.AddAuthorization().AddAuthentication(a => {
				a.RequireAuthenticatedSignIn = true;
				a.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
			}).AddJwtBearer(o => {
				var logger = NLogBuilder.ConfigureNLog(LogManager.Configuration).GetCurrentClassLogger();
				o.SecurityTokenValidators.Add(new GoogleTokenValidator());

				o.Events = new JwtBearerEvents
				{
					OnAuthenticationFailed = a =>
					{
						logger.Info(a.Exception, $"Authentication failed with error {a.Exception?.Message}");
						return Task.CompletedTask;
					},
					OnTokenValidated = e =>
					{
						return Task.CompletedTask;
					},
					OnMessageReceived = e => {
						logger.Trace($"the message was received by the authentication");
						return Task.CompletedTask;
					},
					OnChallenge = e =>
					{
						logger.Debug($"the message was challenged by the authentication");
						return Task.CompletedTask;
					},
					OnForbidden = e => {
						logger.Info($"request was forbidden");
						return Task.CompletedTask;
					}		
				};
			});

			var ocbuilder = services.AddOcelot(Configuration);
			services.AddOcelotPlaceholderSupport(Configuration);
			services.AddControllers();

			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "LetsChess_Backend", Version = "v1" });
			});
			services.Configure<ConnectionStrings>(Configuration.GetSection("ConnectionStrings"));
			services.Configure<Credentials>(Configuration.GetSection("MQCredentials"));
			services.Configure<AuthSettings>(Configuration.GetSection("Authentication:Google"));
			services.Configure<ServiceEndpoints>(Configuration.GetSection("ServiceEndpoints"));

			services.AddSignalR(c => c.EnableDetailedErrors = true);
			services.AddSingleton<MQConnector>();
			services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
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

			//initialize instance
			app.ApplicationServices.GetService<MQConnector>();
		}
	}
}
