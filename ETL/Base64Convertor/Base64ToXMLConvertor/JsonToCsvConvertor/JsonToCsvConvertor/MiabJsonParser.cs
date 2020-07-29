
using JsonToCsvConvertor.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace JsonToCsvConvertor
{
    public static class MiabJsonParser
    {
        public static List<MiabData> GetMiabData(this Stream blobStream)
        {
            var inputdataset = new List<MiabData>();
            using (var reader = new StreamReader(blobStream))
            {
                var jsonContent = reader.ReadToEnd();
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

        public static void AppendToCSV(this Stream csvStream,List<MiabData> miabData)
        {
            using (var writer = new StreamWriter(csvStream))
            {
                foreach (var data in miabData)
                {
                    writer.WriteLine("");
                    var dat = data.ToString();
                    writer.WriteLine(dat);
                }
            }
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
