using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Operations.Climate.AirConditioning;
using P3.Logic.Operations.Climate.Common;
using P3.Logic.Operations.Climate.FanCoil;
using P3.Logic.Operations.Climate.HeatingCoolingController;
using P3.Logic.Operations.Climate.Hvac;
using P3.Logic.Operations.Climate.MixingValve;
using P3.Logic.Operations.Climate.RoomController;
using Xunit;

namespace P3.Logic.Operations.Tests.Climate
{
    /// <summary>
    /// Forwards every template call to the mock and records the default visu tiles
    /// </summary>
    public class RecordingTemplateFactory : DispatchProxy
    {
        public ILogicTemplateFactory Target { get; set; }
        public List<(Guid Logic, int Tile)> Tiles { get; } = new();

        public static (ILogicTemplateFactory Factory, RecordingTemplateFactory Recorder) Create()
        {
            var proxy = Create<ILogicTemplateFactory, RecordingTemplateFactory>();
            var recorder = (RecordingTemplateFactory)(object)proxy;
            recorder.Target = new LogicTemplateFactoryMock();
            return (proxy, recorder);
        }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (targetMethod!.Name == nameof(ILogicTemplateFactory.ChangeDefaultVisuTemplate))
            {
                Tiles.Add(((Guid)args![0], Convert.ToInt32(args[1])));
            }
            return targetMethod.Invoke(Target, args);
        }
    }

    [Collection("ClimateTiles")]
    public class ClimateTileTests
    {
        public static IEnumerable<object[]> Expected => new[]
        {
            new object[] { typeof(RoomControllerLogicFactory), 17 },
            new object[] { typeof(AcUnitLogicFactory), 27 },
            new object[] { typeof(FanCoilUnitLogicFactory), 27 },
            new object[] { typeof(FanCoilFreshAirLogicFactory), 27 },
            new object[] { typeof(HeatingCoolingControllerLogicFactory), 28 },
            new object[] { typeof(HvacControllerLogicFactory), 28 },
            new object[] { typeof(AcCentralLogicFactory), 28 },
            new object[] { typeof(FanCoilCentralLogicFactory), 28 },
            new object[] { typeof(MixingValveLogicFactory), 28 },
        };

        private static List<(Guid Logic, int Tile)> Init(Type factoryType)
        {
            var (factory, recorder) = RecordingTemplateFactory.Create();
            var logic = (LogicFactory)Activator.CreateInstance(factoryType)!;
            logic.InitTemplates(factory);
            Assert.All(recorder.Tiles, t => Assert.Equal(logic.LogicGuid, t.Logic));
            return recorder.Tiles;
        }

        [Theory]
        [MemberData(nameof(Expected))]
        public void Block_SetsItsTile_WhenTheCoreKnowsIt(Type factory, int tile)
        {
            var before = ClimateTemplateBuilder.TileKnown;
            ClimateTemplateBuilder.TileKnown = _ => true;
            try
            {
                Assert.Equal(new[] { tile }, Init(factory).Select(a => a.Tile));
            }
            finally
            {
                ClimateTemplateBuilder.TileKnown = before;
            }
        }

        [Fact]
        public void OlderCore_KeepsTheDefaultTile()
        {
            var before = ClimateTemplateBuilder.TileKnown;
            ClimateTemplateBuilder.TileKnown = _ => false;
            try
            {
                Assert.Empty(Init(typeof(AcUnitLogicFactory)));
                Assert.Empty(Init(typeof(RoomControllerLogicFactory)));
            }
            finally
            {
                ClimateTemplateBuilder.TileKnown = before;
            }
        }

        [Fact]
        public void TileKnown_ComparesTheEnumName()
        {
            // Dimmer = 12 exists in every core, 12 is not called RoomController
            Assert.True(ClimateTemplateBuilder.TileKnown(new ClimateTile((int)VisuMobileObjectTemplateTypes.Dimmer, "Dimmer")));
            Assert.False(ClimateTemplateBuilder.TileKnown(new ClimateTile((int)VisuMobileObjectTemplateTypes.Dimmer, "RoomController")));
            Assert.False(ClimateTemplateBuilder.TileKnown(new ClimateTile(999, "Nothing")));
        }

        [Fact]
        public void SimpleBlocks_KeepTheDefaultTile()
        {
            var withTile = Expected.Select(a => (Type)a[0]).ToHashSet();
            var others = typeof(ClimateLogic).Assembly.GetTypes()
                .Where(t => typeof(LogicFactory).IsAssignableFrom(t) && !t.IsAbstract && t.Namespace!.StartsWith("P3.Logic.Operations.Climate") && !withTile.Contains(t))
                .ToList();
            Assert.Equal(5, others.Count);
            var before = ClimateTemplateBuilder.TileKnown;
            ClimateTemplateBuilder.TileKnown = _ => true;
            try
            {
                Assert.All(others, t => Assert.Empty(Init(t)));
            }
            finally
            {
                ClimateTemplateBuilder.TileKnown = before;
            }
        }
    }
}
