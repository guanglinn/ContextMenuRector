using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ContextMenuManager.Helper
{
    internal class SimplePropertyReader
    {
        public Dictionary<string, string> ReadEmbeddedText(string resourceName)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
                if (stream == null)
                {
                    return keyValuePairs;
                }

                using (StreamReader streamReader = new StreamReader(stream, Encoding.UTF8))
                {
                    string content = streamReader.ReadToEnd();
                    List<string> lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    lines.ForEach(line => line.Trim());
                    lines.RemoveAll(line => line.StartsWith(";") || line.StartsWith("#") || line.StartsWith("["));

                    foreach (string line in lines)
                    {
                        int index = line.LastIndexOf('=');
                        if (index > 0)
                        {
                            keyValuePairs.Add(line.Substring(0, index).TrimEnd().Trim('"'), line.Substring(index + 1).TrimStart());
                        }
                    }
                    return keyValuePairs;
                }
            }
        }
    }
}
