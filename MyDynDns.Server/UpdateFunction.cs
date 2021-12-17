using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace MyDynDns.Server
{

    public static class UpdateFunction
    {
        [FunctionName(nameof(Update))]
        public static async Task<IActionResult> Update(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "update/{hostname}")] HttpRequest req,
            IBinder binder,
            string hostname,
            ILogger log)
        {
            log.LogInformation($"Update request for {hostname}");

            string currentClientIp = GetIpFromRequestHeaders(req);
            log.LogInformation($"Client IP: {currentClientIp.Substring(0,6)}");

            string existingIp = null;
            string zoneIdentifier = null;
            string recordIdentifier = null;
            
            zoneIdentifier = req.Query[nameof(zoneIdentifier)];
            recordIdentifier = req.Query[nameof(recordIdentifier)];

            using (var reader = await binder.BindAsync<TextReader>(new BlobAttribute(
                $"registrations/{hostname.Replace('.','-')}.txt", FileAccess.Read)))
            {
                existingIp = await reader.ReadLineAsync();
       
                if (existingIp == currentClientIp)
                {
                    log.LogInformation("Same IP, skipping update");
                    return new OkObjectResult("ok");
                }

                // we need to update
                log.LogInformation("Updating IP in Cloudflare...");

                if (string.IsNullOrWhiteSpace(zoneIdentifier) || string.IsNullOrWhiteSpace(recordIdentifier))
                {
                    zoneIdentifier = await reader.ReadLineAsync();
                    recordIdentifier = await reader.ReadLineAsync();
                }
            };

            log.LogInformation($"ZoneId: {zoneIdentifier}, Record: {recordIdentifier}");
            log.LogInformation($"Updating {hostname} from {currentClientIp}");

            var cloudFlare = new CloudflareClient(Environment.GetEnvironmentVariable("CLOUDFLARE_APITOKEN"));
            await cloudFlare.UpdateDnsZone(hostname, zoneIdentifier, recordIdentifier, currentClientIp);
            
            log.LogInformation("IP Updated");

            using (var writer = await binder.BindAsync<TextWriter>(new BlobAttribute(
                $"registrations/{hostname.Replace('.', '-')}.txt", FileAccess.Write)))
            {
                string blobRegistrationContent = currentClientIp + Environment.NewLine + zoneIdentifier + Environment.NewLine + recordIdentifier;
                await writer.WriteAsync(blobRegistrationContent);
            };
            return new OkObjectResult("ok");
        }

        private static string GetIpFromRequestHeaders(HttpRequest request)
        {
            string clientIp = (request.Headers["X-Forwarded-For"].FirstOrDefault() ?? "").Split(new char[] { ':' }).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(clientIp))
            {
                clientIp = request.HttpContext.Connection.RemoteIpAddress.ToString();
            }

            return clientIp;
        }
    }
}
