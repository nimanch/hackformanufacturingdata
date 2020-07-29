

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace JsonToCsvConvertor
{
    public static class JsonToCsvConvertor
    {
        [FunctionName("JsonToCsvConvertor")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequest req,
            [Blob("cdm/jabilcdm/dtmi_opcfoundation_org_UA_MiaB_Tags-1/partition-data.csv", FileAccess.ReadWrite, Connection = "AzureWebJobsStorage")] CloudBlockBlob blob, ILogger log)
        {   
            try
            {
                log.LogInformation($"Converting Blob to CSV ");
                var data = await req.Body.GetMiabData().ConfigureAwait(false);
                await blob.AppendToBlob(data).ConfigureAwait(false);
            }
            catch(Exception e)
            {
                return new BadRequestObjectResult($"Exception Occured : {e}");
            }
            return new OkObjectResult("Conversion Complete");
        }
    }
}
