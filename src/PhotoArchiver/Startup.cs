using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.AspNetCore.Http.Features;
using PhotoArchiver.Services;

namespace PhotoArchiver
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
            services.AddMvc();

            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = long.MaxValue;
            });

            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            services.Configure<AppSettings>(ValidateAppSettings);

            services.AddSingleton(new DateTakenExtractorService());
        }

        private void ValidateAppSettings(AppSettings appSettings)
        {
            if (appSettings == null)
                throw new Exception("Invalid configuration file (appSettings is null)");
            if (string.IsNullOrWhiteSpace(appSettings.TargetAbsolutePath))
                throw new Exception("Invalid configuration file (target path unavailable)");
            if (string.IsNullOrWhiteSpace(appSettings.TempAbsolutePath))
                throw new Exception("Invalid configuration file (temp path unavailable)");

            appSettings.TargetAbsolutePath = Utility.ResolvePath(appSettings.TargetAbsolutePath);
            appSettings.TempAbsolutePath = Utility.ResolvePath(appSettings.TempAbsolutePath);

            if (Path.IsPathRooted(appSettings.TargetAbsolutePath) == false)
                throw new Exception($"{nameof(appSettings.TargetAbsolutePath)} must be an absolute path");

            if (Path.IsPathRooted(appSettings.TempAbsolutePath) == false)
                throw new Exception($"{nameof(appSettings.TempAbsolutePath)} must be an absolute path");

            if (Directory.Exists(appSettings.TargetAbsolutePath) == false)
                Directory.CreateDirectory(appSettings.TargetAbsolutePath);

            if (Directory.Exists(appSettings.TempAbsolutePath) == false)
                Directory.CreateDirectory(appSettings.TempAbsolutePath);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            loggerFactory.CreateLogger<Controllers.UploadController>();

            app.UseMvc();

            var options = new DefaultFilesOptions();
            options.DefaultFileNames.Clear();
            options.DefaultFileNames.Add("index.html");
            app.UseDefaultFiles(options);

            app.UseStaticFiles();
        }
    }
}
