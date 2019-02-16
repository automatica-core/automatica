
            using System;
            using System.Collections.Generic;
            using System.Text;
            using Automatica.Core.UnitTests.Drivers;
            using Xunit;

            namespace P3.Driver.EnOcean.DriverFactory.Tests
            {
                public class DriverFactoryRorgD5Tests : DriverFactoryTestBase<EnOceanDriverFactory>
                {
                    
// AUTO GENERATED 20.07.2018 19:29:39
//-------------------------
//-------------------------
// 0xD5 -------------------
//-------------------------
//-------------------------


        [Fact]
        public void Test_DataFieldD5_00_01_1_CO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD5Data.DataFieldD5_00_01_1_CO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(7, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD5_00_01_1_LRN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD5Data.DataFieldD5_00_01_1_LRN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(1, propSize);
        }

                }
            }
            