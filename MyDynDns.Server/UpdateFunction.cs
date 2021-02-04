using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MyDynDns.Server
{
    public static class UpdateFunction
    {
        [FunctionName(nameof(Update))]
        public static async Task<IActionResult> Update(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "update/{hostname}")] HttpRequest req,
            string hostname,
            ILogger log)
        {
            log.LogInformation($"Update request for {hostname}");

            string zoneIdentifier = null;
            string recordIdentifier = null;

            zoneIdentifier = req.Query[nameof(zoneIdentifier)];
            recordIdentifier = req.Query[nameof(recordIdentifier)];

            if (string.IsNullOrWhiteSpace(zoneIdentifier) || string.IsNullOrWhiteSpace(recordIdentifier))
                return new BadRequestResult();

            string content = GetIpFromRequestHeaders(req);

            log.LogInformation($"Updating {hostname} from {content}");

            var cloudFlare = new CloudflareClient(Environment.GetEnvironmentVariable("CLOUDFLARE_APITOKEN"));

            await cloudFlare.UpdateDnsZone(hostname, zoneIdentifier, recordIdentifier, content);
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
