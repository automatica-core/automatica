using System;
using System.Collections.Generic;
using System.Linq;

namespace Automatica.Core.Internals.UserHelper
{
    /// <summary>
    /// Minimal password policy for new passwords: length, not the user name, not a well known default or common password.
    /// </summary>
    public static class PasswordPolicy
    {
        public const int MinLength = 8;
        public const int MaxLength = 256;

        /// <summary>
        /// The old default credentials (user name = password) and very common passwords
        /// </summary>
        private static readonly HashSet<string> Weak = new(StringComparer.OrdinalIgnoreCase)
        {
            "sa", "visu", "admin", "administrator", "automatica", "password", "passwort", "12345678", "123456789",
            "1234567890", "qwertzui", "qwertyui", "qwertz123", "qwerty123", "password1", "passwort1", "changeme", "letmein1",
            "11111111", "00000000", "abcdefgh", "iloveyou"
        };

        /// <returns>null if the password is acceptable, otherwise the reason (a translation key)</returns>
        public static string Validate(string password, string userName)
        {
            if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            {
                return "PASSWORD_TOO_SHORT";
            }

            if (password.Length > MaxLength)
            {
                return "PASSWORD_TOO_LONG";
            }

            if (!string.IsNullOrEmpty(userName) && string.Equals(password, userName, StringComparison.OrdinalIgnoreCase))
            {
                return "PASSWORD_EQUALS_USERNAME";
            }

            if (Weak.Contains(password) || password.Distinct().Count() < 3)
            {
                return "PASSWORD_TOO_WEAK";
            }

            return null;
        }

        /// <summary>
        /// The password a user signed in with must be replaced (old default credentials sa/sa, visu/visu,
        /// or the password equals the user name)
        /// </summary>
        public static bool IsDefaultPassword(string userName, string password)
        {
            return !string.IsNullOrEmpty(password) && string.Equals(userName, password, StringComparison.OrdinalIgnoreCase);
        }
    }
}
