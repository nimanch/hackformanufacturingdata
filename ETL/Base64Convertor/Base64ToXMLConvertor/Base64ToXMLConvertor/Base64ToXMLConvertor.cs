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
            var finalList = new List<string>();
            using (StreamReader reader = new StreamReader(myBlob))
            {                   
                var body = reader.ReadToEnd();
                var coreBody = GetCoreData(body);
                log.LogInformation($"Total Elements = {coreBody.Count}");
                foreach(var elem in coreBody)
                {
                    var el = DecodeStringFromBase64(elem);
                    log.LogInformation($"{el}");
                    finalList.Add(el);
                }
            }

            using (var writer = new StreamWriter(outputStream))
            {
                foreach(string str in finalList)
                {
                    writer.Write(str);
                }
            }

        }

        public static string DecodeStringFromBase64(string stringToDecode)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(stringToDecode));
        }

        public static List<string> GetCoreData(string jsonBody)
        {
            var result =new List<string>();
            var temp = jsonBody;
            for(int startindex=0;startindex<=jsonBody.Length;)
            {
                startindex = temp.IndexOf("\"Body\":");
                if(startindex == -1)
                {
                    break;
                }
                startindex+= +"\"Body\":".Length; 
                var core = temp.Substring(startindex);
                if(core==null || core == string.Empty)
                {
                    break;
                }
                var endindex = core.IndexOf("}");
                if(endindex == -1)
                {
                    break;
                }
                var core1 = core.Substring(0, endindex);
                if(core1 == null || core1 == string.Empty)
                {
                    break;
                }
                core1 = core1.Replace("\"", "");
                result.Add(core1);
                temp = temp.Substring(endindex);
            }
            return result;
        }

       
    }
}
