using AMI.Methods;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using AMI.Neitsillia.User;
using AMYPrototype;
using AMI.Handlers;

namespace AMI.AMIData.Webhooks
{
    public class WebServer
    {
        public static IWebHost CreateHost(bool isDev)
        {
            var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("hosting.json", optional: true)
            .Build();

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:5000")
                .UseConfiguration(config)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<WebServer>()
                .Build();

            host.RunAsync();

            return host;
        }
        
        IConfiguration _config;

        public WebServer(IConfiguration config)
        {
            _config = config;
        }
        
        public void ConfigureServices(IServiceCollection service)
        {
            service.AddRouting();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            //To use static files in folder wwwroot
            //app.UseStaticFiles();

            //app.UseDefaultFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context => 
                {
                    await context.Response.WriteAsync("Hello from web");
                });

                endpoints.MapPost("/registerVote", RegisterVote);
            });
        }

        private async Task RegisterVote(HttpContext context)
        {
            BotUser user = null;
            try
            {
                string bodyContent = await new StreamReader(context.Request.Body).ReadToEndAsync();
                if (bodyContent != null && bodyContent.Length > 0)
                {
                    Log.LogS(bodyContent);
                    var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(bodyContent);
                    user = BotUser.Load(ulong.Parse(values["user"]));
                    await UniqueChannels.Instance.SendToLog($"{user._id}'s vote was received");
                }
            }
            catch (Exception e) 
            {
                await UniqueChannels.Instance.SendToLog(e);
            }

            if (user != null) await user.NewVote();
            else Log.LogS("Failed to register vote");
        }
    }
}
