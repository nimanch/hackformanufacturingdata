using System;
using System.Collections.Generic;
using System.Text;

namespace Base64ToXMLConvertor
{
    public static class DataExtractionExtensions
    {
        public static List<string> GetCoreData(this string jsonBody)
        {
            var result = new List<string>();
            var temp = jsonBody;
            for (int startindex = 0; startindex <= jsonBody.Length;)
            {
                startindex = temp.IndexOf("\"Body\":");
                if (startindex == -1)
                {
                    break;
                }
                startindex += +"\"Body\":".Length;
                var core = temp.Substring(startindex);
                if (core == null || core == string.Empty)
                {
                    break;
                }
                var endindex = core.IndexOf("}");
                if (endindex == -1)
                {
                    break;
                }
                var core1 = core.Substring(0, endindex);
                if (core1 == null || core1 == string.Empty)
                {
                    break;
                }
                core1 = core1.Replace("\"", "");
                result.Add(core1);
                temp = temp.Substring(endindex);
            }
            return result;
        }


        public static string DecodeStringFromBase64(this string stringToDecode)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(stringToDecode));
        }
    }
}
