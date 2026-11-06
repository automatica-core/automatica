using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly: InternalsVisibleTo("Automatica.Core.Driver")]

namespace Automatica.Core.Base.IO
{
    /// <summary>
    /// Tracks how many dispatch hops led to the value that is currently being processed (flows with the async context).
    /// Used to detect dispatch loops (A -> B -> A) which would otherwise spin forever.
    /// </summary>
    internal readonly struct DispatchChain
    {
        /// <summary>
        /// Maximum number of dispatch hops within <see cref="Window"/> before a chain is treated as a loop.
        /// </summary>
        internal const int MaxDepth = 64;

        /// <summary>
        /// Chains older than this restart at depth 1, so timed feedback (e.g. a blinker built from a delay logic) is not treated as a loop.
        /// </summary>
        internal static readonly TimeSpan Window = TimeSpan.FromSeconds(5);

        private static readonly AsyncLocal<DispatchChain> CurrentChain = new();

        internal int Depth { get; }
        internal long StartTimestamp { get; }

        private DispatchChain(int depth, long startTimestamp)
        {
            Depth = depth;
            StartTimestamp = startTimestamp;
        }

        internal bool IsEmpty => Depth == 0;

        /// <summary>
        /// The chain of the value that is currently being processed in this async context (empty if none)
        /// </summary>
        internal static DispatchChain Current
        {
            get => CurrentChain.Value;
            set => CurrentChain.Value = value;
        }

        /// <summary>
        /// Calculates the chain of a new dispatch that happens in the current async context
        /// </summary>
        internal static DispatchChain Next()
        {
            var parent = Current;
            var now = Stopwatch.GetTimestamp();

            if (parent.IsEmpty || Stopwatch.GetElapsedTime(parent.StartTimestamp, now) > Window)
            {
                return new DispatchChain(1, now);
            }

            return new DispatchChain(parent.Depth + 1, parent.StartTimestamp);
        }
    }
}
