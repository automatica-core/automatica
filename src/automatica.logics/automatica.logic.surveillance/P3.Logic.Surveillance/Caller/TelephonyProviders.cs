using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Logic.Surveillance.Caller
{
    /// <summary>
    /// A voice call that reads <see cref="Message"/> with text to speech
    /// </summary>
    public sealed record CallRequest(string To, string Message, string Language);

    public sealed record CallResult(bool Success, string Info)
    {
        public static CallResult Ok(string info = null) => new(true, info ?? "");
        public static CallResult Failed(string info) => new(false, info ?? "");
    }

    /// <summary>
    /// Places phone calls. Implementations must never log the message text or the full number.
    /// </summary>
    public interface ITelephonyProvider
    {
        string Name { get; }
        Task<CallResult> PlaceCallAsync(CallRequest request, CancellationToken token = default);
    }

    /// <summary>
    /// No provider configured: nothing is called, the attempt is only reported (for tests of the wiring)
    /// </summary>
    public sealed class SimulatedTelephonyProvider : ITelephonyProvider
    {
        public string Name => "simulation";

        public Task<CallResult> PlaceCallAsync(CallRequest request, CancellationToken token = default) =>
            Task.FromResult(CallResult.Ok("simulated, no call placed"));
    }

    /// <summary>
    /// Twilio Programmable Voice: POST /2010-04-01/Accounts/{sid}/Calls.json with inline TwiML (&lt;Say&gt;).
    /// Needs an account SID, an auth token and a Twilio number (or a verified caller id) as the caller.
    /// Not tested against the live service.
    /// </summary>
    public sealed class TwilioTelephonyProvider : ITelephonyProvider
    {
        // one client for the process (sockets are pooled)
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _from;
        private readonly Uri _baseUri;

        public TwilioTelephonyProvider(string accountSid, string authToken, string from, Uri baseUri = null)
        {
            _accountSid = accountSid?.Trim() ?? "";
            _authToken = authToken?.Trim() ?? "";
            _from = from?.Trim() ?? "";
            _baseUri = baseUri ?? new Uri("https://api.twilio.com/");
        }

        public string Name => "twilio";

        internal static string TwiML(string message, string language)
        {
            var text = SecurityElement.Escape(message ?? "");
            var lang = SecurityElement.Escape(string.IsNullOrWhiteSpace(language) ? "en-US" : language);
            // read twice: the callee may miss the beginning
            return $"<Response><Say language=\"{lang}\" loop=\"2\">{text}</Say></Response>";
        }

        public async Task<CallResult> PlaceCallAsync(CallRequest request, CancellationToken token = default)
        {
            if (!Regex.IsMatch(_accountSid, "^AC[0-9a-fA-F]{32}$") || string.IsNullOrEmpty(_authToken) || PhoneNumbers.Normalize(_from) == null)
            {
                return CallResult.Failed("twilio account, token or caller number not configured");
            }

            var uri = new Uri(_baseUri, $"2010-04-01/Accounts/{_accountSid}/Calls.json");
            using var message = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["To"] = request.To,
                    ["From"] = PhoneNumbers.Normalize(_from),
                    ["Twiml"] = TwiML(request.Message, request.Language),
                    ["Timeout"] = "40"
                })
            };
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_accountSid}:{_authToken}")));

            try
            {
                using var response = await Http.SendAsync(message, token).ConfigureAwait(false);
                return response.IsSuccessStatusCode
                    ? CallResult.Ok($"twilio {(int)response.StatusCode}")
                    : CallResult.Failed($"twilio HTTP {(int)response.StatusCode}");
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                return CallResult.Failed("twilio not reachable: " + e.GetType().Name);
            }
        }
    }

    public static class PhoneNumbers
    {
        /// <summary>
        /// "0043 664 1234567", "+43-664/1234567" → "+436641234567"; null if it is no international number
        /// </summary>
        public static string Normalize(string number)
        {
            if (string.IsNullOrWhiteSpace(number))
            {
                return null;
            }

            var digits = Regex.Replace(number.Trim(), "[\\s\\-/().]", "");
            if (digits.StartsWith("00", StringComparison.Ordinal))
            {
                digits = "+" + digits.Substring(2);
            }

            return Regex.IsMatch(digits, "^\\+[1-9][0-9]{6,14}$") ? digits : null;
        }

        /// <summary>
        /// For logs and the visualization: only the last three digits
        /// </summary>
        public static string Mask(string number)
        {
            if (string.IsNullOrEmpty(number))
            {
                return "";
            }
            return number.Length <= 3 ? "***" : "***" + number.Substring(number.Length - 3);
        }
    }
}
