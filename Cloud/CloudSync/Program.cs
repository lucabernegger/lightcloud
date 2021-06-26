using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CloudSync
{
    class Program
    {
        static string BearerToken = String.Empty;
        static async Task Main(string[] args)
        {
            var x =await Login("demo", "123");
            BearerToken = x.AccessToken;
            var files = await GetFiles(@"F:\Repos\cloud\Cloud\Cloud\Data\3\");
            Console.WriteLine(JsonSerializer.Serialize(files));
            Console.ReadKey();
        }

        public static async Task<LoginResult> Login(string username, string password)
        {
            using var client = new HttpClient();
            var json = await client.GetStringAsync(
                $"https://localhost:44395/get-token?username={username}&password={password}");
            return JsonConvert.DeserializeObject<LoginResult>(json);
        }  
        public static async Task<List<DbFile>> GetFiles(string path)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", BearerToken);
            var json = await client.GetStringAsync(
                $"https://localhost:44395/Files/GetFiles?path={path}");
            return JsonConvert.DeserializeObject<List<DbFile>>(json);
        }
    }
    public class DbFile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Filename { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
    }
    public class LoginRequest
    {
        [Required]
        [JsonPropertyName("username")]
        public string UserName { get; set; }

        [Required]
        [JsonPropertyName("password")]
        public string Password { get; set; }
    }

    public class LoginResult
    {
        [JsonPropertyName("username")]
        public string UserName { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("originalUserName")]
        public string OriginalUserName { get; set; }

        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; }

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; }
    }

    public class RefreshTokenRequest
    {
        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; }
    }

    public class ImpersonationRequest
    {
        [JsonPropertyName("username")]
        public string UserName { get; set; }
    }
}
