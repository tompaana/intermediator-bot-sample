using IntermediatorBotSample.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StructureMap;
using System;

namespace IntermediatorBotSample
{
    public class Startup
    {
        public static HandoffHelper HandoffHelper
        {
            get;
            private set;
        }


        public static BotSettings BotSettings
        {
            get;
            private set;
        }


        public IConfiguration Configuration
        {
            get;
        }


        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            BotSettings   = new BotSettings(Configuration);
            HandoffHelper = new HandoffHelper(BotSettings);
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .AddControllersAsServices();
            services.AddSingleton(_ => Configuration);
            services.AddBot<IntermediatorBot>(options =>
            {
                options.CredentialProvider = new ConfigurationCredentialProvider(Configuration);
            });
            return ConfigureIoC(services);
        }


        public IServiceProvider ConfigureIoC(IServiceCollection services)
        {
            var container = new Container(new RuntimeRegistry(services));
            container.Configure(c => c.For<BotSettings>().Use(BotSettings));
            return container.GetInstance<IServiceProvider>();
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseBotFramework();
            app.UseMvc();
        }
    }
}
