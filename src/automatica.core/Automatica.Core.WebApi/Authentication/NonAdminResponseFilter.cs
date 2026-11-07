using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Automatica.Core.WebApi.Authentication
{
    /// <summary>
    /// The viewer and visu endpoints return the cached entities (nodes with their properties and child trees, visualization
    /// pages, logics). For every authenticated non-admin user the response is reduced before it is written:
    /// <list type="bullet">
    /// <item>values of <see cref="PropertyTemplateType.Password"/> properties are removed (driver credentials)</item>
    /// <item>nested nodes, logics, areas, categories, pages and visu objects of other user groups are removed
    /// (the endpoints check only the requested object itself)</item>
    /// </list>
    /// Admin responses and anonymous endpoints are not touched.
    /// </summary>
    public sealed class NonAdminResponseFilter : IAsyncResultFilter
    {
        private const string MaskedValue = null;

        private readonly JsonSerializerSettings _settings;

        /// <param name="settings">the serializer settings of the web api (the response is written with the same names)</param>
        public NonAdminResponseFilter(JsonSerializerSettings settings)
        {
            _settings = settings;
        }

        public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (context.Result is ObjectResult { Value: { } value } result &&
                user?.Identity?.IsAuthenticated == true &&
                !user.IsInRole(Role.AdminRole) &&
                !IsSimple(value))
            {
                result.Value = Sanitize(value, _settings, GetUserGroups(user));
                result.DeclaredType = typeof(JToken);
            }

            return next();
        }

        internal static HashSet<string> GetUserGroups(ClaimsPrincipal user)
        {
            return user.Claims.Where(a => a.Type == UserGroup.ClaimType).Select(a => a.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsSimple(object value)
        {
            var type = value.GetType();
            return type.IsPrimitive || type.IsEnum || value is string or decimal or Guid or DateTime or DateTimeOffset or JValue;
        }

        internal static JToken Sanitize(object value, JsonSerializerSettings settings, ISet<string> userGroups)
        {
            var token = value as JToken ?? JToken.FromObject(value, JsonSerializer.Create(settings ?? new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }));

            // the requested object itself was checked by the endpoint, only nested objects are filtered
            if (token is JObject root)
            {
                SanitizeObject(root, userGroups);
            }
            else
            {
                SanitizeChildren(token, userGroups);
            }

            return token;
        }

        private static void SanitizeObject(JObject obj, ISet<string> userGroups)
        {
            if (IsPasswordProperty(obj))
            {
                foreach (var name in new[] { "valueString", "ValueString", "value", "Value" })
                {
                    if (obj.ContainsKey(name))
                    {
                        obj[name] = MaskedValue;
                    }
                }
            }

            SanitizeChildren(obj, userGroups);
        }

        private static void SanitizeChildren(JToken token, ISet<string> userGroups)
        {
            switch (token)
            {
                case JObject obj:
                    foreach (var property in obj.Properties().ToList())
                    {
                        if (property.Value is JObject child)
                        {
                            if (IsHidden(child, userGroups))
                            {
                                property.Value = JValue.CreateNull();
                            }
                            else
                            {
                                SanitizeObject(child, userGroups);
                            }
                        }
                        else if (property.Value is JArray)
                        {
                            SanitizeChildren(property.Value, userGroups);
                        }
                    }

                    break;
                case JArray array:
                    for (var i = array.Count - 1; i >= 0; i--)
                    {
                        if (array[i] is JObject child)
                        {
                            if (IsHidden(child, userGroups))
                            {
                                array.RemoveAt(i);
                            }
                            else
                            {
                                SanitizeObject(child, userGroups);
                            }
                        }
                        else if (array[i] is JArray)
                        {
                            SanitizeChildren(array[i], userGroups);
                        }
                    }

                    break;
            }
        }

        private static JToken Get(JObject obj, string camelCaseName)
        {
            return obj.GetValue(camelCaseName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Nodes, logics, areas, categories, pages and visu objects carry a user group, the user-to-group join entities
        /// (which also have This2User) are not filtered.
        /// </summary>
        private static bool IsHidden(JObject obj, ISet<string> userGroups)
        {
            var group = Get(obj, "this2UserGroup");
            if (group == null || group.Type == JTokenType.Null || Get(obj, "this2User") != null)
            {
                return false;
            }

            var value = group.ToString();
            return !string.IsNullOrEmpty(value) && !userGroups.Contains(value);
        }

        private static bool IsPasswordProperty(JObject obj)
        {
            if (Get(obj, "this2PropertyTemplate") == null)
            {
                return false;
            }

            const long password = (long)PropertyTemplateType.Password;
            if (Get(obj, "propertyType") is JValue type &&
                ((type.Type == JTokenType.Integer && Convert.ToInt64(type.Value) == password) ||
                 (type.Type == JTokenType.String && string.Equals((string)type.Value, nameof(PropertyTemplateType.Password), StringComparison.OrdinalIgnoreCase))))
            {
                return true;
            }

            return Get(obj, "this2PropertyTemplateNavigation") is JObject template &&
                   Get(template, "this2PropertyType") is JValue { Type: JTokenType.Integer } templateType &&
                   Convert.ToInt64(templateType.Value) == password;
        }
    }
}
