using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Base64ToXMLConvertor
{
    public static class Base64ToXMLConvertor
    {
        [FunctionName("Base64ToXMLConvertor")]
        public static void Run([BlobTrigger("messagefiles/iot-hub-hack/{name}.json", Connection = "AzureWebJobsStorage")]Stream myBlob,
            [Blob("messagefiles/iot-hub-output/{name}.json", FileAccess.Write)] Stream outputStream,
            string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            var decodedElements = myBlob.GetDecodedString(log);
            outputStream.WriteToBlob(decodedElements);
        } 
    }
}
