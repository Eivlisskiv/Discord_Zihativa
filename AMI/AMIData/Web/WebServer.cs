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
using System.Net;

namespace AMI.AMIData.Webhooks
{
    public class WebServer
    {
        static MongoDatabase Database => Program.data.database;
        static IConfigurationRoot CreateConfig()
            => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("hosting.json", optional: true).Build();
        public static async Task CreateKestrelHost(bool isDev)
        {
            var config = CreateConfig();

            var host = new WebHostBuilder().UseConfiguration(config)
                .UseContentRoot(Directory.GetCurrentDirectory()).UseStartup<WebServer>();

            if (Program.tokens.platform == Tokens.Platforms.Linux) {
                host.UseKestrel()
                .UseUrls($"http://*:{(isDev ? "5080" : "5000")}");
            }

            await host.Build().RunAsync();
        }

        public static async Task CreateHostW(bool isDev)
        {
            var config = CreateConfig();

            var server = Host.CreateDefaultBuilder().ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<WebServer>().UseContentRoot(Directory.GetCurrentDirectory());
                if (Program.tokens.platform == Tokens.Platforms.Linux)
                {
                    webBuilder.UseKestrel()
                    .UseUrls($"http://*:{(isDev ? "5080" : "5000")}");
                }
            });

            await server.Build().RunAsync();
        }

        public static string GetFileContent(string url)
        {
            try
            { return ReadRequest(Request(url)); } 
            catch (Exception e) {  Log.LogS(e); }
            return null;
        }

        public static HttpWebRequest Request(string url)
        {
            try
            { return (HttpWebRequest)WebRequest.Create(url); }
            catch (Exception e)
            { Log.LogS(e); }
            return null;
        }

        public static string ReadRequest(HttpWebRequest request)
        {
            try
            {
                using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                using StreamReader stIn = new StreamReader(response.GetResponseStream());
                return stIn.ReadToEnd();
            }
            catch (Exception e)
            {
                Log.LogS(e);
            }
            return null;
        }

        IConfiguration _config;

        public WebServer(IConfiguration config)
        {
            _config = config;
        }
        
        public void ConfigureServices(IServiceCollection service)
        {
            service.AddRouting();
            service.AddControllers();

            service.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.WithOrigins("*")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                    });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            app.UseCors();

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
                endpoints.MapPost("/empty", async (ctx) => {
                    
                });

                endpoints.MapControllers();
            });
        }

        private async Task<bool> ValidateToken(HttpContext context)
        {
            string auth = context.Request.Headers["Authorization"];
            var result = await Database.LoadRecordAsync<object>("Sessions", $"{{ token: {auth} }}");

            if(result == null)
            {
                //context.Response.
                return false;
            }

            return true;
        }

        private async Task RegisterVote(HttpContext context)
        {
            if (!context.Request.Headers["Authorization"].Equals(Program.tokens.dblAuth)) return;

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
