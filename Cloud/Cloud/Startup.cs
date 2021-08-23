using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cloud.Extensions;
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
                ".tar.gz",
                "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/master/assets/Folder%20Zip/SVG/ic_fluent_folder_zip_28_regular.svg"
            },
            {
                ".tar",
                "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/master/assets/Folder%20Zip/SVG/ic_fluent_folder_zip_28_regular.svg"
            },
            {
                ".7zip",
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
                ".jpeg",
                "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/master/assets/Image/SVG/ic_fluent_image_28_regular.svg"
            },
            {
                ".bmp",
                "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/master/assets/Image/SVG/ic_fluent_image_28_regular.svg"
            },
            {
                ".raw",
                "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/master/assets/Image/SVG/ic_fluent_image_28_regular.svg"
            },
            {
                ".gif",
                "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/master/assets/Image/SVG/ic_fluent_image_28_regular.svg"
            },
            {
                ".exe",
                "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/master/assets/Apps/SVG/ic_fluent_apps_28_regular.svg"
            },
            {
                ".pdf",
                "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/master/assets/Document%20PDF/SVG/ic_fluent_document_pdf_32_regular.svg"
            },
            {
                ".csv",
                "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/master/assets/Table/SVG/ic_fluent_table_32_regular.svg"
            },
            {
                ".xlsx",
                "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/master/assets/Table/SVG/ic_fluent_table_32_regular.svg"
            },
            {
                ".txt",
                "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/master/assets/Document%20Bullet%20List/SVG/ic_fluent_document_bullet_list_24_regular.svg"
            }
        };

        public static string[] TextPreviewFileExtensions = { ".txt" };
        public static string[] ImagePreviewFileExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp" };

        public static string[] CodePreviewFileExtensions =
        {
            ".sh", ".css", ".less", ".c", ".h", ".vb", ".java", ".lua", ".php", ".py", ".yaml", ".go", ".scss", ".sql",
            ".ts"
        };

        public static Dictionary<int, string> PreviewCache = new();

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public static Settings Settings { get; } =
            JsonSerializer.Deserialize<Settings>(File.ReadAllText("settings.json"));

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue;
                x.MultipartHeadersLengthLimit = int.MaxValue;
            });
            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddDbContext<ApplicationDbContext>();
            services.AddSession(options => { options.IdleTimeout = TimeSpan.FromMinutes(5); });
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
            {
                options.LoginPath = new PathString("/Login");
            });
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
            app.UseSession();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
            app.UseTus(httpContext => new DefaultTusConfiguration
            {
                Store = new TusDiskStore(Settings.TusPath, true),
                UrlPath = "/upload",
                Events = new Events
                {
                    OnFileCompleteAsync = async eventContext =>
                    {
                        var file = await eventContext.GetFileAsync();
                        await FileUploadCompleted(file, env, eventContext.CancellationToken, eventContext.HttpContext);
                        var terminationStore = (ITusTerminationStore)eventContext.Store;
                        await terminationStore.DeleteFileAsync(file.Id, eventContext.CancellationToken);
                    }
                }
            });
            CheckIfNewInstallation();
        }

        private void CheckIfNewInstallation()
        {
            using var db = new ApplicationDbContext();
#if (!DEBUG)
                   db.Database.Migrate();
#endif


            if (!db.Users.Any())
            {
                var pw = UserManager.GenerateRandomPassword();
                var hashed = UserManager.GenerateHashAndSalt(pw);
                db.Users.Add(new User
                {
                    IsAdmin = true,
                    MaxFileBytes = FileMethods.GigabyteToByte(10),
                    Name = "admin",
                    Password = hashed[0],
                    Salt = hashed[1],
                    LastLogin = DateTime.MinValue,
                    FilePassword = UserManager.GenerateRandomCryptoString(16).Encrypt(pw.Sha512())
                });
                Directory.CreateDirectory("Data");
                Directory.CreateDirectory("Data/1");
                Directory.CreateDirectory(Settings.TusPath);
                db.SaveChanges();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[INFO] -------- Generated Admin-account ------- ");
                Console.WriteLine("[INFO] Username: admin");
                Console.WriteLine("[INFO] Password: " + pw);
                Console.WriteLine("[INFO] -------- Generated Admin-account ------- ");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        private async Task FileUploadCompleted(ITusFile file, IWebHostEnvironment env, CancellationToken cs,
            HttpContext ctx)
        {
            var meta = await file.GetMetadataAsync(cs);
            await using var db = new ApplicationDbContext();
            var clientComponent = ctx.Request.Cookies["ClientFileKeyComponent"];
            var serverComponent = ctx.Session.GetString("ServerFileKeyComponent");

            var user = await UserManager.GetUserById(Convert.ToInt32(meta["uid"].GetString(Encoding.UTF8)));
            await using var stream = await file.GetContentAsync(cs);
            var filepath = env.ContentRootPath + "/Data/" + user.Id + "/" + meta["path"].GetString(Encoding.UTF8);
            var size = db.Files.Where(o => o.UserId == user.Id).ToList().Sum(o => o.Size);
            if (stream.Length + size < user.MaxFileBytes)
            {
                var f = new DbFile
                {
                    Name = meta["filename"].GetString(Encoding.UTF8),
                    Path = filepath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar),
                    Filename = meta["filename"].GetString(Encoding.UTF8),
                    LastModified = DateTime.Now,
                    Size = stream.Length,
                    UserId = user.Id
                };

                db.Files.Add(f);
                await db.SaveChangesAsync(cs);
                var k1 = clientComponent.Decrypt(serverComponent);
                var key = user.FilePassword.Decrypt(k1);
                var bytesToEnc = stream.ReadToEnd();
                var bytes = Crypto.EncryptByteArray(Encoding.UTF8.GetBytes(key), bytesToEnc);
                key = null;
                k1 = null;
                await stream.DisposeAsync();
                GC.Collect();
                await File.WriteAllBytesAsync(filepath + "/" + meta["filename"].GetString(Encoding.UTF8), bytes, cs);
            }

            await stream.DisposeAsync();
        }
    }
}