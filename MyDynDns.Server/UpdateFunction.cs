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

            using (var reader = await binder.BindAsync<TextReader>(new BlobAttribute(
                $"registrations/{hostname.Replace('.','-')}.txt", FileAccess.Read)))
            {
                existingIp = await reader.ReadLineAsync();
            };

            if (existingIp == currentClientIp)
            {
                log.LogInformation("Same IP, skipping update");
                return new OkObjectResult("ok");
            }

            log.LogInformation("Updating IP in Cloudflare...");
            // we need to update

            string zoneIdentifier = null;
            string recordIdentifier = null;
            
            zoneIdentifier = req.Query[nameof(zoneIdentifier)];
            recordIdentifier = req.Query[nameof(recordIdentifier)];

            if (string.IsNullOrWhiteSpace(zoneIdentifier) || string.IsNullOrWhiteSpace(recordIdentifier))
                return new BadRequestResult();
            
            log.LogInformation($"Updating {hostname} from {currentClientIp}");

            var cloudFlare = new CloudflareClient(Environment.GetEnvironmentVariable("CLOUDFLARE_APITOKEN"));
            await cloudFlare.UpdateDnsZone(hostname, zoneIdentifier, recordIdentifier, currentClientIp);
            
            log.LogInformation("IP Updated");

            using (var writer = await binder.BindAsync<TextWriter>(new BlobAttribute(
                $"registrations/{hostname.Replace('.', '-')}.txt", FileAccess.Write)))
            {
                await writer.WriteAsync(currentClientIp);
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
