using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Operations.Climate.Common
{
    /// <summary>
    /// Creates the templates of a climate block with derived ids and i18n keys:
    /// block name/description CLIMATE.{BLOCK}.NAME / .DESCRIPTION,
    /// inputs and outputs keep their short (Loxone style) name and get the description CLIMATE.{BLOCK}.IN|OUT.{KEY},
    /// parameters CLIMATE.{BLOCK}.PARAM.{KEY}.NAME / .DESCRIPTION, enum entries CLIMATE.{BLOCK}.PARAM.{KEY}.{VALUE}.
    /// </summary>
    /// <summary>
    /// Visu tile of a climate block: value and name of VisuMobileObjectTemplateTypes in the core that ships it
    /// </summary>
    public readonly record struct ClimateTile(int Value, string Name)
    {
        public static readonly ClimateTile RoomController = new(17, "RoomController");
        public static readonly ClimateTile Unit = new(27, "ClimateUnit");
        public static readonly ClimateTile Central = new(28, "ClimateCentral");
    }

    public sealed class ClimateTemplateBuilder
    {
        public const string Group = "CLIMATE.NAME";

        private readonly ILogicTemplateFactory _factory;
        private readonly Guid _logic;
        private readonly string _block;

        public ClimateTemplateBuilder(ILogicTemplateFactory factory, Guid logic, string block, string key, double height = 100, double width = 100)
        {
            _factory = factory;
            _logic = logic;
            _block = block;
            factory.CreateLogicTemplate(logic, $"CLIMATE.{block}.NAME", $"CLIMATE.{block}.DESCRIPTION", key, Group, height, width);
        }

        /// <summary>
        /// The plugin builds against an older core NuGet without the climate tiles: a tile is only set when the running core knows it
        /// </summary>
        internal static Func<ClimateTile, bool> TileKnown = tile => System.Enum.GetName(typeof(VisuMobileObjectTemplateTypes), tile.Value) == tile.Name;

        /// <summary>
        /// Sets the default visu tile of the block, older cores keep the default logic tile
        /// </summary>
        public bool VisuTile(ClimateTile tile)
        {
            if (!TileKnown(tile))
            {
                return false;
            }
            _factory.ChangeDefaultVisuTemplate(_logic, (VisuMobileObjectTemplateTypes)tile.Value);
            return true;
        }

        public static Guid InputId(Guid logic, string key) => ClimateIds.Of(logic, "in." + key);
        public static Guid OutputId(Guid logic, string key) => ClimateIds.Of(logic, "out." + key);
        public static Guid ParamId(Guid logic, string key) => ClimateIds.Of(logic, "param." + key);

        public void Input(string key, string name, int sort)
        {
            _factory.CreateLogicInterfaceTemplate(InputId(_logic, key), name, $"CLIMATE.{_block}.IN.{key.ToUpperInvariant()}", key,
                _logic, LogicInterfaceDirection.Input, 1, sort);
        }

        public void Output(string key, string name, int sort)
        {
            _factory.CreateLogicInterfaceTemplate(OutputId(_logic, key), name, $"CLIMATE.{_block}.OUT.{key.ToUpperInvariant()}", key,
                _logic, LogicInterfaceDirection.Output, 0, sort, RuleInterfaceType.Status);
        }

        public void Number(string key, int sort, double defaultValue, bool linkable = false)
        {
            Param(key, sort, RuleInterfaceParameterDataType.Double, defaultValue, linkable, null);
        }

        public void Integer(string key, int sort, int defaultValue)
        {
            Param(key, sort, RuleInterfaceParameterDataType.Integer, defaultValue, false, null);
        }

        public void Bool(string key, int sort, bool defaultValue)
        {
            Param(key, sort, RuleInterfaceParameterDataType.Bool, defaultValue, false, null);
        }

        /// <summary>
        /// An enum parameter, the values are the numbers the logic reads, each one gets the text CLIMATE.{BLOCK}.PARAM.{KEY}.{name}
        /// </summary>
        public void Enum(string key, int sort, int defaultValue, params (int Value, string Name)[] values)
        {
            var prefix = $"CLIMATE.{_block}.PARAM.{key.ToUpperInvariant()}";
            var meta = "ENUM(" + string.Join(";", values.Select(a => $"{a.Value.ToString(CultureInfo.InvariantCulture)},{prefix}.{a.Name}")) + ")";
            Param(key, sort, RuleInterfaceParameterDataType.Enum, defaultValue.ToString(CultureInfo.InvariantCulture), false, meta);
        }

        private void Param(string key, int sort, RuleInterfaceParameterDataType type, object defaultValue, bool linkable, string meta)
        {
            var prefix = $"CLIMATE.{_block}.PARAM.{key.ToUpperInvariant()}";
            _factory.CreateParameterLogicInterfaceTemplate(ParamId(_logic, key), $"{prefix}.NAME", $"{prefix}.DESCRIPTION", key,
                _logic, sort, type, defaultValue, linkable, meta);
        }

        /// <summary>
        /// Interface keys used for numbered inputs (d1..d8)
        /// </summary>
        public static IReadOnlyList<string> Numbered(string prefix, int count) =>
            Enumerable.Range(1, count).Select(i => prefix + i.ToString(CultureInfo.InvariantCulture)).ToArray();
    }
}
