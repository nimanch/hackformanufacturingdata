using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Base64ToXMLConvertor
{
    public static class BlobProcessorExtensions
    {
       public static List<string> GetDecodedString(this Stream BlobStream, ILogger log)
       {
            var finalList = new List<string>();
            using (StreamReader reader = new StreamReader(BlobStream))
            {
                var body = reader.ReadToEnd();
                var coreBody = body.GetCoreData();
                log.LogInformation($"Total Elements = {coreBody.Count}");
                foreach (var element in coreBody)
                {
                    var decodedstring = element.DecodeStringFromBase64();
                    log.LogInformation($"{decodedstring}");
                    finalList.Add(decodedstring);
                }
            }
            return finalList;
        }

        public static void WriteToBlob(this Stream outputStream, List<string> elements)
        {
            using (var writer = new StreamWriter(outputStream))
            {
                List<JArray> jarrays = new List<JArray>();
                foreach (string str in elements)
                {
                    jarrays.Add(JArray.Parse(str));
                }
                var concatenated = new JArray(jarrays.SelectMany(arr => arr));
                writer.Write(concatenated.ToString());
            }
        }
    }
}
