using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cloud.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => {
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
                // c:\tusfiles is where to store files
                Store = new TusDiskStore(@"F:\Repos\cloud\Cloud\Cloud\tmp"),
                // On what url should we listen for uploads?
                UrlPath = "/upload",
                Events = new Events
                {
                    OnFileCompleteAsync = async eventContext =>
                    {
                        ITusFile file = await eventContext.GetFileAsync();
                        await FileUploadCompleted(file,env);
                    }
                }
            });
            //UserManager.SetAdmin(1, true);
            /*var x = FileMethods.DirSearch("Data");
            using var db = new ApplicationDbContext();
            int id = 0;
            foreach (var fileInfo in x)
            {
                db.Files.Add(new()
                {
                    FileHash = fileInfo.Name.GetHashCode().ToString(),
                    FileIdentifier = $"{id}_{fileInfo.Name}_{fileInfo.Name.GetHashCode()}",
                    Modified = fileInfo.LastWriteTime,
                    Path = "/1/",
                    Size = "0"
                });
                id++;
            }

            db.SaveChanges();   */
        }

        private async Task FileUploadCompleted(ITusFile file,IWebHostEnvironment _env)
        {
            var meta =await file.GetMetadataAsync(new CancellationToken());
            var user = await UserManager.GetUserById(Convert.ToInt32(meta["uid"].GetString(Encoding.UTF8)));

        
            var stream = await file.GetContentAsync(new CancellationToken());
            string filepath = _env.ContentRootPath + "/Data/" + user.Id;
            Debug.WriteLine(filepath + "/" + meta["filename"].GetString(Encoding.UTF8));
            using (var fileStream = new FileStream(filepath + "/" + meta["filename"].GetString(Encoding.UTF8), FileMode.Create))
            {
               await stream.CopyToAsync(fileStream);
            }
            
        }
    }
}
