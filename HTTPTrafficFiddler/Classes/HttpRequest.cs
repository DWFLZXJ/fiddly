using System;
using System.Text.RegularExpressions;

namespace HTTPTrafficFiddler.Classes
{
    class HttpRequest
    {
        public String Type;
        public String Path;
        public String Host;
        public String Version;

        private static Regex regexPath = new Regex("(GET|POST) (.*?) (HTTP/1.[0-1])\r\n", RegexOptions.Compiled);
        private static Regex regexHost = new Regex("Host: (.*?)\r\n", RegexOptions.Compiled);

        private HttpRequest(String type, String path, String version, String host)
        {
            Type = type;
            Path = path;
            Host = host;
            Version = version;
        }

        /// <summary>
        /// Try parsing "String" as an HTTP request. Returns "null" if parsing fails.
        /// </summary>
        /// <param name="data">String representation of an HTTP request</param>
        public static HttpRequest TryParse(String data)
        {
            // check first char to skip regex if possible
            if (data.Length == 0 || (data[0] != 'G' && data[0] != 'P')) return null; 

            var pathMatch = regexPath.Match(data);
            var hostMatch = regexHost.Match(data);

            if (!pathMatch.Success || !hostMatch.Success) return null;

            var matchedGroups = pathMatch.Groups;
            var hostGroups = hostMatch.Groups;

            return new HttpRequest(matchedGroups[1].Value, matchedGroups[2].Value, matchedGroups[3].Value, hostGroups[1].Value);
        }
    }
}
