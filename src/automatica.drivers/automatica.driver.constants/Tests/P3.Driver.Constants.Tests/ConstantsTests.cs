using System;
using System.Linq;
using Automatica.Core.UnitTests.Base.Drivers;
using Xunit;

namespace P3.Driver.Constants.Tests
{
    public class ConstantsTests : DriverFactoryTestBase<ConstantsDriverFactory>
    {
        [Fact]
        public void Test_ConstantsValue()
        {
            var constantsRoot = CreateNodeInstance(ConstantsDriverFactory.BusId);
            var valueId = CreateNodeInstance(ConstantsDriverFactory.ValueId);

            var prop = valueId.PropertyInstance.SingleOrDefault(a => a.This2PropertyTemplate == ConstantsDriverFactory.PropertyValueId);
            Assert.NotNull(prop);
            prop.Value = 100;

            constantsRoot.InverseThis2ParentNodeInstanceNavigation.Add(valueId);

            var driver = CreateDriver(constantsRoot);

            Assert.True(driver.Children.Count == 1);
            Assert.IsType<Constant>(driver.Children[0]);

            var con = driver.Children[0] as Constant;

            Assert.NotNull(con);
            Assert.Equal(100d, con.Value);
        }

        [Fact]
        public void Test_ConstantPi()
        {
            var constantsRoot = CreateNodeInstance(ConstantsDriverFactory.BusId);
            var valueId = CreateNodeInstance(ConstantsDriverFactory.PiId);

            constantsRoot.InverseThis2ParentNodeInstanceNavigation.Add(valueId);

            var driver = CreateDriver(constantsRoot);

            Assert.True(driver.Children.Count == 1);
            Assert.IsType<Constant>(driver.Children[0]);

            var con = driver.Children[0] as Constant;

            Assert.NotNull(con);
            Assert.Equal(Math.PI, con.Value);
        }

        [Fact]
        public void Test_ConstantHalfPi()
        {
            var constantsRoot = CreateNodeInstance(ConstantsDriverFactory.BusId);
            var valueId = CreateNodeInstance(ConstantsDriverFactory.HalfPiId);

            constantsRoot.InverseThis2ParentNodeInstanceNavigation.Add(valueId);

            var driver = CreateDriver(constantsRoot);

            Assert.True(driver.Children.Count == 1);
            Assert.IsType<Constant>(driver.Children[0]);

            var con = driver.Children[0] as Constant;

            Assert.NotNull(con);
            Assert.Equal(Math.PI/2, con.Value);
        }

        [Fact]
        public void Test_ConstantDoublePi()
        {
            var constantsRoot = CreateNodeInstance(ConstantsDriverFactory.BusId);
            var valueId = CreateNodeInstance(ConstantsDriverFactory.DoublePiId);

            constantsRoot.InverseThis2ParentNodeInstanceNavigation.Add(valueId);

            var driver = CreateDriver(constantsRoot);

            Assert.True(driver.Children.Count == 1);
            Assert.IsType<Constant>(driver.Children[0]);

            var con = driver.Children[0] as Constant;

            Assert.NotNull(con);
            Assert.Equal(Math.PI  * 2, con.Value);
        }

        [Fact]
        public void Test_ConstantString()
        {
            var constantsRoot = CreateNodeInstance(ConstantsDriverFactory.BusId);
            var valueId = CreateNodeInstance(ConstantsDriverFactory.StringValueId);

            var prop = valueId.PropertyInstance.SingleOrDefault(a => a.This2PropertyTemplate == ConstantsDriverFactory.StringPropertyValueId);
            Assert.NotNull(prop);
            prop.Value = "test123";

            constantsRoot.InverseThis2ParentNodeInstanceNavigation.Add(valueId);

            var driver = CreateDriver(constantsRoot);

            Assert.True(driver.Children.Count == 1);
            Assert.IsType<Constant>(driver.Children[0]);

            var con = driver.Children[0] as Constant;

            Assert.NotNull(con);
            Assert.Equal("test123", con.Value);
        }



        [Fact]
        public void Test_ConstantTrue()
        {
            var constantsRoot = CreateNodeInstance(ConstantsDriverFactory.BusId);
            var valueId = CreateNodeInstance(ConstantsDriverFactory.TrueId);

            constantsRoot.InverseThis2ParentNodeInstanceNavigation.Add(valueId);

            var driver = CreateDriver(constantsRoot);

            Assert.True(driver.Children.Count == 1);
            Assert.IsType<Constant>(driver.Children[0]);

            var con = driver.Children[0] as Constant;

            Assert.NotNull(con);
            Assert.True((bool)con.Value);
        }

        [Fact]
        public void Test_ConstantFalse()
        {
            var constantsRoot = CreateNodeInstance(ConstantsDriverFactory.BusId);
            var valueId = CreateNodeInstance(ConstantsDriverFactory.FalseId);

            constantsRoot.InverseThis2ParentNodeInstanceNavigation.Add(valueId);

            var driver = CreateDriver(constantsRoot);

            Assert.True(driver.Children.Count == 1);
            Assert.IsType<Constant>(driver.Children[0]);

            var con = driver.Children[0] as Constant;

            Assert.NotNull(con);
            Assert.False((bool)con.Value);
        }
    }
}
