
            using System;
            using System.Collections.Generic;
            using System.Text;
            using Automatica.Core.UnitTests.Drivers;
            using Xunit;

            namespace P3.Driver.EnOcean.DriverFactory.Tests
            {
                public class DriverFactoryRorgF6Tests : DriverFactoryTestBase<EnOceanDriverFactory>
                {
                    
// AUTO GENERATED 20.07.2018 19:29:39
//-------------------------
//-------------------------
// 0xF6 -------------------
//-------------------------
//-------------------------


        [Fact]
        public void Test_DataFieldF6_01_01_1_PB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_01_01_1_PB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(3, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_02_01_1_R1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_02_01_1_R1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_02_01_1_EB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_02_01_1_EB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(3, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_02_01_1_R2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_02_01_1_R2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_02_01_1_SA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_02_01_1_SA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(7, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_02_01_2_R1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_02_01_2_R1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_02_01_2_EB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_02_01_2_EB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(3, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_02_02_3_R1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_02_02_3_R1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_02_02_3_EB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_02_02_3_EB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(3, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_02_02_3_R2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_02_02_3_R2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_02_02_3_SA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_02_02_3_SA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(7, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_02_02_4_R1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_02_02_4_R1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_02_02_4_EB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_02_02_4_EB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(3, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_02_03_5_RA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_02_03_5_RA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_02_04_6_EBO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_02_04_6_EBO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_02_04_6_BC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_02_04_6_BC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_02_04_6_RBI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_02_04_6_RBI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_02_04_6_RB0_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_02_04_6_RB0_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(5, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_02_04_6_RAI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_02_04_6_RAI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_02_04_6_RA0_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_02_04_6_RA0_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(7, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_03_01_1_R1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_03_01_1_R1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_03_01_1_EB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_03_01_1_EB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(3, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_03_01_1_R2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_03_01_1_R2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_03_01_1_SA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_03_01_1_SA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(7, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_03_01_2_R1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_03_01_2_R1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_03_01_2_EB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_03_01_2_EB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(3, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_03_02_3_R1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_03_02_3_R1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_03_02_3_EB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_03_02_3_EB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(3, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_03_02_3_R2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_03_02_3_R2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_03_02_3_SA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_03_02_3_SA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(7, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_03_02_4_R1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_03_02_4_R1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_03_02_4_EB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_03_02_4_EB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(3, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_04_01_1_KC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_04_01_1_KC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_04_01_2_KC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_04_01_2_KC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_04_02_3_EBO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_04_02_3_EBO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_04_02_3_BC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_04_02_3_BC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_04_02_3_SOC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_04_02_3_SOC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(5, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_05_00_1_WND_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_05_00_1_WND_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_05_01_2_WAS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_05_01_2_WAS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_05_02_3_SMO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_05_02_3_SMO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_10_00_1_WIN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_10_00_1_WIN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_10_01_2_HC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_10_01_2_HC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldF6_10_01_2_HVL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgF6Data.DataFieldF6_10_01_2_HVL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

                }
            }
            