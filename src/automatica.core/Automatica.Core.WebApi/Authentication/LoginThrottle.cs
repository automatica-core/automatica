using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;

namespace Automatica.Core.WebApi.Authentication
{
    /// <summary>
    /// Per user name lockout of the login (in addition to the per client rate limit): after <see cref="FreeAttempts"/> failed
    /// logins the user name is locked for 30s, doubling with every further failure up to <see cref="MaxLockout"/>.
    /// A successful login resets it. In memory only: a restart unlocks everything.
    /// </summary>
    public class LoginThrottle
    {
        public const int FreeAttempts = 5;
        public static readonly TimeSpan MaxLockout = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Failures older than this are forgotten
        /// </summary>
        public static readonly TimeSpan FailureWindow = TimeSpan.FromHours(1);

        private const int MaxEntries = 10_000;

        private readonly ConcurrentDictionary<string, Entry> _entries = new();
        private readonly Func<DateTime> _now;

        private sealed class Entry
        {
            public int Failures;
            public DateTime LastFailure;
            public DateTime LockedUntil;
        }

        public LoginThrottle() : this(() => DateTime.UtcNow)
        {
        }

        internal LoginThrottle(Func<DateTime> now)
        {
            _now = now;
        }

        private static string Key(string userName) => (userName ?? string.Empty).Trim().ToLowerInvariant();

        /// <returns>true if the user name is locked, <paramref name="retryAfter"/> is the remaining time</returns>
        public bool IsLocked(string userName, out TimeSpan retryAfter)
        {
            retryAfter = TimeSpan.Zero;
            if (!_entries.TryGetValue(Key(userName), out var entry))
            {
                return false;
            }

            lock (entry)
            {
                var now = _now();
                if (entry.LockedUntil > now)
                {
                    retryAfter = entry.LockedUntil - now;
                    return true;
                }
            }

            return false;
        }

        public void Failed(string userName)
        {
            var now = _now();
            if (_entries.Count > MaxEntries)
            {
                Cleanup(now);
            }

            var entry = _entries.GetOrAdd(Key(userName), _ => new Entry());
            lock (entry)
            {
                if (now - entry.LastFailure > FailureWindow)
                {
                    entry.Failures = 0;
                }

                entry.Failures++;
                entry.LastFailure = now;

                if (entry.Failures >= FreeAttempts)
                {
                    var exponent = Math.Min(entry.Failures - FreeAttempts, 10);
                    var lockout = TimeSpan.FromSeconds(30 * Math.Pow(2, exponent));
                    entry.LockedUntil = now + (lockout > MaxLockout ? MaxLockout : lockout);
                }
            }
        }

        public void Succeeded(string userName)
        {
            _entries.TryRemove(Key(userName), out _);
        }

        private void Cleanup(DateTime now)
        {
            foreach (var item in _entries.Where(a => now - a.Value.LastFailure > FailureWindow && a.Value.LockedUntil < now).ToList())
            {
                _entries.TryRemove(item.Key, out _);
            }
        }

        /// <summary>
        /// The client address for the login rate limit. Requests through the local Remote Connect tunnel (frpc) or a local
        /// reverse proxy come from the loopback address: only for them the last X-Forwarded-For entry (the one the proxy added)
        /// is used. From any other peer the header is ignored, it can be forged.
        /// </summary>
        public static string GetClientKey(HttpContext context)
        {
            var remote = context.Connection.RemoteIpAddress;
            if (remote != null && IPAddress.IsLoopback(remote))
            {
                var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
                if (!string.IsNullOrWhiteSpace(forwarded))
                {
                    var last = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();
                    if (IPAddress.TryParse(last, out var forwardedAddress))
                    {
                        return "fwd:" + forwardedAddress;
                    }
                }
            }

            return remote?.ToString() ?? "unknown";
        }

        /// <summary>
        /// A user name as it may be logged: no control characters (log forging), at most 64 characters
        /// </summary>
        public static string SanitizeForLog(string value)
        {
            if (value == null)
            {
                return "<null>";
            }

            var cleaned = new string(value.Select(c => char.IsControl(c) ? '_' : c).ToArray());
            return cleaned.Length > 64 ? cleaned.Substring(0, 64) + "..." : cleaned;
        }
    }
}
