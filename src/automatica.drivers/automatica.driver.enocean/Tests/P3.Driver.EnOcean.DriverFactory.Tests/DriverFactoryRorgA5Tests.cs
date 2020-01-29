
            using System;
            using System.Collections.Generic;
            using System.Text;
            using Automatica.Core.UnitTests.Base.Drivers;
            using Automatica.Core.UnitTests.Drivers;
            using Xunit;

            namespace P3.Driver.EnOcean.DriverFactory.Tests
            {
                public class DriverFactoryRorgA5Tests : DriverFactoryTestBase<EnOceanDriverFactory>
                {
                    
// AUTO GENERATED 20.07.2018 19:29:39
//-------------------------
//-------------------------
// 0xA5 -------------------
//-------------------------
//-------------------------


        [Fact]
        public void Test_DataFieldA5_02_01_1_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_01_1_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_01_1_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_01_1_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_01_1_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_01_1_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_01_1_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_01_1_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-40, scaleMin);
            Assert.Equal(0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_02_2_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_02_2_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_02_2_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_02_2_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_02_2_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_02_2_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_02_2_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_02_2_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-30, scaleMin);
            Assert.Equal(10, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_03_3_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_03_3_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_03_3_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_03_3_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_03_3_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_03_3_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_03_3_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_03_3_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-20, scaleMin);
            Assert.Equal(20, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_04_4_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_04_4_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_04_4_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_04_4_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_04_4_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_04_4_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_04_4_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_04_4_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-10, scaleMin);
            Assert.Equal(30, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_05_5_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_05_5_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_05_5_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_05_5_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_05_5_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_05_5_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_05_5_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_05_5_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_06_6_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_06_6_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_06_6_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_06_6_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_06_6_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_06_6_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_06_6_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_06_6_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(10, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_07_7_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_07_7_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_07_7_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_07_7_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_07_7_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_07_7_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_07_7_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_07_7_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(20, scaleMin);
            Assert.Equal(60, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_08_8_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_08_8_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_08_8_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_08_8_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_08_8_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_08_8_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_08_8_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_08_8_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(30, scaleMin);
            Assert.Equal(70, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_09_9_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_09_9_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_09_9_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_09_9_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_09_9_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_09_9_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_09_9_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_09_9_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(40, scaleMin);
            Assert.Equal(80, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_0A_10_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_0A_10_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_0A_10_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_0A_10_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_0A_10_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_0A_10_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_0A_10_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_0A_10_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(50, scaleMin);
            Assert.Equal(90, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_0B_11_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_0B_11_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_0B_11_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_0B_11_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_0B_11_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_0B_11_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_0B_11_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_0B_11_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(60, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_10_12_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_10_12_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_10_12_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_10_12_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_10_12_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_10_12_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_10_12_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_10_12_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-60, scaleMin);
            Assert.Equal(20, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_11_13_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_11_13_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_11_13_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_11_13_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_11_13_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_11_13_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_11_13_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_11_13_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-50, scaleMin);
            Assert.Equal(30, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_12_14_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_12_14_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_12_14_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_12_14_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_12_14_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_12_14_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_12_14_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_12_14_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-40, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_13_15_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_13_15_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_13_15_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_13_15_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_13_15_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_13_15_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_13_15_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_13_15_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-30, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_14_16_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_14_16_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_14_16_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_14_16_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_14_16_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_14_16_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_14_16_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_14_16_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-20, scaleMin);
            Assert.Equal(60, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_15_17_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_15_17_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_15_17_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_15_17_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_15_17_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_15_17_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_15_17_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_15_17_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-10, scaleMin);
            Assert.Equal(70, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_16_18_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_16_18_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_16_18_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_16_18_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_16_18_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_16_18_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_16_18_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_16_18_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(80, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_17_19_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_17_19_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_17_19_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_17_19_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_17_19_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_17_19_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_17_19_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_17_19_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(10, scaleMin);
            Assert.Equal(90, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_18_20_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_18_20_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_18_20_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_18_20_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_18_20_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_18_20_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_18_20_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_18_20_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(20, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_19_21_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_19_21_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_19_21_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_19_21_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_19_21_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_19_21_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_19_21_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_19_21_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(30, scaleMin);
            Assert.Equal(110, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_1A_22_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_1A_22_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_1A_22_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_1A_22_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_1A_22_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_1A_22_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_1A_22_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_1A_22_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(40, scaleMin);
            Assert.Equal(120, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_1B_23_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_1B_23_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_1B_23_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_1B_23_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_1B_23_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_1B_23_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_1B_23_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_1B_23_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(50, scaleMin);
            Assert.Equal(130, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_20_24_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_20_24_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_20_24_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_20_24_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(14, propOffset);
            Assert.Equal(10, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_20_24_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_20_24_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1023, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_20_24_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_20_24_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-10, scaleMin);
            Assert.Equal(41.2, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_30_25_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_30_25_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_30_25_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_30_25_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(14, propOffset);
            Assert.Equal(10, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_02_30_25_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_30_25_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1023, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_02_30_25_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_02_30_25_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-40, scaleMin);
            Assert.Equal(62.3, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_04_01_1_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_01_1_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_04_01_1_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_01_1_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_04_01_1_HUM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_01_1_HUM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_04_01_1_HUM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_01_1_HUM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_04_01_1_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_01_1_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_04_01_1_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_01_1_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_04_01_1_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_01_1_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_04_01_1_TSN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_01_1_TSN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_04_02_2_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_02_2_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_04_02_2_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_02_2_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_04_02_2_HUM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_02_2_HUM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_04_02_2_HUM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_02_2_HUM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_04_02_2_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_02_2_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_04_02_2_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_02_2_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_04_02_2_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_02_2_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-20, scaleMin);
            Assert.Equal(60, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_04_02_2_TSN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_02_2_TSN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_04_03_3_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_03_3_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_04_03_3_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_03_3_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_04_03_3_HUM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_03_3_HUM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_04_03_3_HUM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_03_3_HUM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_04_03_3_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_03_3_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(14, propOffset);
            Assert.Equal(10, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_04_03_3_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_03_3_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(1023, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_04_03_3_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_03_3_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-20, scaleMin);
            Assert.Equal(60, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_04_03_3_TTP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_04_03_3_TTP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_05_01_1_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_05_01_1_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_05_01_1_BAR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_05_01_1_BAR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(10, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_05_01_1_BAR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_05_01_1_BAR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(1023, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_05_01_1_BAR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_05_01_1_BAR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(500, scaleMin);
            Assert.Equal(1150, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_05_01_1_TTP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_05_01_1_TTP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_01_1_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_01_1_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_01_1_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_01_1_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_01_1_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_01_1_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_01_1_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_01_1_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.1, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_01_1_ILL2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_01_1_ILL2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_01_1_ILL2_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_01_1_ILL2_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_01_1_ILL2_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_01_1_ILL2_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(300, scaleMin);
            Assert.Equal(30000, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_01_1_ILL1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_01_1_ILL1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_01_1_ILL1_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_01_1_ILL1_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_01_1_ILL1_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_01_1_ILL1_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(600, scaleMin);
            Assert.Equal(60000, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_01_1_RS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_01_1_RS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_02_2_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_02_2_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_02_2_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_02_2_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_02_2_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_02_2_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_02_2_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_02_2_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.1, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_02_2_ILL2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_02_2_ILL2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_02_2_ILL2_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_02_2_ILL2_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_02_2_ILL2_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_02_2_ILL2_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(510, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_02_2_ILL1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_02_2_ILL1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_02_2_ILL1_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_02_2_ILL1_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_02_2_ILL1_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_02_2_ILL1_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(1020, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_02_2_RS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_02_2_RS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_03_3_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_03_3_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_03_3_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_03_3_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_03_3_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_03_3_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_03_3_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_03_3_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_03_3_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_03_3_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(10, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_03_3_ILL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_03_3_ILL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(1000, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_03_3_ILL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_03_3_ILL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(1000, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_04_4_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_04_4_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_04_4_TEMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_04_4_TEMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_04_4_TEMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_04_4_TEMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_04_4_TEMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_04_4_TEMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-20, scaleMin);
            Assert.Equal(60, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_04_4_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_04_4_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_04_4_ILL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_04_4_ILL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(65535, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_04_4_ILL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_04_4_ILL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(65535, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_04_4_SV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_04_4_SV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_04_4_SV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_04_4_SV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(15, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_04_4_SV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_04_4_SV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_04_4_TMPAV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_04_4_TMPAV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_04_4_ENAV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_04_4_ENAV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_05_5_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_05_5_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_05_5_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_05_5_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_05_5_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_05_5_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_05_5_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_05_5_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.1, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_05_5_ILL2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_05_5_ILL2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_05_5_ILL2_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_05_5_ILL2_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_05_5_ILL2_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_05_5_ILL2_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_05_5_ILL1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_05_5_ILL1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_06_05_5_ILL1_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_05_5_ILL1_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_05_5_ILL1_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_05_5_ILL1_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(10200, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_06_05_5_RS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_06_05_5_RS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_07_01_1_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_07_01_1_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_07_01_1_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_07_01_1_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_07_01_1_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_07_01_1_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_07_01_1_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_07_01_1_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_07_01_1_PIRS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_07_01_1_PIRS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_07_01_1_SVA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_07_01_1_SVA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_07_02_2_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_07_02_2_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_07_02_2_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_07_02_2_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_07_02_2_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_07_02_2_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_07_02_2_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_07_02_2_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_07_02_2_PIRS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_07_02_2_PIRS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_07_03_3_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_07_03_3_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_07_03_3_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_07_03_3_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_07_03_3_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_07_03_3_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_07_03_3_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_07_03_3_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_07_03_3_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_07_03_3_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(10, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_07_03_3_ILL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_07_03_3_ILL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(1000, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_07_03_3_ILL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_07_03_3_ILL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(1000, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_07_03_3_PIRS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_07_03_3_PIRS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_08_01_1_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_01_1_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_08_01_1_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_01_1_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_08_01_1_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_01_1_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_08_01_1_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_01_1_SVC_Guid);

            var scaleMinStr = dataField.GetPropertyValue("enocean-scale-min", "0");
            var scaleMaxStr = dataField.GetPropertyValue("enocean-scale-max", "0");

            Console.WriteLine($"min {scaleMinStr} max {scaleMaxStr}");

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.1, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_08_01_1_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_01_1_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_08_01_1_ILL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_01_1_ILL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_08_01_1_ILL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_01_1_ILL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(510, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_08_01_1_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_01_1_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_08_01_1_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_01_1_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_08_01_1_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_01_1_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(51, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_08_01_1_PIRS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_01_1_PIRS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_08_01_1_OCC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_01_1_OCC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_08_02_2_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_02_2_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_08_02_2_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_02_2_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_08_02_2_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_02_2_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_08_02_2_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_02_2_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.1, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_08_02_2_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_02_2_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_08_02_2_ILL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_02_2_ILL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_08_02_2_ILL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_02_2_ILL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(1020, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_08_02_2_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_02_2_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_08_02_2_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_02_2_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_08_02_2_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_02_2_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(51, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_08_02_2_PIRS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_02_2_PIRS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_08_02_2_OCC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_02_2_OCC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_08_03_3_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_03_3_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_08_03_3_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_03_3_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_08_03_3_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_03_3_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_08_03_3_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_03_3_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.1, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_08_03_3_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_03_3_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_08_03_3_ILL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_03_3_ILL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_08_03_3_ILL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_03_3_ILL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(1530, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_08_03_3_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_03_3_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_08_03_3_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_03_3_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_08_03_3_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_03_3_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-30, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_08_03_3_PIRS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_03_3_PIRS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_08_03_3_OCC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_08_03_3_OCC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_02_1_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_02_1_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_02_1_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_02_1_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_02_1_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_02_1_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_02_1_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_02_1_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.1, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_02_1_Conc_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_02_1_Conc_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_02_1_Conc_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_02_1_Conc_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_02_1_Conc_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_02_1_Conc_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(1020, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_02_1_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_02_1_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_02_1_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_02_1_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_02_1_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_02_1_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(51, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_02_1_TSN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_02_1_TSN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_04_2_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_04_2_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_04_2_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_04_2_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_04_2_HUM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_04_2_HUM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(200, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_04_2_HUM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_04_2_HUM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_04_2_Conc_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_04_2_Conc_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_04_2_Conc_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_04_2_Conc_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_04_2_Conc_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_04_2_Conc_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(2550, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_04_2_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_04_2_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_04_2_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_04_2_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_04_2_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_04_2_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(51.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_04_2_HSN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_04_2_HSN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_04_2_TSN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_04_2_TSN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_05_3_Conc_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_05_3_Conc_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_05_3_Conc_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_05_3_Conc_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(65535, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_05_3_Conc_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_05_3_Conc_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(65535, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_05_3_VOC_ID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_05_3_VOC_ID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_05_3_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_05_3_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_05_3_SCM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_05_3_SCM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_06_4_Act_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_06_4_Act_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(10, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_06_4_Act_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_06_4_Act_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(1023, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_06_4_Act_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_06_4_Act_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(1023, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_06_4_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_06_4_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_07_5_PM10_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_07_5_PM10_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_07_5_PM10_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_07_5_PM10_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(511, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_07_5_PM10_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_07_5_PM10_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(511, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_07_5_PM2_5_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_07_5_PM2_5_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_07_5_PM2_5_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_07_5_PM2_5_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(511, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_07_5_PM2_5_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_07_5_PM2_5_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(511, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_07_5_PM1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_07_5_PM1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(18, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_07_5_PM1_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_07_5_PM1_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(511, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_07_5_PM1_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_07_5_PM1_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(511, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_07_5_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_07_5_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_07_5_PM10a_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_07_5_PM10a_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_07_5_PM2_5a_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_07_5_PM2_5a_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_07_5_PM1a_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_07_5_PM1a_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_08_6_CO2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_08_6_CO2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_08_6_CO2_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_08_6_CO2_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_08_6_CO2_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_08_6_CO2_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(2000, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_08_6_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_08_6_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_09_7_CO2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_09_7_CO2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_09_7_CO2_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_09_7_CO2_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_09_7_CO2_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_09_7_CO2_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(2000, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_09_7_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_09_7_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_09_7_PFD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_09_7_PFD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_0A_8_Conc_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0A_8_Conc_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_0A_8_Conc_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0A_8_Conc_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(65535, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_0A_8_Conc_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0A_8_Conc_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(65535, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_0A_8_TEMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0A_8_TEMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_0A_8_TEMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0A_8_TEMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_0A_8_TEMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0A_8_TEMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-20, scaleMin);
            Assert.Equal(60, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_0A_8_SV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0A_8_SV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_0A_8_SV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0A_8_SV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(15, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_0A_8_SV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0A_8_SV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(2.0, scaleMin);
            Assert.Equal(5.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_0A_8_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0A_8_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_0A_8_TSA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0A_8_TSA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_0A_8_SVA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0A_8_SVA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_0B_9_SV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0B_9_SV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_0B_9_SV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0B_9_SV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(15, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_0B_9_SV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0B_9_SV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(2.0, scaleMin);
            Assert.Equal(5.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_0B_9_Ract_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0B_9_Ract_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_0B_9_Ract_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0B_9_Ract_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(65535, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_0B_9_Ract_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0B_9_Ract_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(6553, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_0B_9_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0B_9_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_0B_9_SCM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0B_9_SCM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_0B_9_VUNIT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0B_9_VUNIT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_0B_9_SVA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0B_9_SVA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_0C_10_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0C_10_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_0C_10_Conc_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0C_10_Conc_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_0C_10_Conc_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0C_10_Conc_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(65535, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_0C_10_Conc_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0C_10_Conc_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(65535, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_09_0C_10_VOCID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0C_10_VOCID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_0C_10_Unit_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0C_10_Unit_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_09_0C_10_SCM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_09_0C_10_SCM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_01_1_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_01_1_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_01_1_FAN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_01_1_FAN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_01_1_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_01_1_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_01_1_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_01_1_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_01_1_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_01_1_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_01_1_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_01_1_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_01_1_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_01_1_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_01_1_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_01_1_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_01_1_OCC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_01_1_OCC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_02_2_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_02_2_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_02_2_FAN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_02_2_FAN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_02_2_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_02_2_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_02_2_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_02_2_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_02_2_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_02_2_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_02_2_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_02_2_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_02_2_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_02_2_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_02_2_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_02_2_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_02_2_SLSW_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_02_2_SLSW_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_03_3_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_03_3_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_03_3_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_03_3_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_03_3_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_03_3_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_03_3_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_03_3_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_03_3_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_03_3_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_03_3_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_03_3_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_03_3_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_03_3_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_04_4_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_04_4_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_04_4_FAN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_04_4_FAN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_04_4_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_04_4_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_04_4_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_04_4_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_04_4_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_04_4_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_04_4_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_04_4_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_04_4_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_04_4_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_04_4_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_04_4_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_05_5_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_05_5_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_05_5_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_05_5_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_05_5_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_05_5_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_05_5_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_05_5_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_05_5_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_05_5_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_05_5_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_05_5_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_05_5_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_05_5_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_05_5_OCC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_05_5_OCC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_06_6_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_06_6_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_06_6_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_06_6_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_06_6_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_06_6_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_06_6_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_06_6_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_06_6_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_06_6_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_06_6_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_06_6_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_06_6_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_06_6_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_06_6_SLSW_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_06_6_SLSW_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_07_7_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_07_7_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_07_7_FAN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_07_7_FAN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_07_7_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_07_7_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_07_7_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_07_7_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_07_7_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_07_7_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_08_8_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_08_8_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_08_8_FAN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_08_8_FAN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_08_8_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_08_8_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_08_8_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_08_8_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_08_8_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_08_8_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_08_8_OCC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_08_8_OCC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_09_9_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_09_9_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_09_9_FAN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_09_9_FAN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_09_9_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_09_9_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_09_9_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_09_9_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_09_9_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_09_9_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_09_9_SLSW_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_09_9_SLSW_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_0A_10_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0A_10_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_0A_10_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0A_10_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_0A_10_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0A_10_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_0A_10_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0A_10_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_0A_10_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0A_10_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_0A_10_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0A_10_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_0A_10_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0A_10_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_0A_10_CTST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0A_10_CTST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_0B_11_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0B_11_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_0B_11_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0B_11_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_0B_11_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0B_11_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_0B_11_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0B_11_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_0B_11_CTST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0B_11_CTST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_0C_12_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0C_12_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_0C_12_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0C_12_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_0C_12_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0C_12_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_0C_12_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0C_12_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_0C_12_OCC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0C_12_OCC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_0D_13_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0D_13_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_0D_13_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0D_13_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_0D_13_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0D_13_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_0D_13_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0D_13_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_0D_13_SLSW_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_0D_13_SLSW_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_10_14_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_10_14_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_10_14_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_10_14_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_10_14_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_10_14_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_10_14_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_10_14_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_10_14_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_10_14_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_10_14_HUM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_10_14_HUM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_10_14_HUM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_10_14_HUM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_10_14_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_10_14_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_10_14_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_10_14_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_10_14_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_10_14_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_10_14_OCC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_10_14_OCC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_11_15_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_11_15_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_11_15_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_11_15_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_11_15_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_11_15_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_11_15_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_11_15_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_11_15_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_11_15_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_11_15_HUM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_11_15_HUM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_11_15_HUM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_11_15_HUM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_11_15_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_11_15_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_11_15_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_11_15_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_11_15_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_11_15_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_11_15_SLSW_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_11_15_SLSW_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_12_16_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_12_16_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_12_16_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_12_16_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_12_16_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_12_16_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_12_16_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_12_16_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_12_16_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_12_16_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_12_16_HUM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_12_16_HUM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_12_16_HUM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_12_16_HUM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_12_16_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_12_16_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_12_16_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_12_16_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_12_16_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_12_16_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_13_17_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_13_17_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_13_17_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_13_17_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_13_17_HUM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_13_17_HUM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_13_17_HUM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_13_17_HUM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_13_17_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_13_17_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_13_17_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_13_17_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_13_17_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_13_17_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_13_17_OCC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_13_17_OCC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_14_18_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_14_18_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_14_18_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_14_18_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_14_18_HUM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_14_18_HUM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_14_18_HUM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_14_18_HUM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_14_18_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_14_18_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_14_18_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_14_18_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_14_18_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_14_18_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_14_18_SLSW_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_14_18_SLSW_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_15_19_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_15_19_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_15_19_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_15_19_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(14, propOffset);
            Assert.Equal(10, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_15_19_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_15_19_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1023, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_15_19_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_15_19_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-10, scaleMin);
            Assert.Equal(41.2, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_15_19_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_15_19_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_15_19_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_15_19_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(63, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_15_19_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_15_19_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(63, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_16_20_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_16_20_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_16_20_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_16_20_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(14, propOffset);
            Assert.Equal(10, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_16_20_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_16_20_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1023, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_16_20_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_16_20_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-10, scaleMin);
            Assert.Equal(41.2, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_16_20_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_16_20_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_16_20_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_16_20_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(63, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_16_20_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_16_20_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(63, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_16_20_OCC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_16_20_OCC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_17_21_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_17_21_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_17_21_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_17_21_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(14, propOffset);
            Assert.Equal(10, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_17_21_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_17_21_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1023, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_17_21_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_17_21_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-10, scaleMin);
            Assert.Equal(41.2, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_17_21_OCC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_17_21_OCC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_18_22_FAN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_18_22_FAN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_18_22_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_18_22_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_18_22_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_18_22_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_18_22_ILL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_18_22_ILL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_18_22_ILL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_18_22_ILL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(1000, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_18_22_TMPSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_18_22_TMPSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_18_22_TMPSP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_18_22_TMPSP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(250, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_18_22_TMPSP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_18_22_TMPSP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_18_22_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_18_22_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_18_22_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_18_22_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(250, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_18_22_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_18_22_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_18_22_OED_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_18_22_OED_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_18_22_OB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_18_22_OB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_19_23_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_19_23_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_19_23_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_19_23_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_19_23_HUM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_19_23_HUM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_19_23_HUM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_19_23_HUM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_19_23_TMPSp_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_19_23_TMPSp_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_19_23_TMPSp_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_19_23_TMPSp_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(250, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_19_23_TMPSp_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_19_23_TMPSp_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_19_23_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_19_23_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_19_23_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_19_23_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(250, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_19_23_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_19_23_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_19_23_FAN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_19_23_FAN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_19_23_OED_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_19_23_OED_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_19_23_OB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_19_23_OB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1A_24_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1A_24_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1A_24_SV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1A_24_SV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1A_24_SV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1A_24_SV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1A_24_SV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1A_24_SV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1A_24_TMPSp_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1A_24_TMPSp_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1A_24_TMPSp_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1A_24_TMPSp_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(250, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1A_24_TMPSp_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1A_24_TMPSp_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1A_24_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1A_24_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1A_24_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1A_24_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(250, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1A_24_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1A_24_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1A_24_FAN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1A_24_FAN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1A_24_OED_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1A_24_OED_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1A_24_OB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1A_24_OB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1B_25_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1B_25_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1B_25_SV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1B_25_SV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1B_25_SV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1B_25_SV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1B_25_SV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1B_25_SV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1B_25_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1B_25_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1B_25_ILL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1B_25_ILL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1B_25_ILL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1B_25_ILL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(1000, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1B_25_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1B_25_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1B_25_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1B_25_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(250, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1B_25_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1B_25_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1B_25_FAN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1B_25_FAN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1B_25_OED_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1B_25_OED_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1B_25_OB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1B_25_OB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1C_26_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1C_26_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1C_26_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1C_26_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1C_26_ILL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1C_26_ILL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1C_26_ILL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1C_26_ILL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(1000, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1C_26_ILLSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1C_26_ILLSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1C_26_ILLSP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1C_26_ILLSP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1C_26_ILLSP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1C_26_ILLSP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(1000, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1C_26_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1C_26_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1C_26_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1C_26_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(250, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1C_26_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1C_26_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1C_26_FAN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1C_26_FAN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1C_26_OED_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1C_26_OED_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1C_26_OB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1C_26_OB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1D_27_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1D_27_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1D_27_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1D_27_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1D_27_HUM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1D_27_HUM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1D_27_HUM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1D_27_HUM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1D_27_HUMSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1D_27_HUMSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1D_27_HUMSP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1D_27_HUMSP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1D_27_HUMSP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1D_27_HUMSP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1D_27_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1D_27_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1D_27_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1D_27_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(250, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1D_27_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1D_27_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1D_27_FAN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1D_27_FAN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1D_27_OED_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1D_27_OED_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1D_27_OB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1D_27_OB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1F_28_FAN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1F_28_FAN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1F_28_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1F_28_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1F_28_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1F_28_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1F_28_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1F_28_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1F_28_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1F_28_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1F_28_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1F_28_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1F_28_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1F_28_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_1F_28_TMP_F_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1F_28_TMP_F_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1F_28_SP_F_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1F_28_SP_F_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(26, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1F_28_FAN_F_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1F_28_FAN_F_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(27, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1F_28_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1F_28_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1F_28_UNOCC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1F_28_UNOCC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_1F_28_OCC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_1F_28_OCC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_20_29_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_20_29_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_20_29_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_20_29_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_20_29_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_20_29_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_20_29_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_20_29_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_20_29_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_20_29_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_20_29_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_20_29_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_20_29_SPM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_20_29_SPM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_20_29_BATT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_20_29_BATT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(27, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_20_29_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_20_29_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_20_29_ACT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_20_29_ACT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_21_30_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_21_30_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_21_30_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_21_30_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_21_30_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_21_30_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_21_30_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_21_30_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_21_30_HUM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_21_30_HUM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_21_30_HUM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_21_30_HUM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_21_30_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_21_30_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_21_30_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_21_30_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_21_30_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_21_30_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_21_30_SPM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_21_30_SPM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_21_30_BATT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_21_30_BATT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(27, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_21_30_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_21_30_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_21_30_ACT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_21_30_ACT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_22_31_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_22_31_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_22_31_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_22_31_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_22_31_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_22_31_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_22_31_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_22_31_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_22_31_HUM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_22_31_HUM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_22_31_HUM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_22_31_HUM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_22_31_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_22_31_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_22_31_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_22_31_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_22_31_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_22_31_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_22_31_FAN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_22_31_FAN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_22_31_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_22_31_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_23_32_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_23_32_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_23_32_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_23_32_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_23_32_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_23_32_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_23_32_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_23_32_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_23_32_HUM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_23_32_HUM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_23_32_HUM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_23_32_HUM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_23_32_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_23_32_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_23_32_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_23_32_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_23_32_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_23_32_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_10_23_32_FAN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_23_32_FAN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_23_32_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_23_32_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_10_23_32_OCC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_10_23_32_OCC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_01_1_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_01_1_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_01_1_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_01_1_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_01_1_ILL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_01_1_ILL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_11_01_1_ILL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_01_1_ILL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(510, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_11_01_1_ISP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_01_1_ISP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_01_1_ISP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_01_1_ISP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_11_01_1_ISP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_01_1_ISP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_11_01_1_DIM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_01_1_DIM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_01_1_DIM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_01_1_DIM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_11_01_1_DIM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_01_1_DIM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_11_01_1_REP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_01_1_REP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_01_1_PRT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_01_1_PRT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_01_1_DHV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_01_1_DHV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(26, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_01_1_EDIM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_01_1_EDIM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(27, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_01_1_MGC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_01_1_MGC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_01_1_OCC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_01_1_OCC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_01_1_PWR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_01_1_PWR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_02_2_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_02_2_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_02_2_CVAR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_02_2_CVAR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_02_2_CVAR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_02_2_CVAR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_11_02_2_CVAR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_02_2_CVAR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_11_02_2_FAN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_02_2_FAN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_02_2_ASP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_02_2_ASP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_02_2_ASP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_02_2_ASP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_11_02_2_ASP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_02_2_ASP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(51.2, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_11_02_2_ALR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_02_2_ALR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_02_2_CTM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_02_2_CTM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_02_2_CST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_02_2_CST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(27, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_02_2_ERH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_02_2_ERH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_02_2_RO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_02_2_RO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_03_3_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_03_3_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_03_3_BSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_03_3_BSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_03_3_BSP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_03_3_BSP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_11_03_3_BSP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_03_3_BSP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_11_03_3_AS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_03_3_AS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_03_3_AN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_03_3_AN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_03_3_AN_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_03_3_AN_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(90, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_11_03_3_AN_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_03_3_AN_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(180, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_11_03_3_PVF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_03_3_PVF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_03_3_AVF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_03_3_AVF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_03_3_ES_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_03_3_ES_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(18, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_03_3_EP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_03_3_EP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(20, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_03_3_ST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_03_3_ST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(22, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_03_3_SM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_03_3_SM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_03_3_MOTP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_03_3_MOTP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_04_4_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_04_4_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_04_4_P1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_04_4_P1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_04_4_P2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_04_4_P2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_04_4_P3_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_04_4_P3_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_04_4_SM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_04_4_SM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_04_4_OHF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_04_4_OHF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_04_4_ES_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_04_4_ES_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(26, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_04_4_PM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_04_4_PM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_04_4_ST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_04_4_ST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_05_5_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_05_5_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_05_5_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_05_5_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_05_6_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_05_6_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_05_6_WM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_05_6_WM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_05_6_RS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_05_6_RS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_11_05_6_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_11_05_6_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_00_1_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_00_1_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_00_1_MR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_00_1_MR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(24, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_00_1_MR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_00_1_MR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(16777215, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_00_1_MR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_00_1_MR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_00_1_CH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_00_1_CH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_00_1_CH_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_00_1_CH_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(15, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_00_1_CH_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_00_1_CH_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(15, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_00_1_DT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_00_1_DT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_00_1_DIV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_00_1_DIV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_01_2_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_01_2_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_01_2_MR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_01_2_MR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(24, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_01_2_MR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_01_2_MR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(16777215, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_01_2_MR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_01_2_MR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_01_2_TI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_01_2_TI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_01_2_TI_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_01_2_TI_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(15, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_01_2_TI_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_01_2_TI_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(15, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_01_2_DT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_01_2_DT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_01_2_DIV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_01_2_DIV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_02_3_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_02_3_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_02_3_MR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_02_3_MR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(24, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_02_3_MR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_02_3_MR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(16777215, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_02_3_MR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_02_3_MR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_02_3_TI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_02_3_TI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_02_3_TI_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_02_3_TI_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(15, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_02_3_TI_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_02_3_TI_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(15, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_02_3_DT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_02_3_DT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_02_3_DIV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_02_3_DIV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_03_4_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_03_4_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_03_4_MR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_03_4_MR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(24, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_03_4_MR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_03_4_MR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(16777215, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_03_4_MR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_03_4_MR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_03_4_TI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_03_4_TI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_03_4_TI_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_03_4_TI_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(15, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_03_4_TI_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_03_4_TI_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(15, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_03_4_DT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_03_4_DT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_03_4_DIV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_03_4_DIV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_04_5_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_04_5_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_04_5_MR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_04_5_MR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(14, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_04_5_MR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_04_5_MR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(16383, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_04_5_MR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_04_5_MR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(16383, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_04_5_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_04_5_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_04_5_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_04_5_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_04_5_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_04_5_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-40, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_04_5_BL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_04_5_BL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_05_6_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_05_6_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_05_6_PS0_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_05_6_PS0_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_05_6_PS1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_05_6_PS1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_05_6_PS2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_05_6_PS2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(2, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_05_6_PS3_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_05_6_PS3_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(3, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_05_6_PS4_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_05_6_PS4_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_05_6_PS5_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_05_6_PS5_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(5, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_05_6_PS6_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_05_6_PS6_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_05_6_PS7_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_05_6_PS7_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(7, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_05_6_PS8_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_05_6_PS8_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_05_6_PS9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_05_6_PS9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_05_6_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_05_6_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_05_6_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_05_6_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_05_6_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_05_6_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-40, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_05_6_BL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_05_6_BL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_10_7_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_10_7_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_10_7_MR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_10_7_MR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(24, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_10_7_MR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_10_7_MR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(16777215, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_10_7_MR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_10_7_MR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_10_7_CH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_10_7_CH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_10_7_CH_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_10_7_CH_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(15, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_10_7_CH_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_10_7_CH_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(15, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_12_10_7_DT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_10_7_DT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_12_10_7_DIV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_12_10_7_DIV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_01_1_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_01_1_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_01_1_DWS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_01_1_DWS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_01_1_DWS_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_01_1_DWS_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_01_1_DWS_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_01_1_DWS_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(999, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_01_1_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_01_1_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_01_1_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_01_1_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_01_1_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_01_1_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-40, scaleMin);
            Assert.Equal(80, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_01_1_WND_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_01_1_WND_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_01_1_WND_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_01_1_WND_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_01_1_WND_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_01_1_WND_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(70, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_01_1_ID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_01_1_ID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_01_1_DN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_01_1_DN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_01_1_RAN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_01_1_RAN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_02_2_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_02_2_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_02_2_SNW_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_02_2_SNW_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_02_2_SNW_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_02_2_SNW_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_02_2_SNW_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_02_2_SNW_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(150, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_02_2_SNS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_02_2_SNS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_02_2_SNS_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_02_2_SNS_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_02_2_SNS_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_02_2_SNS_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(150, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_02_2_SNE_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_02_2_SNE_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_02_2_SNE_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_02_2_SNE_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_02_2_SNE_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_02_2_SNE_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(150, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_02_2_ID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_02_2_ID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_02_2_HEM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_02_2_HEM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_03_3_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_03_3_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_03_3_DY_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_03_3_DY_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(3, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_03_3_DY_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_03_3_DY_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(31, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_03_3_DY_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_03_3_DY_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(31, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_03_3_MTH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_03_3_MTH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_03_3_MTH_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_03_3_MTH_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(12, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_03_3_MTH_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_03_3_MTH_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(12, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_03_3_YR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_03_3_YR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_03_3_YR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_03_3_YR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(99, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_03_3_YR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_03_3_YR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(2000, scaleMin);
            Assert.Equal(2099, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_03_3_ID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_03_3_ID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_03_3_SRC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_03_3_SRC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_04_4_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_04_4_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_04_4_WDY_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_04_4_WDY_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_04_4_HR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_04_4_HR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(3, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_04_4_HR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_04_4_HR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(23, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_04_4_HR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_04_4_HR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(23, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_04_4_MIN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_04_4_MIN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_04_4_MIN_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_04_4_MIN_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(59, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_04_4_MIN_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_04_4_MIN_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(59, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_04_4_SEC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_04_4_SEC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(18, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_04_4_SEC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_04_4_SEC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(59, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_04_4_SEC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_04_4_SEC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(59, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_04_4_ID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_04_4_ID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_04_4_TMF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_04_4_TMF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_04_4_APM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_04_4_APM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_04_4_SRC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_04_4_SRC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_05_5_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_05_5_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_05_5_ELV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_05_5_ELV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_05_5_ELV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_05_5_ELV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(180, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_05_5_ELV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_05_5_ELV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-90, scaleMin);
            Assert.Equal(90, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_05_5_AZM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_05_5_AZM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(15, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_05_5_AZM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_05_5_AZM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(359, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_05_5_AZM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_05_5_AZM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(359, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_05_5_ID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_05_5_ID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_06_6_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_06_6_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_06_6_LAT_MSB__DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_06_6_LAT_MSB__Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_06_6_LAT_MSB__Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_06_6_LAT_MSB__Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4095, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_06_6_LAT_MSB__Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_06_6_LAT_MSB__Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_06_6_LOT_MSB__DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_06_6_LOT_MSB__Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_06_6_LOT_MSB__Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_06_6_LOT_MSB__Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4095, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_06_6_LOT_MSB__Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_06_6_LOT_MSB__Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_06_6_LAT_LSB__DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_06_6_LAT_LSB__Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_06_6_LAT_LSB__Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_06_6_LAT_LSB__Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4095, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_06_6_LAT_LSB__Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_06_6_LAT_LSB__Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-90, scaleMin);
            Assert.Equal(90, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_06_6_LOT_LSB__DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_06_6_LOT_LSB__Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_06_6_LOT_LSB__Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_06_6_LOT_LSB__Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4095, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_06_6_LOT_LSB__Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_06_6_LOT_LSB__Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-180, scaleMin);
            Assert.Equal(180, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_06_6_ID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_06_6_ID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_07_7_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_07_7_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_07_7_WD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_07_7_WD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_07_7_AWS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_07_7_AWS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_07_7_AWS_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_07_7_AWS_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_07_7_AWS_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_07_7_AWS_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(199.9, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_07_7_MWS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_07_7_MWS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_07_7_MWS_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_07_7_MWS_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_07_7_MWS_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_07_7_MWS_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(199.9, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_07_7_BS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_07_7_BS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_08_8_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_08_8_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_08_8_RAS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_08_8_RAS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_08_8_RFA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_08_8_RFA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(2, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_08_8_RFC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_08_8_RFC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_08_8_RFC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_08_8_RFC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(65535, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_08_8_RFC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_08_8_RFC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(65535, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_08_8_BS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_08_8_BS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_10_9_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_10_9_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_10_9_DN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_10_9_DN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(7, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_10_9_SNE_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_10_9_SNE_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_10_9_SNE_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_10_9_SNE_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(90, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_10_9_SNE_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_10_9_SNE_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(90, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_10_9_SNA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_10_9_SNA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_10_9_SNA_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_10_9_SNA_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(180, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_10_9_SNA_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_10_9_SNA_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-90, scaleMin);
            Assert.Equal(90, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_10_9_SRA_MSB__DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_10_9_SRA_MSB__Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_10_9_SRA_MSB__Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_10_9_SRA_MSB__Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(2000, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_10_9_SRA_MSB__Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_10_9_SRA_MSB__Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_10_9_SRA_LSB__DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_10_9_SRA_LSB__Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_13_10_9_SRA_LSB__Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_10_9_SRA_LSB__Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(2000, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_10_9_SRA_LSB__Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_10_9_SRA_LSB__Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(2000, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_13_10_9_ID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_13_10_9_ID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_01_1_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_01_1_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_01_1_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_01_1_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_01_1_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_01_1_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_01_1_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_01_1_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_01_1_CT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_01_1_CT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_02_2_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_02_2_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_02_2_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_02_2_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_02_2_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_02_2_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_02_2_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_02_2_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_02_2_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_02_2_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_02_2_ILL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_02_2_ILL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_02_2_ILL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_02_2_ILL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(1000, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_02_2_CT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_02_2_CT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_03_3_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_03_3_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_03_3_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_03_3_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_03_3_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_03_3_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_03_3_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_03_3_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_03_3_VIB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_03_3_VIB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_03_3_CT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_03_3_CT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_04_4_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_04_4_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_04_4_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_04_4_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_04_4_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_04_4_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_04_4_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_04_4_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_04_4_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_04_4_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_04_4_ILL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_04_4_ILL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_04_4_ILL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_04_4_ILL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(1000, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_04_4_VIB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_04_4_VIB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_04_4_CT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_04_4_CT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_05_5_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_05_5_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_05_5_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_05_5_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_05_5_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_05_5_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_05_5_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_05_5_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_05_5_VIB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_05_5_VIB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_06_6_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_06_6_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_06_6_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_06_6_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_06_6_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_06_6_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_06_6_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_06_6_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_06_6_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_06_6_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_06_6_ILL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_06_6_ILL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_06_6_ILL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_06_6_ILL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(1000, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_06_6_VIB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_06_6_VIB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_07_7_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_07_7_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_07_7_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_07_7_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_07_7_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_07_7_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_07_7_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_07_7_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_07_7_DCT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_07_7_DCT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_07_7_LCT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_07_7_LCT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_08_8_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_08_8_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_08_8_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_08_8_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_08_8_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_08_8_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_08_8_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_08_8_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_08_8_DCT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_08_8_DCT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_08_8_LCT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_08_8_LCT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_08_8_VIB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_08_8_VIB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_09_9_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_09_9_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_09_9_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_09_9_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_09_9_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_09_9_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_09_9_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_09_9_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_09_9_CT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_09_9_CT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_0A_10_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_0A_10_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_0A_10_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_0A_10_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_0A_10_SVC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_0A_10_SVC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_0A_10_SVC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_0A_10_SVC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(5.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_14_0A_10_CT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_0A_10_CT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_14_0A_10_VIB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_14_0A_10_VIB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_1_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_1_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_1_CV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_1_CV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_1_CV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_1_CV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_1_CV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_1_CV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_1_SO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_1_SO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_1_ENIE_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_1_ENIE_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_1_ES_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_1_ES_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_1_BCAP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_1_BCAP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_1_CCO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_1_CCO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_1_FTS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_1_FTS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_1_DWO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_1_DWO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(14, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_1_ACO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_1_ACO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(15, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_1_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_1_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_1_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_1_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_1_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_1_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_2_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_2_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_2_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_2_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_2_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_2_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_2_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_2_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_2_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_2_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_2_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_2_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_2_RIN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_2_RIN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_2_LFS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_2_LFS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_2_VO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_2_VO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(18, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_2_VC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_2_VC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(19, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_2_SB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_2_SB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(20, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_2_SPS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_2_SPS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_2_SPN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_2_SPN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(22, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_01_2_RCU_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_2_RCU_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(23, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_02_3_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_02_3_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_02_3_AV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_02_3_AV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_02_3_AV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_02_3_AV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_02_3_AV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_02_3_AV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_02_3_SPI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_02_3_SPI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(22, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_02_4_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_02_4_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_02_4_VSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_02_4_VSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_02_4_VSP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_02_4_VSP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_02_4_VSP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_02_4_VSP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_03_5_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_03_5_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_03_5_AV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_03_5_AV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_03_5_AV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_03_5_AV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_03_5_AV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_03_5_AV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_03_5_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_03_5_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_03_5_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_03_5_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_03_5_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_03_5_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_03_6__DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_03_6__Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(22, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_03_6_SPS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_03_6_SPS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_03_6_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_03_6_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_03_6_ATS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_03_6_ATS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_03_6_ATS_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_03_6_ATS_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_03_6_TMPRC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_03_6_TMPRC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_03_6_TMPRC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_03_6_TMPRC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_03_6_TMPRC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_03_6_TMPRC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_7_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_7_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_7_CP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_7_CP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_7_CP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_7_CP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_7_CP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_7_CP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_7_FTS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_7_FTS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_7_FTS_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_7_FTS_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_7_FTS_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_7_FTS_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_7_TMPFC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_7_TMPFC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_7_MST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_7_MST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_7_STR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_7_STR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_7_BLS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_7_BLS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_7_TS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_7_TS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_7_FL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_7_FL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_8_POS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_8_POS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_8_POS_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_8_POS_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_8_POS_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_8_POS_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_8_TSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_8_TSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_8_TSP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_8_TSP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_8_TSP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_8_TSP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(10, scaleMin);
            Assert.Equal(30, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_8_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_8_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_8_MC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_8_MC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_8_WUC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_8_WUC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(18, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_8_DSO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_8_DSO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(26, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_8_BLC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_8_BLC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_04_8_SER_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_04_8_SER_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_05_9_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_05_9_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_05_9_ASP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_05_9_ASP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_05_9_ASPT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_05_9_ASPT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(3, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_05_9_E_NBAT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_05_9_E_NBAT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_05_9_E_NCOM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_05_9_E_NCOM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_05_9_E_SENS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_05_9_E_SENS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_05_9_E_FSPD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_05_9_E_FSPD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(14, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_05_9_ERR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_05_9_ERR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(15, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_05_9_FLTR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_05_9_FLTR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_05_9_AUTO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_05_9_AUTO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_05_9_BPASS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_05_9_BPASS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_05_9_FROST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_05_9_FROST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(26, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_05_10_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_05_10_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_05_10_NSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_05_10_NSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_05_10_NSPT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_05_10_NSPT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(3, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_05_10_RSTE_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_05_10_RSTE_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_05_10_RSTF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_05_10_RSTF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_10_11_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_10_11_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_10_11_MD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_10_11_MD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_10_11_VPS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_10_11_VPS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_10_11_FANSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_10_11_FANSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_10_11_CVAR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_10_11_CVAR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_10_11_CVAR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_10_11_CVAR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_10_11_RO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_10_11_RO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_10_11_OI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_10_11_OI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_10_12_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_10_12_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_10_12_MD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_10_12_MD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_10_12_VPS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_10_12_VPS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_10_12_FANSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_10_12_FANSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_10_12_CVAR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_10_12_CVAR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_10_12_CVAR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_10_12_CVAR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_10_12_RO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_10_12_RO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_10_12_OI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_10_12_OI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_11_13_DRC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_11_13_DRC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_11_13_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_11_13_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_11_13_EXDS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_11_13_EXDS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(23, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_11_13_WC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_11_13_WC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_11_14_RCD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_11_14_RCD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_11_14_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_11_14_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_11_14_ERR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_11_14_ERR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_11_14_ERR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_11_14_ERR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(65535, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_11_14_ERR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_11_14_ERR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(65535, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_11_14_RES_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_11_14_RES_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_11_14_OD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_11_14_OD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(20, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_11_14_WCD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_11_14_WCD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_11_14_KCD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_11_14_KCD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(22, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_11_14_ED_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_11_14_ED_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(23, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_11_14_WC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_11_14_WC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_11_14_AS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_11_14_AS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_12_15_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_12_15_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_12_15_CV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_12_15_CV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_12_15_CV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_12_15_CV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_12_15_CV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_12_15_CV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_12_15_FANOR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_12_15_FANOR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_12_15_SPS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_12_15_SPS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_12_15_SPS_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_12_15_SPS_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_12_15_SPS_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_12_15_SPS_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-10, scaleMin);
            Assert.Equal(10, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_20_12_15_CTM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_12_15_CTM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_12_15_CST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_12_15_CST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(27, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_12_15_ERH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_12_15_ERH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_20_12_15_RO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_12_15_RO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_01_1_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_01_1_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_01_1_SVC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_01_1_SVC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_01_1_IPS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_01_1_IPS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_02_2_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_02_2_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_02_2_IPS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_02_2_IPS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_03_3_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_03_3_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_03_3_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_03_3_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_03_3_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_03_3_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(255, rangeMin);
            Assert.Equal(0, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_30_03_3_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_03_3_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_30_03_3_WA0_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_03_3_WA0_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(19, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_03_3_DI3_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_03_3_DI3_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(20, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_03_3_DI2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_03_3_DI2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_03_3_DI1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_03_3_DI1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(22, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_03_3_DI0_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_03_3_DI0_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(23, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_04_4_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_04_4_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_04_4_DV0_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_04_4_DV0_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_04_4_DV0_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_04_4_DV0_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_30_04_4_DV0_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_04_4_DV0_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_30_04_4_DI2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_04_4_DI2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_04_4_DI1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_04_4_DI1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_04_4_DI0_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_04_4_DI0_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_05_5_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_05_5_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_05_5_VDD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_05_5_VDD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_05_5_VDD_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_05_5_VDD_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_30_05_5_VDD_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_05_5_VDD_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(3.3, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_30_05_5_ST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_05_5_ST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_30_05_5_IOS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_30_05_5_IOS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_37_01_1_DRL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_37_01_1_DRL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_37_01_1_DRL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_37_01_1_DRL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(15, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_37_01_1_DRL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_37_01_1_DRL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(15, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_37_01_1_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_37_01_1_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_37_01_1_TMPD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_37_01_1_TMPD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_37_01_1_TMPD_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_37_01_1_TMPD_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_37_01_1_TMPD_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_37_01_1_TMPD_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_37_01_1_SPWRU_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_37_01_1_SPWRU_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_37_01_1_PWRU_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_37_01_1_PWRU_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_37_01_1_PWRU_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_37_01_1_PWRU_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_37_01_1_PWRU_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_37_01_1_PWRU_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_37_01_1_TMOS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_37_01_1_TMOS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_37_01_1_TMOS_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_37_01_1_TMOS_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_37_01_1_TMOS_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_37_01_1_TMOS_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(15, scaleMin);
            Assert.Equal(3825, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_37_01_1_RSD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_37_01_1_RSD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_37_01_1_RED_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_37_01_1_RED_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_37_01_1_MPWRU_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_37_01_1_MPWRU_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_1_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_1_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_1_COM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_1_COM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_1_TIM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_1_TIM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_1_TIM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_1_TIM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(65535, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_1_TIM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_1_TIM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0.1, scaleMin);
            Assert.Equal(6553.5, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_1_LCK_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_1_LCK_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_1_DEL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_1_DEL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_1_SW_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_1_SW_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_2_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_2_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_2_COM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_2_COM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_2_EDIM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_2_EDIM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_2_EDIM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_2_EDIM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_2_EDIM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_2_EDIM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_2_RMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_2_RMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_2_RMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_2_RMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_2_RMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_2_RMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_2_EDIMR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_2_EDIMR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_2_STR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_2_STR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_2_SW_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_2_SW_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_3_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_3_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_3_COM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_3_COM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_3_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_3_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_3_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_3_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_3_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_3_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-12.7, scaleMin);
            Assert.Equal(12.8, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_4_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_4_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_4_COM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_4_COM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_4_BSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_4_BSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_4_BSP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_4_BSP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_4_BSP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_4_BSP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(51.2, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_5_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_5_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_5_COM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_5_COM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_5_CVOV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_5_CVOV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_5_CVOV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_5_CVOV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_5_CVOV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_5_CVOV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_5_CM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_5_CM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_5_CS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_5_CS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(27, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_5_ENHO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_5_ENHO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_5_RMOCC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_5_RMOCC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_6_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_6_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_6_COM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_6_COM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_6_FO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_6_FO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_7_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_7_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_7_COM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_7_COM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_7_P1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_7_P1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_7_P2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_7_P2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_7_FUNC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_7_FUNC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_7_SSF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_7_SSF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_7_PAF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_7_PAF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_08_7_SMF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_08_7_SMF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_09_8_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_09_8_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_09_8_P1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_09_8_P1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_09_8_P2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_09_8_P2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_09_8_P3_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_09_8_P3_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_09_8_FUNC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_09_8_FUNC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_09_8_SSF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_09_8_SSF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_09_8_SFV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_09_8_SFV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_38_09_8_SMF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_38_09_8_SMF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_3F_00_1_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_3F_00_1_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_3F_00_1_MSGID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_3F_00_1_MSGID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_3F_00_1_MSGS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_3F_00_1_MSGS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_3F_00_2_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_3F_00_2_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_3F_00_2_STCNT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_3F_00_2_STCNT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_3F_00_2_RSLV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_3F_00_2_RSLV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(2, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_3F_00_2_MSGID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_3F_00_2_MSGID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_3F_00_2_MSGS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_3F_00_2_MSGS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_3F_00_3_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_3F_00_3_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_3F_00_3_MC_MSB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_3F_00_3_MC_MSB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_3F_00_3_MC_LSB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_3F_00_3_MC_LSB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(5, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_3F_00_3_MSGS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_3F_00_3_MSGS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(7, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_3F_00_4_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_3F_00_4_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_3F_00_4_MC_MSB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_3F_00_4_MC_MSB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_3F_00_4_MC_LSB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_3F_00_4_MC_LSB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(5, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_3F_00_4_MSGS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_3F_00_4_MSGS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(7, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_3F_7F_5_LRNB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_3F_7F_5_LRNB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldA5_3F_7F_5_undef_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_3F_7F_5_undef_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(28, propSize);
        }

                }
            }
            