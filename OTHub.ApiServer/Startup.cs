using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;
using OTHub.APIServer.Helpers;
using OTHub.APIServer.Messaging;
using OTHub.APIServer.SignalR;
using OTHub.Settings;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.Swagger;

namespace OTHub.APIServer
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

            var settings = new OTHubSettings();
            Configuration.Bind("OTHub", settings);

            settings.Validate();

            services.AddMemoryCache();
            
            services.AddSwaggerExamples();

            List<string> origins = new List<string>();

#if DEBUG
            origins.Add("http://localhost:4200");
#endif

            origins.Add(OTHubSettings.Instance.WebServer.AccessControlAllowOrigin);



            services.AddCors(options =>
            {
                //options.AddPolicy("AllowAll",
                //    builder =>
                //    {
                //        builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
                //            .SetPreflightMaxAge(TimeSpan.FromDays(7));
                //    });

                options.AddPolicy("SignalR",
                    builder => builder.WithOrigins(origins.ToArray())
                        .AllowAnyHeader().SetIsOriginAllowed((origin => true))
                        .AllowAnyMethod()
                        .AllowCredentials().SetPreflightMaxAge(TimeSpan.FromDays(7)));
            });

            services.AddControllers()
                .AddNewtonsoftJson(
                opt =>
                {
                    opt.SerializerSettings.ContractResolver = new DefaultContractResolver { NamingStrategy = new DefaultNamingStrategy() };
                }
                );


            services.AddSignalR(o =>
            {
                o.EnableDetailedErrors = true;
            }).AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });


            services.AddSwaggerGen(c =>
            {
                c.EnableAnnotations();
                c.ExampleFilters();
                c.SwaggerDoc("Mainnet", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "OT Hub", Version = "1.0.0" });
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.Authority = "https://othub.eu.auth0.com/";
                options.Audience = "https://othubapi";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = ClaimTypes.NameIdentifier
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        // If the request is for our hub...
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/signalr/notifications")))
                        {
                            // Read the token out of the query string
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });



 

            services.AddSingleton<RabbitMQService>();

            services.AddSingleton<TelegramBot>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
     

            //app.UseMiddleware<CustomCorsMiddleware>();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseSwagger(c =>
            {
                c.RouteTemplate = "docs/{documentName}/swagger.json";
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

    

            app.UseSwaggerUI(c =>
            {
                c.DocumentTitle = "OT Hub - API Documentation";
                c.RoutePrefix = "docs";
                c.SwaggerEndpoint($"/docs/Mainnet/swagger.json", $"OT Hub Mainnet");
            });

            app.UseCors("SignalR");

            app.UseAuthentication();

            app.UseRouting();
            app.UseAuthorization();

            //var webSocketOptions = new WebSocketOptions()
            //{
            //    KeepAliveInterval = TimeSpan.FromSeconds(120),
            //    AllowedOrigins = { "*" }
            //};

            //app.UseWebSockets();
            app.UseWebSockets();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<NotificationsHub>("/signalr/notifications");
            });

            //Loads up the singleton
            app.ApplicationServices.GetService<RabbitMQService>();
            app.ApplicationServices.GetService<TelegramBot>();
        }
    }
}