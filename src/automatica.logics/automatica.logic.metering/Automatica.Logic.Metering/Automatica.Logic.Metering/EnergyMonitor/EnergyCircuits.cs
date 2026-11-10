using System;
using System.Collections.Generic;
using System.Linq;

namespace Automatica.Logic.Metering.EnergyMonitor
{
    /// <summary>
    /// Measured sub-circuits of the energy monitor (e.g. the CT channels of an energy monitor like the Refoss EM16P).
    /// Each circuit belongs to a group: the house (0) or one of the extra consumers (1-6). The circuits of a group do not
    /// have to cover it: what is not measured is the rest of the group.
    /// </summary>
    public sealed class EnergyCircuitInputs
    {
        public const int CircuitCount = 18;
        /// <summary>house + the extra consumers</summary>
        public const int GroupCount = EnergyFlowInputs.ConsumerCount + 1;
        public const int HouseGroup = 0;

        /// <summary>power of each circuit in kW, null while the input never got a value</summary>
        public double?[] Power { get; } = new double?[CircuitCount];
        /// <summary>group of each circuit: 0 = house, 1-6 = consumer 1-6</summary>
        public int[] Group { get; } = new int[CircuitCount];
        public string[] Name { get; } = new string[CircuitCount];

        /// <summary>a circuit is used once it got a value or a name</summary>
        public bool IsUsed(int circuit) => Power[circuit].HasValue || !string.IsNullOrWhiteSpace(Name[circuit]);

        /// <summary>0-6, anything else (an old or wrong parameter value) counts as house</summary>
        public static int NormalizeGroup(double? group)
        {
            if (!group.HasValue || !double.IsFinite(group.Value))
            {
                return HouseGroup;
            }
            var g = (int)Math.Round(group.Value);
            return g is >= 0 and < GroupCount ? g : HouseGroup;
        }
    }

    /// <summary>one group (house or consumer) split into its measured circuits and the rest</summary>
    public sealed class EnergyCircuitGroup
    {
        public int Group { get; init; }
        /// <summary>power of the house or the consumer in kW</summary>
        public double Total { get; init; }
        /// <summary>sum of the measured circuits in kW</summary>
        public double Measured { get; init; }
        /// <summary>total - measured, never below 0 (circuits are not read at the same moment as the total)</summary>
        public double Rest { get; init; }
        public IReadOnlyList<int> Circuits { get; init; } = Array.Empty<int>();
    }

    public sealed class EnergyCircuitBreakdown
    {
        /// <summary>power of each circuit in kW, 0 without a value and never negative</summary>
        public double[] Power { get; init; } = new double[EnergyCircuitInputs.CircuitCount];
        /// <summary>the groups that have at least one used circuit, by group number</summary>
        public IReadOnlyDictionary<int, EnergyCircuitGroup> Groups { get; init; } = new Dictionary<int, EnergyCircuitGroup>();

        /// <summary>rest power of the group, null when the group has no circuits</summary>
        public double? Rest(int group) => Groups.TryGetValue(group, out var g) ? g.Rest : null;
    }

    public static class EnergyCircuitCalculator
    {
        public static EnergyCircuitBreakdown Calculate(EnergyCircuitInputs inputs, EnergyFlowSnapshot snapshot)
        {
            var power = new double[EnergyCircuitInputs.CircuitCount];
            for (var i = 0; i < power.Length; i++)
            {
                var p = inputs.Power[i];
                power[i] = p.HasValue && double.IsFinite(p.Value) ? Math.Max(p.Value, 0) : 0;
            }

            var groups = Enumerable.Range(0, EnergyCircuitInputs.CircuitCount)
                .Where(inputs.IsUsed)
                .GroupBy(i => inputs.Group[i])
                .ToDictionary(g => g.Key, g =>
                {
                    var total = GroupTotal(snapshot, g.Key);
                    var measured = g.Sum(i => power[i]);
                    return new EnergyCircuitGroup
                    {
                        Group = g.Key,
                        Total = total,
                        Measured = measured,
                        Rest = Math.Max(total - measured, 0),
                        Circuits = g.ToList()
                    };
                });

            return new EnergyCircuitBreakdown { Power = power, Groups = groups };
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
