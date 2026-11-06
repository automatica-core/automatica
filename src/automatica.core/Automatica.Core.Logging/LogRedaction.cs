using System.Text.RegularExpressions;

namespace Automatica.Core.Logging
{
    /// <summary>
    /// Removes secrets that end up in logged urls: the SignalR access token (query string) and bearer tokens
    /// </summary>
    public static class LogRedaction
    {
        private static readonly Regex AccessToken = new(@"(access_token=)[^&\s""']+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex Bearer = new(@"(Bearer\s+)[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string Redact(string message)
        {
            if (string.IsNullOrEmpty(message) || (message.IndexOf("access_token", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                                                  message.IndexOf("Bearer", System.StringComparison.OrdinalIgnoreCase) < 0))
            {
                return message;
            }

            message = AccessToken.Replace(message, "$1***");
            return Bearer.Replace(message, "$1***");
        }
    }
}
