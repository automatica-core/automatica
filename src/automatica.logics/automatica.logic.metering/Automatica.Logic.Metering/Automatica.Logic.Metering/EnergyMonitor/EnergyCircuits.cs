using System;
using System.Collections.Generic;
using System.Linq;

namespace Automatica.Logic.Metering.EnergyMonitor
{
    /// <summary>
    /// Measured sub-circuits of the energy monitor (e.g. the CT channels of an energy monitor like the Refoss EM16P).
    /// Each circuit belongs to the house (0), one of the extra consumers (1-6) or another circuit (101-124), so circuits form
    /// a tree below the house and the consumers. The circuits below something do not have to cover it: what they do not
    /// measure is its rest.
    /// </summary>
    public sealed class EnergyCircuitInputs
    {
        public const int CircuitCount = 24;
        /// <summary>house + the extra consumers</summary>
        public const int GroupCount = EnergyFlowInputs.ConsumerCount + 1;
        public const int HouseGroup = 0;
        /// <summary>parent value of "belongs to circuit n" (n 1-based) is this + n</summary>
        public const int CircuitParentOffset = 100;

        /// <summary>power of each circuit in kW, null while the input never got a value</summary>
        public double?[] Power { get; } = new double?[CircuitCount];
        /// <summary>parent of each circuit as the parameter stores it: 0 = house, 1-6 = consumer 1-6, 101-124 = circuit 1-24</summary>
        public int[] Parent { get; } = new int[CircuitCount];
        public string[] Name { get; } = new string[CircuitCount];

        /// <summary>a circuit is used once it got a value or a name</summary>
        public bool IsUsed(int circuit) => Power[circuit].HasValue || !string.IsNullOrWhiteSpace(Name[circuit]);

        /// <summary>parameter value of "belongs to circuit" (0-based circuit)</summary>
        public static int CircuitParent(int circuit) => CircuitParentOffset + circuit + 1;

        /// <summary>the parent circuit (0-based) of a parent value, null for the house or a consumer</summary>
        public static int? ParentCircuit(int parent) => parent > CircuitParentOffset ? parent - CircuitParentOffset - 1 : null;

        /// <summary>
        /// House, consumer or another circuit; anything else (an old or wrong parameter value, the circuit itself) counts as
        /// house.
        /// </summary>
        public static int NormalizeParent(double? parent, int circuit)
        {
            if (!parent.HasValue || !double.IsFinite(parent.Value))
            {
                return HouseGroup;
            }
            var p = (int)Math.Round(parent.Value);
            if (p is >= 0 and < GroupCount)
            {
                return p;
            }
            return ParentCircuit(p) is { } c && c < CircuitCount && c != circuit ? p : HouseGroup;
        }
    }

    /// <summary>a circuit in the tree</summary>
    public sealed class EnergyCircuitNode
    {
        /// <summary>0-based</summary>
        public int Circuit { get; init; }
        /// <summary>the house or consumer at the top of its branch</summary>
        public int Group { get; init; }
        /// <summary>the circuit it belongs to, null directly below the house or a consumer</summary>
        public int? ParentCircuit { get; init; }
        /// <summary>the circuits that belong to it, by circuit number</summary>
        public IReadOnlyList<int> Children { get; init; } = Array.Empty<int>();
        /// <summary>
        /// its own power when it has a value, otherwise what its circuits measure; null when nothing in its branch is measured
        /// </summary>
        public double? Known { get; init; }
        /// <summary>what its circuits measure in kW</summary>
        public double Measured { get; init; }
        /// <summary>own power - measured, never below 0; only for a circuit with a value and circuits below it</summary>
        public double? Rest { get; init; }
    }

    /// <summary>one group (house or consumer) split into the circuits directly below it and the rest</summary>
    public sealed class EnergyCircuitGroup
    {
        public int Group { get; init; }
        /// <summary>power of the house or the consumer in kW</summary>
        public double Total { get; init; }
        /// <summary>what the circuits directly below it measure in kW</summary>
        public double Measured { get; init; }
        /// <summary>total - measured, never below 0 (circuits are not read at the same moment as the total)</summary>
        public double Rest { get; init; }
        /// <summary>the circuits directly below it, by circuit number</summary>
        public IReadOnlyList<int> Circuits { get; init; } = Array.Empty<int>();
    }

    public sealed class EnergyCircuitBreakdown
    {
        /// <summary>power of each circuit in kW, 0 without a value and never negative</summary>
        public double[] Power { get; init; } = new double[EnergyCircuitInputs.CircuitCount];
        /// <summary>the circuits in the tree: used ones and the circuits they belong to</summary>
        public IReadOnlyDictionary<int, EnergyCircuitNode> Circuits { get; init; } = new Dictionary<int, EnergyCircuitNode>();
        /// <summary>the groups that have at least one circuit, by group number</summary>
        public IReadOnlyDictionary<int, EnergyCircuitGroup> Groups { get; init; } = new Dictionary<int, EnergyCircuitGroup>();

        /// <summary>rest power of the group, null when the group has no circuits</summary>
        public double? Rest(int group) => Groups.TryGetValue(group, out var g) ? g.Rest : null;

        /// <summary>rest power of a circuit with circuits below it, else null</summary>
        public double? CircuitRest(int circuit) => Circuits.TryGetValue(circuit, out var c) ? c.Rest : null;
    }

    public static class EnergyCircuitCalculator
    {
        public static EnergyCircuitBreakdown Calculate(EnergyCircuitInputs inputs, EnergyFlowSnapshot snapshot)
        {
            const int count = EnergyCircuitInputs.CircuitCount;
            var power = new double[count];
            for (var i = 0; i < count; i++)
            {
                var p = inputs.Power[i];
                power[i] = p.HasValue && double.IsFinite(p.Value) ? Math.Max(p.Value, 0) : 0;
            }

            var parent = ResolveParents(inputs);

            // used circuits and every circuit above them
            var inTree = new bool[count];
            for (var i = 0; i < count; i++)
            {
                if (!inputs.IsUsed(i))
                {
                    continue;
                }
                for (int? c = i; c.HasValue && !inTree[c.Value]; c = parent[c.Value])
                {
                    inTree[c.Value] = true;
                }
            }

            var children = Enumerable.Range(0, count).ToDictionary(i => i, _ => new List<int>());
            var groupChildren = new Dictionary<int, List<int>>();
            for (var i = 0; i < count; i++)
            {
                if (!inTree[i])
                {
                    continue;
                }
                if (parent[i] is { } p)
                {
                    children[p].Add(i);
                }
                else
                {
                    var g = inputs.Parent[i] is >= 0 and < EnergyCircuitInputs.GroupCount ? inputs.Parent[i] : EnergyCircuitInputs.HouseGroup;
                    if (!groupChildren.TryGetValue(g, out var list))
                    {
                        groupChildren[g] = list = new List<int>();
                    }
                    list.Add(i);
                }
            }

            var nodes = new Dictionary<int, EnergyCircuitNode>();
            foreach (var (group, top) in groupChildren)
            {
                foreach (var circuit in top)
                {
                    AddNode(circuit, group, null);
                }
            }

            var groups = groupChildren.ToDictionary(g => g.Key, g =>
            {
                var total = GroupTotal(snapshot, g.Key);
                var measured = g.Value.Sum(i => nodes[i].Known ?? 0);
                return new EnergyCircuitGroup
                {
                    Group = g.Key,
                    Total = total,
                    Measured = measured,
                    Rest = Math.Max(total - measured, 0),
                    Circuits = g.Value
                };
            });

            return new EnergyCircuitBreakdown { Power = power, Circuits = nodes, Groups = groups };

            // depth first, the circuits below a circuit are known before the circuit itself
            void AddNode(int circuit, int group, int? parentCircuit)
            {
                var below = children[circuit];
                foreach (var child in below)
                {
                    AddNode(child, group, circuit);
                }
                var known = below.Select(c => nodes[c].Known).Where(k => k.HasValue).ToList();
                var measured = known.Sum(k => k.Value);
                var hasValue = inputs.Power[circuit].HasValue;
                nodes[circuit] = new EnergyCircuitNode
                {
                    Circuit = circuit,
                    Group = group,
                    ParentCircuit = parentCircuit,
                    Children = below,
                    Known = hasValue ? power[circuit] : known.Count > 0 ? measured : null,
                    Measured = measured,
                    Rest = hasValue && below.Count > 0 ? Math.Max(power[circuit] - measured, 0) : null
                };
            }
        }

        /// <summary>
        /// The parent circuit of each circuit, null below the house or a consumer. Circuits that belong to each other in a
        /// loop are put directly below the house.
        /// </summary>
        private static int?[] ResolveParents(EnergyCircuitInputs inputs)
        {
            const int count = EnergyCircuitInputs.CircuitCount;
            var parent = new int?[count];
            for (var i = 0; i < count; i++)
            {
                parent[i] = EnergyCircuitInputs.ParentCircuit(inputs.Parent[i]) is { } p && p < count && p != i ? p : null;
            }

            var inLoop = new bool[count];
            for (var i = 0; i < count; i++)
            {
                var c = parent[i];
                for (var step = 0; c.HasValue && step < count; step++, c = parent[c.Value])
                {
                    if (c.Value == i)
                    {
                        inLoop[i] = true;
                        break;
                    }
                }
            }
            for (var i = 0; i < count; i++)
            {
                if (inLoop[i])
                {
                    parent[i] = null;
                }
            }
            return parent;
        }

        /// <summary>house or consumer power of the snapshot in kW</summary>
        public static double GroupTotal(EnergyFlowSnapshot snapshot, int group)
        {
            if (group == EnergyCircuitInputs.HouseGroup)
            {
                return snapshot.House;
            }
            var consumer = group - 1;
            return consumer >= 0 && consumer < snapshot.Consumers.Length ? snapshot.Consumers[consumer] : 0;
        }
    }
}
