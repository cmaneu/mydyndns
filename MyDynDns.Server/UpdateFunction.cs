using System;
using System.IO;
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

            return new OkObjectResult("ok");
        }
    }
}
