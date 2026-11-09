using Automatica.Core.Internals.Cache.Common;

namespace Automatica.Core.Mcp
{
    /// <summary>
    /// Server settings (seeded by DatabaseInit, editable on the settings page) that control the MCP endpoint.
    /// </summary>
    public static class McpSettings
    {
        public const string Group = "SERVER.MCP";
        public const string EnabledKey = "mcpEnabled";
        public const string ApiKeyKey = "mcpApiKey";
        public const string AllowWriteKey = "mcpAllowWrite";
        public const string AllowLogsKey = "mcpAllowLogs";

        public static bool IsEnabled(this ISettingsCache cache) => ReadBool(cache, EnabledKey);

        public static bool IsWriteAllowedForApiKey(this ISettingsCache cache) => ReadBool(cache, AllowWriteKey);

        public static bool IsLogAccessAllowedForApiKey(this ISettingsCache cache) => ReadBool(cache, AllowLogsKey);

        public static string GetApiKey(this ISettingsCache cache)
        {
            return cache.GetByKey(ApiKeyKey)?.ValueText;
        }

        private static bool ReadBool(ISettingsCache cache, string key)
        {
            var setting = cache.GetByKey(key);
            return setting?.ValueInt is > 0;
        }
    }
}
