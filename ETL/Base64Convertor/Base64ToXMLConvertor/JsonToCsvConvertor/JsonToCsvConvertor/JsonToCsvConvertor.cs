

using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace JsonToCsvConvertor
{
    public static class JsonToCsvConvertor
    {
        [FunctionName("JsonToCsvConvertor")]
        public static void Run(Stream inputBlob, ILogger log)
        {
            log.LogInformation($"Converting Blog to CSV");
            inputBlob.AppendToCSV(inputBlob.GetMiabData());
        }
    }
}
