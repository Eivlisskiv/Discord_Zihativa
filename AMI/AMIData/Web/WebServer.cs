using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AMI.AMIData.Webhooks
{
    public class WebServer
    {
        public static void CreateHostBuilder(string[] args)
        {
            var server = Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<WebServer>();
            });

            server.Build().RunAsync();
        } 
        
        public void ConfigureServices(IServiceCollection service)
        {

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

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
            
            string bodyContent = new StreamReader(context.Request.Body).ReadToEnd();
        }
    }
}
