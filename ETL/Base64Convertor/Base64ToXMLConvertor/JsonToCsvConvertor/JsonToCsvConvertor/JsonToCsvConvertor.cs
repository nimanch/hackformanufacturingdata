

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
        public static async Task Run([BlobTrigger("messagefiles/iot-hub-output/{name}.json", Connection = "AzureWebJobsStorage")] Stream myBlob,string name,
            [Blob("cdm/jabilcdm/dtmi_opcfoundation_org_UA_MiaB_Tags-1/partition-data.csv", FileAccess.ReadWrite, Connection = "AzureWebJobsStorage")] CloudBlockBlob blob, ILogger log)
        {   
            try
            {
                log.LogInformation($"Converting Blob to CSV for file {name} ");
                var data = await myBlob.GetMiabData().ConfigureAwait(false);
                await blob.AppendToBlob(data).ConfigureAwait(false);
            }
            catch(Exception e)
            {
                log.LogError($"Exception Occured : {e}");
            }
            log.LogInformation("Conversion Complete");
        }
    }
}
