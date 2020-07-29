using System.IO;
using Xunit;
using FluentAssertions;
using JsonToCsvConvertor;
using System.Collections.Generic;
using JsonToCsvConvertor;
using JsonToCsvConvertor.Data;
using System.Threading.Tasks;

namespace Base64ToXMLConvertor.Test.Unit
{
    public class JsonConvertorTests
    {
        [Fact]
        public async Task Can_append_to_csv()
        {
            List<MiabData> miabData;
            using (var fs = new FileStream("output.json", FileMode.Open, FileAccess.Read))
            {
                miabData = await fs.GetMiabData().ConfigureAwait(false);
            }
            using (var fs = new FileStream("partition-data.csv", FileMode.Append, FileAccess.Write))
            {
                fs.AppendToCSV(miabData);
            }
        }
    }
}
