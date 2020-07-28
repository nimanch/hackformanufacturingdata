
using System.Collections.Generic;
using System.IO;
using Base64ToXMLConvertor;
using Xunit;
using NSubstitute;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Globalization;
using FluentAssertions;

namespace Base64ToXMLConvertor.Test.Unit
{
    public class BlobProcessorTests
    {
        [Fact]
        public void Can_get_decoded_data()
        {
            var decodedList = new List<string>();
            var logger = Substitute.For<ILogger>();
            using (var fs = new FileStream("input.json",FileMode.Open,FileAccess.Read))
            {
                decodedList = fs.GetDecodedString(logger);
            }
            using (var fs = new FileStream("codeoutput.json", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                fs.WriteToBlob(decodedList);
            }
            var actualstring = File.ReadAllText("codeoutput.json");
            var expectedString = File.ReadAllText("output.json");
            actualstring.Should().Be(expectedString);
        }
    }
}
