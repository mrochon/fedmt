#r "Newtonsoft.Json"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

private static DateTime _expires_at;
private static string _access_token = String.Empty;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    log.LogInformation("IsMemberOfGroup starting.");

    string userObjectId = req.Query["userObjectId"];
    string groupName = req.Query["groupName"];

    log.LogInformation($"userObjectId:{userObjectId}");
    log.LogInformation($"groupName:{groupName}");

    if (String.IsNullOrEmpty(userObjectId) || String.IsNullOrEmpty(groupName))
        return new BadRequestObjectResult(new { version = "1.0.0", status = 409, userMessage = "Invalid arguments" });

    log.LogInformation($"IsMemberOfGroup for userObjectId={userObjectId}&group={groupName}");
    var tenant_id = Environment.GetEnvironmentVariable("B2C:tenant_id");
    var client_id = Environment.GetEnvironmentVariable("B2C:client_id");
    var client_secret = Environment.GetEnvironmentVariable("B2C:client_secret");

    using (var http = new HttpClient())
    {
        try
        {
            //TODO: cache token and its expiry time, reuse
            log.LogInformation($"Graph client: {client_id}");
            var limit_time = DateTime.UtcNow.AddMinutes(-5);
            log.LogInformation($"Current: {limit_time}; Expiry: {_expires_at}");
            if(limit_time.CompareTo(_expires_at) >= 0)
            {
                log.LogInformation($"Getting new token");
                var resp = await http.PostAsync($"https://login.microsoftonline.com/{tenant_id}/oauth2/v2.0/token",
                    new FormUrlEncodedContent(new List<KeyValuePair<String, String>>
                    {
                        new KeyValuePair<string,string>("client_id", client_id),
                        new KeyValuePair<string,string>("scope", "https://graph.microsoft.com/.default"),
                        new KeyValuePair<string,string>("client_secret", client_secret),
                        new KeyValuePair<string,string>("grant_type", "client_credentials")
                    }));
                if (!resp.IsSuccessStatusCode)
                    return new BadRequestObjectResult(new { version = "1.0.0", status = 409, userMessage = "Authorization failure" });
                dynamic tokens = JsonConvert.DeserializeObject(await resp.Content.ReadAsStringAsync());
                log.LogInformation("Token Json received");
                _access_token = (string)(tokens.access_token);
                _expires_at = DateTime.UtcNow.AddSeconds((int) (tokens.expires_in));
                log.LogInformation($"New token obtained. Expires at: {_expires_at}");
            }
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _access_token);            
            // Cache this!!  https://docs.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-5.0#:~:text=Cache%20in-memory%20in%20ASP.NET%20Core%201%20Caching%20basics.,8%20Background%20cache%20update.%20...%209%20Additional%20resources
            log.LogInformation("Token obtained");

            //TODO: implement throttling support
            var json = await http.GetStringAsync($"https://graph.microsoft.com/v1.0/users/{userObjectId}/memberOf");
            foreach(var group in JObject.Parse(json)["value"])
            {
                if(String.Compare(group["displayName"].Value<string>(), groupName, ignoreCase:true) == 0)
                    return new OkResult();
            }
            return new BadRequestObjectResult(new { version = "1.0.0", status = 409, userMessage = "You cannot sign in to this organization." });
        }
        catch (Exception ex)
        {
            log.LogError($"user/roles exception: {ex.Message}");
            return new BadRequestObjectResult(new { version = "1.0.0", status = 409, userMessage = ex.Message });
        }
    }
}
