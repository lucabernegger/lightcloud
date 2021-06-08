using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cloud.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using tusdotnet;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;
using tusdotnet.Stores;

namespace Cloud
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public static Settings Settings { get; } =
            JsonSerializer.Deserialize<Settings>(File.ReadAllText("settings.json"));

        public static Dictionary<string, string> FileExtensionIcon = new()
        {
            {
                ".zip",
                "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/master/assets/Folder%20Zip/SVG/ic_fluent_folder_zip_28_regular.svg"
            },
            {
                ".rar",
                "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/master/assets/Folder%20Zip/SVG/ic_fluent_folder_zip_28_regular.svg"
            },
            {
                ".png",
                "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/master/assets/Image/SVG/ic_fluent_image_28_regular.svg"
            },
            {
                ".jpg",
                "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/master/assets/Image/SVG/ic_fluent_image_28_regular.svg"
            },
            {
                ".exe",
                "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/master/assets/Apps/SVG/ic_fluent_apps_28_regular.svg"
            },

        };
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue;
                x.MultipartHeadersLengthLimit = int.MaxValue;
            });
            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
            {
                options.LoginPath = new PathString("/Login");
            });
            services.AddDbContext<ApplicationDbContext>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();
            app.UseAuthentication();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
            app.UseTus(httpContext => new DefaultTusConfiguration
            {
                Store = new TusDiskStore(@"F:\Repos\cloud\Cloud\Cloud\tmp", true),
                UrlPath = "/upload",
                Events = new Events
                {
                    OnFileCompleteAsync = async eventContext =>
                    {
                        var file = await eventContext.GetFileAsync();

                        await FileUploadCompleted(file, env, eventContext.CancellationToken);
                        var terminationStore = (ITusTerminationStore) eventContext.Store;
                        await terminationStore.DeleteFileAsync(file.Id, eventContext.CancellationToken);
                    }
                }
            });
        }

        private async Task FileUploadCompleted(ITusFile file, IWebHostEnvironment env, CancellationToken cs)
        {
            var meta = await file.GetMetadataAsync(cs);
            var user = await UserManager.GetUserById(Convert.ToInt32(meta["uid"].GetString(Encoding.UTF8)));
            await using var stream = await file.GetContentAsync(cs);
            var filepath = env.ContentRootPath + "/Data/" + user.Id + "/" + meta["path"].GetString(Encoding.UTF8);
            var size = new DirectoryInfo(filepath).GetSizeOfDirectory();
            if (stream.Length + size < user.MaxFileBytes)
            {
                var f = new DbFile()
                {
                    Name = meta["filename"].GetString(Encoding.UTF8),
                    Path = filepath,
                    Filename = meta["filename"].GetString(Encoding.UTF8),
                    LastModified = DateTime.Now,
                    Size = stream.Length,
                    UserId = user.Id
                };
                await using var db = new ApplicationDbContext();
                db.Files.Add(f);
                await db.SaveChangesAsync();
                await using var fileStream = new FileStream(filepath + "/" + meta["filename"].GetString(Encoding.UTF8),
                    FileMode.Create);
                await stream.CopyToAsync(fileStream, cs);
                await fileStream.DisposeAsync();
            }

            await stream.DisposeAsync();
        }
    }
}