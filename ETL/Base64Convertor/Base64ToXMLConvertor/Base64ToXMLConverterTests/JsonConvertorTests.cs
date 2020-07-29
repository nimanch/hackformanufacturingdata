using System.IO;
using Xunit;
using FluentAssertions;
using JsonToCsvConvertor;
using System.Collections.Generic;
using JsonToCsvConvertor;
using JsonToCsvConvertor.Data;

namespace Base64ToXMLConvertor.Test.Unit
{
    public class JsonConvertorTests
    {
        [Fact]
        public void Can_append_to_csv()
        {
            List<MiabData> miabData;
            using (var fs = new FileStream("output.json", FileMode.Open, FileAccess.Read))
            {
                miabData = fs.GetMiabData();
            }
            using (var fs = new FileStream("partition-data.csv", FileMode.Append, FileAccess.Write))
            {
                fs.AppendToCSV(miabData);
            }
        }
    }
}
