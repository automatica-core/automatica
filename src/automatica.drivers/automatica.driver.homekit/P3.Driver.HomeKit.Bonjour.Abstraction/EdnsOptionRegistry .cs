using System;
using System.Collections.Generic;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Metadata on EDNS options.
    /// </summary>
    /// <see cref="EdnsOption"/>
    public static class EdnsOptionRegistry
    {
        /// <summary>
        ///   All the EDNS options.
        /// </summary>
        /// <remarks>
        ///   The key is the <see cref="EdnsOptionType"/>.
        ///   The value is a function that returns a new <see cref="EdnsOption"/>.
        /// </remarks>
        public static Dictionary<EdnsOptionType, Func<EdnsOption>> Options;

        static EdnsOptionRegistry()
        {
            Options = new Dictionary<EdnsOptionType, Func<EdnsOption>>();
            Register<EdnsPaddingOption>();
            Register<EdnsNSIDOption>();
            Register<EdnsKeepaliveOption>();
            Register<EdnsDAUOption>();
            Register<EdnsDHUOption>();
            Register<EdnsN3UOption>();
        }

        /// <summary>
        ///   Register a new EDNS option.
        /// </summary>
        /// <typeparam name="T">
        ///   A type that is derived from <see cref="EdnsOption"/>.
        /// </typeparam>
        public static void Register<T>() where T : EdnsOption, new()
        {
            var option = new T();
            Options.Add(option.Type, () => new T());
        }

    }
}
