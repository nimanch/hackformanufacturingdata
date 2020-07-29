
using JsonToCsvConvertor.Data;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace JsonToCsvConvertor
{
    public static class MiabJsonParser
    {
        public static async Task<List<MiabData>> GetMiabData(this Stream blobStream)
        {
            var inputdataset = new List<MiabData>();
            using (var reader = new StreamReader(blobStream))
            {
                var jsonContent = await reader.ReadToEndAsync().ConfigureAwait(false);
                var input = JArray.Parse(jsonContent);
                foreach (var token in input)
                {
                    var node = new MiabData();
                    node.dtId = Guid.NewGuid();
                    node.timestamp = GetTimeFromToken(token);
                    node.Analog = GetEntittyFromToken(token, "Analog");
                    node.CO2 = GetEntittyFromToken(token, "CO2");
                    node.Humidity = GetEntittyFromToken(token, "Humidity");
                    node.Temperature = GetEntittyFromToken(token, "Temperature");
                    node.Digital = GetEntittyFromToken(token, "Digital");
                    node.Blue = GetEntittyFromToken(token, "Blue");
                    inputdataset.Add(node);
                }

                return inputdataset;
            }
        }

        public static async Task<bool> AppendToCSV(this Stream csvStream, List<MiabData> miabData)
        {
           
            using (var writer = new StreamWriter(csvStream))
            {
                await writer.WriteLineAsync("").ConfigureAwait(false);
                foreach (var data in miabData)
                {

                    var dat = data.ToString();
                    await writer.WriteLineAsync(dat).ConfigureAwait(false);
                }
                return true;
            }

        }

        public static async Task<bool> AppendToBlob(this CloudBlockBlob blob, List<MiabData> miabData)
        {

            using (MemoryStream stream = new MemoryStream())
            {
                await blob.DownloadToStreamAsync(stream).ConfigureAwait(false);
                using (StreamWriter sw = new StreamWriter(stream))
                {
                    await sw.WriteLineAsync("").ConfigureAwait(false);
                    foreach (var item in miabData)
                    {
                        await sw.WriteLineAsync(item.ToString()).ConfigureAwait(false);
                    }
                    sw.Flush();
                    stream.Position = 0;
                    await blob.UploadFromStreamAsync(stream).ConfigureAwait(false);
                }
            }
            return true;
        }

        private static string GetTimeFromToken(JToken token)
        {
            return token.Value<JToken>("Value").Value<string>("SourceTimestamp");
        }

        private static string GetEntittyFromToken(JToken token, string entityName)
        {
            if (token.Value<string>("NodeId").Contains(entityName))
            {
                return token.Value<JToken>("Value").Value<string>("Value");
            }
            return string.Empty;
        }
    }
}
