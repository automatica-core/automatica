
using Automatica.Core.UnitTests.Base.Drivers;
            using Xunit;

            namespace P3.Driver.EnOcean.DriverFactory.Tests
            {
                public class DriverFactoryRorgD2Tests : DriverFactoryTestBase<EnOceanDriverFactory>
                {
                    
// AUTO GENERATED 20.07.2018 19:29:39
//-------------------------
//-------------------------
// 0xD2 -------------------
//-------------------------
//-------------------------


        [Fact]
        public void Test_DataFieldD2_00_01_1_MI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_1_MI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(5, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_1_KP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_1_KP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_1_CV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_1_CV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_2_MI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_2_MI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(5, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_2_MD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_2_MD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_2_F_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_2_F_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_2_M_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_2_M_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_2_TA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_2_TA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_2_PR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_2_PR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_2_ZA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_2_ZA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_2_Sa_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_2_Sa_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(39, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_2_Sb_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_2_Sb_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(38, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_2_Sc_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_2_Sc_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(37, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_2_Sd_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_2_Sd_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(36, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_2_Se_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_2_Se_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(35, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_3_MI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_3_MI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(5, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_3_F_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_3_F_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_3_TA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_3_TA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_3_PR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_3_PR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_3_ZA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_3_ZA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_3_ZA_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_3_ZA_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(-1270, rangeMin);
            Assert.Equal(1270, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_3_ZA_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_3_ZA_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-12.70, scaleMin);
            Assert.Equal(12.70, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_4_MI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_4_MI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(5, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_4_VA_LSB__DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_4_VA_LSB__Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_4_VA_LSB__Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_4_VA_LSB__Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4000, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_4_VA_LSB__Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_4_VA_LSB__Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40.00, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_4_VA_MSB__DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_4_VA_MSB__Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(20, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_4_TA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_4_TA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_5_MI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_5_MI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(5, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_5_MD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_5_MD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_5_SPR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_5_SPR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_5_SPS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_5_SPS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_5_TT_LSB__DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_5_TT_LSB__Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_5_TT_MSB__DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_5_TT_MSB__Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(38, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_5_F_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_5_F_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(35, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_5_PR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_5_PR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_5_KA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_5_KA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(45, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_5_ST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_5_ST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_5_ST_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_5_ST_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(15, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_00_01_5_ST_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_00_01_5_ST_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0.0, scaleMin);
            Assert.Equal(3.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_1_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_1_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_1_DV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_1_DV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_1_IO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_1_IO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_1_OV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_1_OV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_2_de_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_2_de_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_2_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_2_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_2_OC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_2_OC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_2_RO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_2_RO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_2_LC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_2_LC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_2_IO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_2_IO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_2_DT3_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_2_DT3_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(20, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_2_DT1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_2_DT1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_2_DT2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_2_DT2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_2_dn_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_2_dn_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_2_PF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_2_PF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_2_DS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_2_DS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(26, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_3_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_3_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_3_IO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_3_IO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_4_PF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_4_PF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_4_PFD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_4_PFD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_4_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_4_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_4_OC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_4_OC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_4_EL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_4_EL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_4_IO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_4_IO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_4_LC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_4_LC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_4_OV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_4_OV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_5_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_5_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_5_RM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_5_RM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_5_RE_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_5_RE_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_5_ep_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_5_ep_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_5_IO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_5_IO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_5_MD_LSB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_5_MD_LSB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_5_MD_LSB_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_5_MD_LSB_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4095, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_5_MD_LSB_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_5_MD_LSB_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(4095, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_5_MD_MSB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_5_MD_MSB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_5_MD_MSB_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_5_MD_MSB_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4095, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_5_MD_MSB_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_5_MD_MSB_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(4095, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_5_UN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_5_UN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_5_MAT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_5_MAT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_5_MIT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_5_MIT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_6_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_6_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_6_qu_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_6_qu_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_6_IO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_6_IO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_7_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_7_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_7_UN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_7_UN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_7_IO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_7_IO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_7_MV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_7_MV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(32, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_7_MV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_7_MV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4294967295, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_8_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_8_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_8_PM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_8_PM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_9_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_9_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_10_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_10_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_10_PM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_10_PM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_11_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_11_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_11_IO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_11_IO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_11_AOT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_11_AOT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_11_DOT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_11_DOT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_11_EBM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_11_EBM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(48, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_11_SWT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_11_SWT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(50, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_12_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_12_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_12_IO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_12_IO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_13_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_13_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_13_IO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_13_IO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_13_AOT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_13_AOT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_13_DOT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_13_DOT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_13_EBM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_13_EBM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(48, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_13_SWT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_13_SWT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(50, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_14_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_14_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_14_ECID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_14_ECID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_14_IO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_14_IO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_14_MAXV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_14_MAXV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_14_MINV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_14_MINV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(33, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_15_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_15_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_15_ECID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_15_ECID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_15_IO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_15_IO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_16_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_16_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_16_ECID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_16_ECID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_16_IO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_16_IO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_16_MAXV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_16_MAXV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_01_00_16_MINV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_01_00_16_MINV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(33, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_1_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_1_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_1_type_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_1_type_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_1_MV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_1_MV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_1_MV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_1_MV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(65535, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_2_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_2_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_2_ST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_2_ST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_2_TA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_2_TA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_3_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_3_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_3_RM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_3_RM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_3_MD_LSB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_3_MD_LSB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_3_MD_LSB_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_3_MD_LSB_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4095, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_3_MD_LSB_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_3_MD_LSB_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(4095, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_3_MD_MSB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_3_MD_MSB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_3_MD_MSB_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_3_MD_MSB_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4095, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_3_MD_MSB_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_3_MD_MSB_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(4095, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_3_UN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_3_UN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_3_MAT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_3_MAT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_3_MAT_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_3_MAT_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_3_MAT_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_3_MAT_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(10, scaleMin);
            Assert.Equal(2550, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_3_MIT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_3_MIT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_3_MIT_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_3_MIT_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_3_MIT_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_3_MIT_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_4_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_4_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_02_00_4_qu_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_02_00_4_qu_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_03_00_1_RI2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_03_00_1_RI2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_03_10_2_WIN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_03_10_2_WIN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_03_20_3_ES_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_03_20_3_ES_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_04_00_1_CO2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_04_00_1_CO2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_04_00_1_CO2_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_04_00_1_CO2_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_04_00_1_CO2_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_04_00_1_CO2_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_04_00_1_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_04_00_1_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_04_00_1_HUM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_04_00_1_HUM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(200, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_04_00_1_HUM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_04_00_1_HUM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_04_00_1_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_04_00_1_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_04_00_1_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_04_00_1_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_04_00_1_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_04_00_1_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(51, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_04_00_1_DN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_04_00_1_DN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_04_00_1_BA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_04_00_1_BA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_1_POS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_1_POS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_1_ANG_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_1_ANG_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_1_REPO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_1_REPO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_1_LOCK_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_1_LOCK_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_1_CHN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_1_CHN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_1_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_1_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_2_CHN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_2_CHN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_2_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_2_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_3_CHN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_3_CHN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_3_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_3_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_4_POS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_4_POS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_4_ANG_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_4_ANG_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_4_LOCK_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_4_LOCK_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_4_CHN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_4_CHN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_4_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_4_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_5_VERT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_5_VERT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(15, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_5_ROT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_5_ROT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_5_AA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_5_AA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_5_CHN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_5_CHN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_00_5_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_00_5_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(36, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_02_6_POS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_02_6_POS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_02_6_ANG_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_02_6_ANG_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_02_6_REPO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_02_6_REPO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_02_6_LOCK_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_02_6_LOCK_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_02_6_CHN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_02_6_CHN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_02_6_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_02_6_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_02_7_CHN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_02_7_CHN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_02_7_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_02_7_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_02_8_CHN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_02_8_CHN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_02_8_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_02_8_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_02_9_POS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_02_9_POS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_02_9_ANG_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_02_9_ANG_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_02_9_LOCK_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_02_9_LOCK_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_02_9_CHN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_02_9_CHN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_05_02_9_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_05_02_9_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_1_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_1_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_1_BAL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_1_BAL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_1_PPAL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_1_PPAL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_1_HP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_1_HP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_1_WS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_1_WS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(20, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_1_BR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_1_BR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_1_BL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_1_BL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_1_M_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_1_M_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_1_V_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_1_V_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(36, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_1_T_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_1_T_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_1_H_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_1_H_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(48, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_1_I_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_1_I_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(56, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_1_BS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_1_BS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(72, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_2_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_2_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_2_VMR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_2_VMR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_2_HCCR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_2_HCCR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_2_BLCR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_2_BLCR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_2_SUIR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_2_SUIR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_2_VBIR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_2_VBIR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_3_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_3_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_3_PON_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_3_PON_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(32, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_3_ALL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_3_ALL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(32, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_4_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_4_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_4_HMC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_4_HMC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(32, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_4_HMO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_4_HMO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(32, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_4_HMT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_4_HMT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(72, propOffset);
            Assert.Equal(32, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_5_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_5_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_5_WT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_5_WT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(32, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_6_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_6_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_6_BRP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_6_BRP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(32, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_6_BLP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_6_BLP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(32, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_7_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_7_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_7_GCS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_7_GCS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_7_GLD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_7_GLD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_7_VMS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_7_VMS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_7_HCCS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_7_HCCS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_7_BLCS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_7_BLCS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_7_SUIS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_7_SUIS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_01_7_VBIS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_01_7_VBIS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_50_8_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_50_8_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_50_8_AM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_50_8_AM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_50_8_WDS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_50_8_WDS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_50_8_CT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_50_8_CT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(32, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_50_8_CB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_50_8_CB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(48, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_50_8_BS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_50_8_BS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(49, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_50_8_MOE_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_50_8_MOE_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(56, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_50_8_ACE_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_50_8_ACE_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(58, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_50_8_MAE_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_50_8_MAE_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(60, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_50_8_SE_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_50_8_SE_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(62, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_50_9_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_50_9_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_50_9_BA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_50_9_BA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_50_10_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_50_10_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_50_10_CS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_50_10_CS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_06_50_10_CAL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_06_50_10_CAL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_0A_00_1_BL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_0A_00_1_BL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_0A_00_1_CH1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_0A_00_1_CH1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_0A_00_1_CH1_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_0A_00_1_CH1_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_0A_00_1_CH1_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_0A_00_1_CH1_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(85, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_0A_00_1_CH2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_0A_00_1_CH2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_0A_00_1_CH2_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_0A_00_1_CH2_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_0A_00_1_CH2_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_0A_00_1_CH2_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(85, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_0A_00_1_CH3_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_0A_00_1_CH3_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_0A_00_1_CH3_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_0A_00_1_CH3_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_0A_00_1_CH3_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_0A_00_1_CH3_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(85, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_0A_01_2_BL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_0A_01_2_BL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_0A_01_2_CH1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_0A_01_2_CH1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_0A_01_2_CH2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_0A_01_2_CH2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_0A_01_2_CH3_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_0A_01_2_CH3_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_1_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_1_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_1_MCF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_1_MCF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_1_IRC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_1_IRC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_1_FBC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_1_FBC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_1_GMT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_1_GMT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(15, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_MCF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_MCF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_HUM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_HUM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_HUM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_HUM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_HVF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_HVF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_FS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_FS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_FS_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_FS_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_FS_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_FS_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_FSV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_FSV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_FSM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_FSM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_CW2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_CW2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(27, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_CW1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_CW1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_MW_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_MW_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_WOD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_WOD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_BS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_BS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(33, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_SPS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_SPS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(35, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_PIR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_PIR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(36, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_OBS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_OBS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(38, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_COO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_COO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_HEA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_HEA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(42, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_RCM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_RCM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(44, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_SPV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_SPV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(46, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_TPV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_TPV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(47, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_TSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_TSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(48, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_TSP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_TSP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_TSP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_TSP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(56, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_2_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_2_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_MCF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_MCF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_PSL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_PSL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_TSL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_TSL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_DCL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_DCL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_DTL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_DTL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_TPL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_TPL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_OBL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_OBL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_SPL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_SPL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(14, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_FSL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_FSL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(15, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_RCI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_RCI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_KL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_KL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(22, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_DC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_DC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_TS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_TS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(27, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_DST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_DST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_TN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_TN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_DAY_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_DAY_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_DAY_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_DAY_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(31, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_DAY_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_DAY_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(31, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_MON_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_MON_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(37, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_MON_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_MON_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(12, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_MON_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_MON_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(12, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_YR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_YR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(41, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_YR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_YR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(127, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_YR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_YR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(2000, scaleMin);
            Assert.Equal(2127, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_MIN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_MIN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(48, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_MIN_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_MIN_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(59, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_MIN_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_MIN_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(59, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_HR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_HR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(56, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_HR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_HR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(23, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_HR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_HR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(23, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_3_DTU_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_3_DTU_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(63, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_4_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_4_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_4_MCF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_4_MCF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_4_SPB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_4_SPB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_4_SPB_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_4_SPB_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_4_SPB_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_4_SPB_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_4_SPP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_4_SPP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_4_SPP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_4_SPP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_4_SPP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_4_SPP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_4_SPE_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_4_SPE_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_4_SPE_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_4_SPE_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_4_SPE_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_4_SPE_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_4_SPC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_4_SPC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_4_SPC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_4_SPC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_4_SPC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_4_SPC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_4_SFB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_4_SFB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(44, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_4_SFP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_4_SFP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(45, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_4_SFE_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_4_SFE_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(46, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_4_SFC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_4_SFC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(47, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_5_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_5_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_5_MCF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_5_MCF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_5_ETM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_5_ETM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_5_ETM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_5_ETM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(59, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_5_ETM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_5_ETM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(59, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_5_ETH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_5_ETH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(19, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_5_ETH_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_5_ETH_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(23, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_5_ETH_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_5_ETH_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(23, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_5_STM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_5_STM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(26, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_5_STM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_5_STM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(59, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_5_STM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_5_STM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(59, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_5_STH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_5_STH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(35, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_5_STH_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_5_STH_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(23, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_5_STH_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_5_STH_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(23, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_5_PER_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_5_PER_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_5_RCM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_5_RCM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(44, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_00_5_TPD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_00_5_TPD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(47, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_6_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_6_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_6_MCF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_6_MCF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_6_IRC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_6_IRC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_6_FBC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_6_FBC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_6_GMT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_6_GMT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(15, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_7_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_7_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_7_MCF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_7_MCF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_7_CW2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_7_CW2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(27, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_7_CW1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_7_CW1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_7_WOD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_7_WOD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_7_BS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_7_BS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(33, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_7_OBS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_7_OBS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(38, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_7_RCM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_7_RCM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(44, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_7_SPV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_7_SPV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(46, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_7_TPV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_7_TPV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(47, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_7_TSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_7_TSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(48, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_7_TSP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_7_TSP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_7_TSP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_7_TSP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_7_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_7_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(56, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_7_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_7_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_7_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_7_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_MCF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_MCF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_TSL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_TSL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_DCL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_DCL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_DTL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_DTL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_TPL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_TPL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_OBL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_OBL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_SPL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_SPL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(14, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_RCI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_RCI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_KL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_KL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(22, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_DC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_DC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_TS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_TS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(27, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_DST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_DST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_TN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_TN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_DAY_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_DAY_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_DAY_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_DAY_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(31, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_DAY_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_DAY_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(31, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_MON_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_MON_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(37, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_MON_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_MON_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(12, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_MON_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_MON_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(12, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_YR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_YR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(41, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_YR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_YR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(127, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_YR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_YR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(2000, scaleMin);
            Assert.Equal(2127, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_MIN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_MIN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(48, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_MIN_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_MIN_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(59, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_MIN_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_MIN_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(59, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_HR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_HR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(56, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_HR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_HR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(23, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_HR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_HR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(23, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_8_DTU_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_8_DTU_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(63, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_9_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_9_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_9_MCF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_9_MCF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_9_SPB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_9_SPB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_9_SPB_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_9_SPB_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_9_SPB_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_9_SPB_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_9_SPE_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_9_SPE_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_9_SPE_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_9_SPE_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_9_SPE_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_9_SPE_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_9_SPC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_9_SPC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_9_SPC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_9_SPC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_9_SPC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_9_SPC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_9_SFB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_9_SFB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(44, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_9_SFE_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_9_SFE_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(46, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_9_SFC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_9_SFC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(47, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_10_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_10_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_10_MCF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_10_MCF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_10_ETM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_10_ETM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_10_ETM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_10_ETM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(59, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_10_ETM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_10_ETM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(59, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_10_ETH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_10_ETH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(19, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_10_ETH_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_10_ETH_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(23, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_10_ETH_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_10_ETH_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(23, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_10_STM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_10_STM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(26, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_10_STM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_10_STM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(59, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_10_STM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_10_STM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(59, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_10_STH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_10_STH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(35, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_10_STH_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_10_STH_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(23, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_10_STH_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_10_STH_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(23, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_10_PER_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_10_PER_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_10_RCM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_10_RCM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(44, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_01_10_TPD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_01_10_TPD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(47, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_11_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_11_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_11_MCF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_11_MCF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_11_IRC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_11_IRC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_11_FBC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_11_FBC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_11_GMT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_11_GMT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(15, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_12_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_12_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_12_MCF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_12_MCF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_12_CW2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_12_CW2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(27, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_12_CW1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_12_CW1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_12_WOD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_12_WOD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_12_BS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_12_BS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(33, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_12_SPS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_12_SPS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(35, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_12_PIR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_12_PIR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(36, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_12_OBS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_12_OBS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(38, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_12_RCM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_12_RCM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(44, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_12_SPV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_12_SPV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(46, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_12_TPV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_12_TPV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(47, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_12_TSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_12_TSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(48, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_12_TSP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_12_TSP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_12_TSP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_12_TSP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_12_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_12_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(56, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_12_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_12_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_12_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_12_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_MCF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_MCF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_PSL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_PSL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_DCL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_DCL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_DTL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_DTL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_TPL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_TPL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_OBL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_OBL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_RCI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_RCI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_DC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_DC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_TS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_TS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(27, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_DST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_DST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_TN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_TN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_DAY_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_DAY_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_DAY_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_DAY_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(31, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_DAY_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_DAY_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(31, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_MON_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_MON_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(37, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_MON_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_MON_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(12, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_MON_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_MON_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(12, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_YR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_YR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(41, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_YR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_YR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(127, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_YR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_YR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(2000, scaleMin);
            Assert.Equal(2127, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_MIN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_MIN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(48, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_MIN_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_MIN_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(59, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_MIN_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_MIN_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(59, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_HR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_HR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(56, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_HR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_HR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(23, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_HR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_HR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(23, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_13_DTU_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_13_DTU_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(63, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_14_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_14_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_14_MCF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_14_MCF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_14_SPE_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_14_SPE_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_14_SPE_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_14_SPE_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_14_SPE_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_14_SPE_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_14_SPC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_14_SPC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_14_SPC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_14_SPC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_14_SPC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_14_SPC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_14_SFE_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_14_SFE_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(46, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_02_14_SFC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_02_14_SFC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(47, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_15_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_15_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_16_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_16_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_HUM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_HUM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(200, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_HUM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_HUM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_WOD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_WOD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_OBS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_OBS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(18, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_RCM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_RCM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(23, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_TMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_TMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(500, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_TMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_TMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_PIR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_PIR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(35, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_FS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_FS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(37, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_TSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_TSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_TSP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_TSP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_TSP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_TSP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_TSPR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_TSPR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(48, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_TSPR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_TSPR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(40, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_TSPR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_TSPR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-10, scaleMin);
            Assert.Equal(10, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_AV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_AV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(56, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_AV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_AV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_17_AV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_17_AV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_UITY_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_UITY_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_MW_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_MW_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(14, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_DC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_DC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_IEC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_IEC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(20, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_PHS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_PHS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_HCS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_HCS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(26, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_SMS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_SMS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_DST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_DST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_AL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_AL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_BS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_BS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(35, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_SPS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_SPS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(38, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_SPB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_SPB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_SPB_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_SPB_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_SPB_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_SPB_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_SPE_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_SPE_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(48, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_SPE_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_SPE_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_SPE_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_SPE_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_SPC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_SPC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(56, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_SPC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_SPC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_18_SPC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_18_SPC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_VP1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_VP1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_VP1_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_VP1_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_VP1_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_VP1_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_VP2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_VP2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_VP2_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_VP2_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_VP2_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_VP2_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_VP3_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_VP3_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_VP3_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_VP3_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_VP3_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_VP3_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_VP4_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_VP4_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_VP4_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_VP4_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_VP4_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_VP4_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_AT1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_AT1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_AT1_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_AT1_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(14, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_AT1_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_AT1_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(70, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_AT2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_AT2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(44, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_AT2_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_AT2_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(14, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_AT2_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_AT2_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(70, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_AT3_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_AT3_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(48, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_AT3_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_AT3_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(14, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_AT3_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_AT3_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(70, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_AT4_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_AT4_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(52, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_AT4_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_AT4_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(14, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_AT4_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_AT4_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(70, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_AS1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_AS1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(56, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_AS2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_AS2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(60, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_AS3_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_AS3_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(64, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_19_AS4_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_19_AS4_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(68, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPRR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPRR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPRR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPRR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(20, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPRR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPRR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(10, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPBUL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPBUL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPBUL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPBUL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPBUL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPBUL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPBLL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPBLL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPBLL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPBLL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPBLL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPBLL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPEUL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPEUL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPEUL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPEUL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPEUL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPEUL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPELL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPELL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPELL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPELL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPELL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPELL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPCUL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPCUL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(48, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPCUL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPCUL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPCUL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPCUL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPCLL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPCLL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(56, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPCLL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPCLL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_20_SPCLL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_20_SPCLL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_PSL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_PSL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_TSL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_TSL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_DCL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_DCL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_DTL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_DTL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(14, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_TPL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_TPL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_OBL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_OBL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(18, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_SPL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_SPL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(20, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_FSL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_FSL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(22, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_HFL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_HFL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_SBA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_SBA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(26, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_SEM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_SEM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_MWS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_MWS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_MWC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_MWC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_MWH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_MWH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(35, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_SPM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_SPM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(38, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_DCTL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_DCTL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_RCI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_RCI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(50, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_TS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_TS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(54, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_HC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_HC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(56, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_KL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_KL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(59, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_21_WDST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_21_WDST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(61, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_22_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_22_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_22_TN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_22_TN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_22_DAY_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_22_DAY_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_22_DAY_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_22_DAY_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(31, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_22_DAY_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_22_DAY_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(31, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_22_MON_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_22_MON_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(20, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_22_MON_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_22_MON_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(12, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_22_MON_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_22_MON_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(12, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_22_YR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_22_YR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_22_YR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_22_YR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(127, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_22_YR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_22_YR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(2000, scaleMin);
            Assert.Equal(2127, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_22_MIN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_22_MIN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(34, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_22_MIN_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_22_MIN_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(59, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_22_MIN_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_22_MIN_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(59, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_22_DTU_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_22_DTU_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(42, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_22_HR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_22_HR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(43, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_22_HR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_22_HR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(23, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_22_HR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_22_HR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(23, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_23_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_23_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_23_TMPO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_23_TMPO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(23, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_23_TMPO_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_23_TMPO_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(500, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_23_TMPO_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_23_TMPO_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_24_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_24_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_24_TSPO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_24_TSPO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_24_TSPO_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_24_TSPO_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_24_TSPO_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_24_TSPO_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_25_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_25_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_25_TSPRO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_25_TSPRO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_25_TSPRO_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_25_TSPRO_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(40, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_25_TSPRO_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_25_TSPRO_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-10, scaleMin);
            Assert.Equal(10, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_26_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_26_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_26_EXVSU_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_26_EXVSU_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(18, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_26_EXV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_26_EXV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(22, propOffset);
            Assert.Equal(10, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_26_EXV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_26_EXV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(1023, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_26_EXV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_26_EXV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(1023, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_27_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_27_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_27_HUMO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_27_HUMO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_27_HUMO_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_27_HUMO_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(200, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_27_HUMO_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_27_HUMO_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_28_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_28_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_28_FSO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_28_FSO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_29_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_29_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_29_RCMO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_29_RCMO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_30_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_30_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_30_WODO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_30_WODO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_31_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_31_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_31_PIRO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_31_PIRO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_32_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_32_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_32_OBO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_32_OBO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_33_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_33_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_33_SDAS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_33_SDAS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_34_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_34_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_34_ALO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_34_ALO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_35_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_35_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_35_SDCH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_35_SDCH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_36_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_36_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_36_SDSM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_36_SDSM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_37_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_37_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_37_DCO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_37_DCO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_38_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_38_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_38_DSTO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_38_DSTO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_39_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_39_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_39_UDEC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_39_UDEC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_40_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_40_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_40_SPBO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_40_SPBO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_40_SPBO_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_40_SPBO_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_40_SPBO_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_40_SPBO_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_41_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_41_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_41_SPCO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_41_SPCO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_41_SPCO_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_41_SPCO_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_41_SPCO_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_41_SPCO_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_42_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_42_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_42_SPEO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_42_SPEO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_42_SPEO_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_42_SPEO_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_42_SPEO_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_42_SPEO_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_43_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_43_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_43_SPBULO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_43_SPBULO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_43_SPBULO_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_43_SPBULO_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_43_SPBULO_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_43_SPBULO_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_44_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_44_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_44_SPBLLO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_44_SPBLLO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_44_SPBLLO_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_44_SPBLLO_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_44_SPBLLO_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_44_SPBLLO_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_45_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_45_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_45_SPEULO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_45_SPEULO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_45_SPEULO_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_45_SPEULO_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_45_SPEULO_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_45_SPEULO_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_46_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_46_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_46_SPELLO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_46_SPELLO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_46_SPELLO_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_46_SPELLO_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_46_SPELLO_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_46_SPELLO_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_47_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_47_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_47_SPCULO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_47_SPCULO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_47_SPCULO_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_47_SPCULO_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_47_SPCULO_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_47_SPCULO_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_48_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_48_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_48_SPCLLO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_48_SPCLLO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_48_SPCLLO_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_48_SPCLLO_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_48_SPCLLO_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_48_SPCLLO_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(50, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_49_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_49_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_49_SPRRO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_49_SPRRO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_49_SPRRO_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_49_SPRRO_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(20, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_49_SPRRO_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_49_SPRRO_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(10, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_50_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_50_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_10_30_50_SEMO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_10_30_50_SEMO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_1_SPT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_1_SPT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_1_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_1_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_2_SPT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_2_SPT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_2_DHS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_2_DHS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_2_DCS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_2_DCS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(2, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_2_SSW_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_2_SSW_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(3, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_2_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_2_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_2_OSO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_2_OSO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_2_OSO_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_2_OSO_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_2_OSO_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_2_OSO_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_2_BSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_2_BSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_2_COA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_2_COA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_2_OFS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_2_OFS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_2_OOS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_2_OOS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_3_SPT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_3_SPT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_3_TT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_3_TT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_3_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_3_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_3_TEMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_3_TEMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_3_TEMP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_3_TEMP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_3_TEMP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_3_TEMP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_3_HUMI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_3_HUMI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_3_HUMI_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_3_HUMI_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_3_HUMI_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_3_HUMI_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_3_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_3_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_3_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_3_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_3_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_3_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_3_IBS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_3_IBS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_3_BSB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_3_BSB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_3_FS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_3_FS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(44, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_01_3_OS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_01_3_OS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(47, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_4_TTP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_4_TTP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_4_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_4_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_5_WMS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_5_WMS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_5_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_5_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_5_RCPM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_5_RCPM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_5_BS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_5_BS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_5_FC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_5_FC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(14, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_5_RTEMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_5_RTEMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_5_ACCTST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_5_ACCTST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(23, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_5_FHO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_5_FHO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_5_FHTSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_5_FHTSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_5_ACMSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_5_ACMSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_5_ACFSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_5_ACFSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_5_ACTS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_5_ACTS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(35, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_5_FVO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_5_FVO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_5_FVSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_5_FVSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(41, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_6_WMS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_6_WMS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_6_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_6_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_6_RCPM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_6_RCPM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_6_BS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_6_BS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_6_FC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_6_FC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(14, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_6_RTEMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_6_RTEMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_6_ACCTST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_6_ACCTST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(23, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_7_WMS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_7_WMS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_7_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_7_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_7_RCPM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_7_RCPM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_7_FVO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_7_FVO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_7_FVSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_7_FVSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_7_FHO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_7_FHO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_7_FHTSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_7_FHTSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_7_ACMSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_7_ACMSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(22, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_7_ACFSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_7_ACFSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_7_ACTS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_7_ACTS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(27, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_8_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_8_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_8_RCPM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_8_RCPM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_8_TSSO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_8_TSSO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_8_ATC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_8_ATC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_8_SSC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_8_SSC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_8_ACT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_8_ACT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(19, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_8_ACFO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_8_ACFO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_8_LOCK_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_8_LOCK_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(23, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_8_FVS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_8_FVS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_8_TSCV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_8_TSCV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(27, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_8_ACCTST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_8_ACCTST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_8_FHTSR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_8_FHTSR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(34, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_8_ACTSR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_8_ACTSR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(37, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_9_MCF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_9_MCF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_9_RIN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_9_RIN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_9_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_9_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(2, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_9_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_9_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_10_MCF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_10_MCF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_10_RIN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_10_RIN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_10_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_10_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(2, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_10_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_10_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_10_ES_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_10_ES_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_10_FHO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_10_FHO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_10_FHTSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_10_FHTSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_10_ACMSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_10_ACMSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_10_ACFSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_10_ACFSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(18, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_10_FVSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_10_FVSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_10_ACTS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_10_ACTS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_10_FVO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_10_FVO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_10_WMS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_10_WMS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(30, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_10_RTOV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_10_RTOV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_11_MCF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_11_MCF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_11_RIN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_11_RIN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_11_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_11_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(2, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_11_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_11_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_11_ES_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_11_ES_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_11_RTOV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_11_RTOV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_12_MCF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_12_MCF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_12_RIN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_12_RIN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_12_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_12_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(2, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_12_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_12_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_12_FVO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_12_FVO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_12_FVSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_12_FVSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_12_WMS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_12_WMS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(14, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_12_FHO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_12_FHO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_12_FHTSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_12_FHTSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_12_ACMSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_12_ACMSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(22, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_12_ACFSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_12_ACFSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_12_ACTS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_12_ACTS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(27, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_13_MCF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_13_MCF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_13_RIN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_13_RIN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_13_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_13_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(2, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_13_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_13_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_13_RCPM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_13_RCPM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_13_TSSO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_13_TSSO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_13_ATC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_13_ATC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_13_SSC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_13_SSC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_13_ACFO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_13_ACFO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(19, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_13_LOCK_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_13_LOCK_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_13_ACCTST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_13_ACCTST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(22, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_13_ACT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_13_ACT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(23, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_13_FVS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_13_FVS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_13_TSCV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_13_TSCV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_13_FHTSR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_13_FHTSR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(34, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_11_20_13_ACTSR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_11_20_13_ACTSR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(37, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_00_1_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_00_1_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_00_1_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_00_1_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_01_2_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_01_2_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_01_2_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_01_2_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_01_2_BA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_01_2_BA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_01_2_BB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_01_2_BB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(19, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_02_3_VOC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_02_3_VOC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_03_4_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_03_4_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_03_4_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_03_4_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_03_4_VOC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_03_4_VOC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_04_5_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_04_5_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_04_5_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_04_5_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_04_5_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_04_5_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(17, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_05_6_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_05_6_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_05_6_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_05_6_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_05_6_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_05_6_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(17, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_05_6_VOC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_05_6_VOC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(34, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_06_7_CO2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_06_7_CO2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_07_8_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_07_8_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_07_8_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_07_8_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_07_8_CO2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_07_8_CO2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_08_9_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_08_9_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_08_9_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_08_9_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_08_9_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_08_9_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(17, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_08_9_CO2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_08_9_CO2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(34, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_09_10_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_09_10_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_09_10_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_09_10_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_09_10_VOC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_09_10_VOC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_09_10_CO2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_09_10_CO2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0A_11_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0A_11_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0A_11_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0A_11_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0A_11_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0A_11_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(17, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0A_11_VOC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0A_11_VOC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(34, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0A_11_CO2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0A_11_CO2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(42, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0B_12_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0B_12_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0B_12_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0B_12_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0B_12_VOC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0B_12_VOC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0C_13_CO2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0C_13_CO2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0D_14_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0D_14_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0D_14_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0D_14_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0D_14_VOC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0D_14_VOC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0D_14_BA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0D_14_BA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0E_15_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0E_15_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0E_15_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0E_15_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0E_15_VOC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0E_15_VOC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0E_15_CO2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0E_15_CO2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0E_15_BA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0E_15_BA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(33, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0F_16_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0F_16_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0F_16_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0F_16_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_0F_16_BAR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_0F_16_BAR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_10_17_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_10_17_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_10_17_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_10_17_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_10_17_VOC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_10_17_VOC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_10_17_PR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_10_17_PR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_1A_18_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_1A_18_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_1A_18_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_1A_18_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_1A_18_ES_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_1A_18_ES_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_1B_19_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_1B_19_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_1B_19_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_1B_19_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_1B_19_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_1B_19_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(17, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_1B_19_ES_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_1B_19_ES_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(34, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_1C_20_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_1C_20_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_1C_20_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_1C_20_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_1C_20_ES_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_1C_20_ES_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_1C_20_BAR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_1C_20_BAR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(19, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_1D_21_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_1D_21_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_1D_21_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_1D_21_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_1D_21_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_1D_21_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(17, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_1D_21_ES_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_1D_21_ES_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(34, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_1D_21_BAR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_1D_21_BAR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(36, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_20_22_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_20_22_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_20_22_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_20_22_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_20_22_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_20_22_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(17, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_20_22_ES_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_20_22_ES_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(34, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_21_23_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_21_23_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_21_23_ES_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_21_23_ES_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_22_24_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_22_24_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_22_24_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_22_24_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_22_24_ES_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_22_24_ES_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_23_25_TMP9_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_23_25_TMP9_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_23_25_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_23_25_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(17, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_23_25_ES_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_23_25_ES_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(26, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_24_26_ILL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_24_26_ILL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(17, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_24_26_ES_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_24_26_ES_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_30_27_SMA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_30_27_SMA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_30_27_ES_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_30_27_ES_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_30_27_RPLT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_30_27_RPLT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(15, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_30_27_TMP8_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_30_27_TMP8_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(23, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_30_27_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_30_27_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_30_27_HCI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_30_27_HCI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(39, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_30_27_IAQTH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_30_27_IAQTH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(41, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_31_28_COA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_31_28_COA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_31_28_ES_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_31_28_ES_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_31_28_RPLT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_31_28_RPLT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(15, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_31_28_CO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_31_28_CO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(23, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_31_28_TMP8_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_31_28_TMP8_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_31_28_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_31_28_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(39, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_31_28_HCI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_31_28_HCI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(47, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_31_28_IAQTH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_31_28_IAQTH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(49, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_14_31_28_IAQCO_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_31_28_IAQCO_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(52, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_00_1_OM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_00_1_OM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_00_1_TL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_00_1_TL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(5, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_00_1_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_00_1_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(7, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_00_1_HC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_00_1_HC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_00_1_RSR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_00_1_RSR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_00_1_RS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_00_1_RS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_00_1_HT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_00_1_HT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_00_1_FS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_00_1_FS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_00_2_OMS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_00_2_OMS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_00_2_SI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_00_2_SI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_00_2_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_00_2_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(7, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_00_2_HCS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_00_2_HCS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_00_2_RSR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_00_2_RSR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_00_2_RSS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_00_2_RSS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_00_2_HUM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_00_2_HUM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_00_2_FSS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_00_2_FSS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_01_3_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_01_3_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(7, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_01_3_RSR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_01_3_RSR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_01_3_RS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_01_3_RS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_01_3_FS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_01_3_FS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_01_4_OMS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_01_4_OMS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_01_4_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_01_4_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(7, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_01_4_RSR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_01_4_RSR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_01_4_RSS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_01_4_RSS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_01_4_FSS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_01_4_FSS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_02_5_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_02_5_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(7, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_02_5_RSR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_02_5_RSR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_02_5_RS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_02_5_RS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_02_5_FS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_02_5_FS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_02_6_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_02_6_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(7, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_02_6_HCS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_02_6_HCS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_02_6_RSR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_02_6_RSR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_02_6_RSS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_02_6_RSS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_20_02_6_FSS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_20_02_6_FSS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_1_PERIOD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_1_PERIOD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_1_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_1_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_1_VTYP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_1_VTYP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_1_HCH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_1_HCH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_1_RIN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_1_RIN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_1_POS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_1_POS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_1_POS_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_1_POS_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_1_POS_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_1_POS_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_2_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_2_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_2_HCH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_2_HCH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_3_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_3_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_3_STATUS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_3_STATUS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_3_HCH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_3_HCH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_3_POS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_3_POS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_3_POS_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_3_POS_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_3_POS_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_3_POS_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_3_TEMPRET_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_3_TEMPRET_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_3_TEMPRET_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_3_TEMPRET_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(180, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_3_TEMPRET_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_3_TEMPRET_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(90, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_4_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_4_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_4_STATUS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_4_STATUS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_4_HCH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_4_HCH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_4_TSUP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_4_TSUP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_4_TSUP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_4_TSUP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(180, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_4_TSUP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_4_TSUP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(90, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_4_TRET_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_4_TRET_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_4_TRET_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_4_TRET_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(180, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_4_TRET_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_4_TRET_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(90, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_5_RM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_5_RM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_5_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_5_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_5_BUS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_5_BUS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_5_MCH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_5_MCH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_5_MCH_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_5_MCH_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(30, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_5_MCH_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_5_MCH_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(30, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_5_UNIT1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_5_UNIT1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(18, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_5_UNIT2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_5_UNIT2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_5_ADDR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_5_ADDR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_5_ADDR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_5_ADDR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_5_ADDR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_5_ADDR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(250, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_6_RM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_6_RM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_6_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_6_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_6_BUS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_6_BUS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_6_MCH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_6_MCH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_6_MCH_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_6_MCH_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(30, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_6_MCH_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_6_MCH_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(30, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_6_UNIT1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_6_UNIT1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(18, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_6_UNIT2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_6_UNIT2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_6_FACP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_6_FACP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_6_NOP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_6_NOP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(26, propOffset);
            Assert.Equal(14, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_6_RST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_6_RST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(32, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_7_RM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_7_RM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_7_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_7_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_7_BUS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_7_BUS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_7_MCH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_7_MCH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_7_MCH_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_7_MCH_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(30, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_7_MCH_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_7_MCH_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(30, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_7_UNIT1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_7_UNIT1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(18, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_7_UNIT2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_7_UNIT2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_7_PROT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_7_PROT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_8_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_8_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_8_BUS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_8_BUS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_8_MCH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_8_MCH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_9_MSTAT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_9_MSTAT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_9_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_9_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_9_BUS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_9_BUS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_9_MCH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_9_MCH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_9_MCH_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_9_MCH_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(30, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_9_MCH_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_9_MCH_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(30, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_9_VSEL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_9_VSEL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(19, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_9_VUNIT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_9_VUNIT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_9_VAL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_9_VAL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(32, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_9_VAL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_9_VAL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4294967295, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_30_00_9_VAL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_30_00_9_VAL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(4294967295, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_1_RM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_1_RM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_1_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_1_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_1_BUS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_1_BUS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_1_MCH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_1_MCH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_1_MCH_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_1_MCH_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(30, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_1_MCH_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_1_MCH_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(30, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_1_UNIT1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_1_UNIT1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(18, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_1_UNIT2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_1_UNIT2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_1_ADDR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_1_ADDR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_1_ADDR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_1_ADDR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(250, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_1_ADDR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_1_ADDR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(250, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_2_RM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_2_RM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_2_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_2_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_2_BUS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_2_BUS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_2_MCH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_2_MCH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_2_MCH_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_2_MCH_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(30, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_2_MCH_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_2_MCH_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(30, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_2_UNIT1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_2_UNIT1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(18, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_2_UNIT2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_2_UNIT2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_2_FACP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_2_FACP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_2_NOP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_2_NOP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(26, propOffset);
            Assert.Equal(14, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_2_RST_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_2_RST_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(32, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_3_RM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_3_RM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_3_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_3_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_3_BUS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_3_BUS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_3_MCH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_3_MCH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_3_MCH_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_3_MCH_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(30, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_3_MCH_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_3_MCH_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(30, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_3_UNIT1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_3_UNIT1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(18, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_3_UNIT2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_3_UNIT2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_3_PROT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_3_PROT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_4_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_4_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_4_BUS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_4_BUS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_4_MCH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_4_MCH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_5_MSTAT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_5_MSTAT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_5_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_5_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_5_BUS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_5_BUS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_5_MCH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_5_MCH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_5_MCH_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_5_MCH_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(30, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_5_MCH_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_5_MCH_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(30, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_5_VSEL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_5_VSEL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(19, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_5_VUNIT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_5_VUNIT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_5_VAL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_5_VAL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(32, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_5_VAL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_5_VAL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4294967295, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_31_00_5_VAL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_31_00_5_VAL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(4294967295, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_32_00_1_PF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_00_1_PF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_32_00_1_DIV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_00_1_DIV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_32_00_1_CH1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_00_1_CH1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(12, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_32_00_1_CH1_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_00_1_CH1_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4095, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_32_00_1_CH1_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_00_1_CH1_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_32_01_2_PF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_01_2_PF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_32_01_2_DIV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_01_2_DIV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_32_01_2_CH1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_01_2_CH1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(12, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_32_01_2_CH1_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_01_2_CH1_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4095, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_32_01_2_CH1_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_01_2_CH1_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_32_01_2_CH2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_01_2_CH2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(20, propOffset);
            Assert.Equal(12, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_32_01_2_CH2_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_01_2_CH2_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4095, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_32_01_2_CH2_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_01_2_CH2_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_32_02_3_PF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_02_3_PF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_32_02_3_DIV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_02_3_DIV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_32_02_3_CH1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_02_3_CH1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(12, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_32_02_3_CH1_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_02_3_CH1_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4095, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_32_02_3_CH1_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_02_3_CH1_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_32_02_3_CH2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_02_3_CH2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(20, propOffset);
            Assert.Equal(12, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_32_02_3_CH2_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_02_3_CH2_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4095, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_32_02_3_CH2_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_02_3_CH2_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_32_02_3_CH3_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_02_3_CH3_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(12, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_32_02_3_CH3_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_02_3_CH3_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4095, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_32_02_3_CH3_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_32_02_3_CH3_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_1_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_1_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_1_REQ_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_1_REQ_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_1_EXT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_1_EXT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_1_EXT_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_1_EXT_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(500, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_1_EXT_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_1_EXT_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0.1, scaleMin);
            Assert.Equal(50.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_2_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_2_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_2_WOS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_2_WOS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_2_PIS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_2_PIS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(5, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_2_RTS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_2_RTS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_2_CVS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_2_CVS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(7, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_2_COS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_2_COS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_2_C2S_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_2_C2S_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_2_P1S_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_2_P1S_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_2_P2S_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_2_P2S_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(11, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_2_P10S_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_2_P10S_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_2_RAS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_2_RAS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_2_SOS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_2_SOS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(14, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_2_HYS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_2_HYS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(15, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_2_AMS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_2_AMS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_2_PRS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_2_PRS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_2_TSS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_2_TSS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(18, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_2_TNS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_2_TNS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(20, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_2_DCS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_2_DCS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(22, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_2_DGS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_2_DGS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_TPT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_TPT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_ETD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_ETD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(5, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_ETM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_ETM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_ETM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_ETM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(59, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_ETM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_ETM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(59, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_ETH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_ETH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(14, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_ETH_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_ETH_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(23, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_ETH_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_ETH_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(23, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_STD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_STD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(19, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_STM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_STM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(22, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_STM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_STM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(59, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_STM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_STM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(59, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_STH_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_STH_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_STH_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_STH_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(23, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_STH_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_STH_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(23, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_TSP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_TSP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(33, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_TSP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_TSP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(500, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_TSP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_TSP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0.1, scaleMin);
            Assert.Equal(50.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_3_CSC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_3_CSC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(42, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_4_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_4_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_4_DAY_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_4_DAY_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_4_DAY_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_4_DAY_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(31, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_4_DAY_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_4_DAY_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(31, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_4_MON_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_4_MON_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_4_MON_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_4_MON_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(12, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_4_MON_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_4_MON_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(12, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_4_YR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_4_YR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(12, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_4_YR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_4_YR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4095, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_4_YR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_4_YR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(4095, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_4_MIN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_4_MIN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_4_MIN_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_4_MIN_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(59, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_4_MIN_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_4_MIN_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(59, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_4_HR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_4_HR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_4_HR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_4_HR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(23, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_4_HR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_4_HR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(23, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_4_DAYW_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_4_DAYW_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(36, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_5_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_5_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_5_REQ_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_5_REQ_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_5_ERF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_5_ERF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_5_HTF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_5_HTF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_5_PWF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_5_PWF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_5_WOF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_5_WOF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(27, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_5_PIF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_5_PIF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(29, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_5_KLU_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_5_KLU_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_5_RTF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_5_RTF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_5_DGF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_5_DGF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(33, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_5_INT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_5_INT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(34, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_5_INT_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_5_INT_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(500, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_5_INT_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_5_INT_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0.1, scaleMin);
            Assert.Equal(50.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_6_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_6_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_6_EM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_6_EM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(24, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_6_EM_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_6_EM_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(16777215, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_6_EM_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_6_EM_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(1677721, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_6_DTS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_6_DTS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_6_DTS_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_6_DTS_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(500, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_6_DTS_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_6_DTS_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0.1, scaleMin);
            Assert.Equal(50.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_6_FWV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_6_FWV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(37, propOffset);
            Assert.Equal(10, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_6_FWV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_6_FWV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(1024, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_6_FWV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_6_FWV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(1024, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_7_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_7_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_7_CVV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_7_CVV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_7_CVV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_7_CVV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(65535, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_7_CVV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_7_CVV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(65535, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_7_VOCT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_7_VOCT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(20, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_7_VOCT_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_7_VOCT_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_7_VOCT_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_7_VOCT_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(255, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_7_C2V_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_7_C2V_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_7_C2V_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_7_C2V_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(255, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_7_C2V_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_7_C2V_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(10, scaleMin);
            Assert.Equal(2550, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_7_SOV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_7_SOV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(36, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_7_SOV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_7_SOV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(127, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_7_SOV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_7_SOV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(127, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_8_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_8_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_8_PM1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_8_PM1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_8_PM1_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_8_PM1_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(511, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_8_PM1_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_8_PM1_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(511, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_8_PM2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_8_PM2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_8_PM2_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_8_PM2_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(511, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_8_PM2_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_8_PM2_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(511, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_8_PM10_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_8_PM10_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(22, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_8_PM10_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_8_PM10_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(511, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_8_PM10_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_8_PM10_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(511, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_8_RAV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_8_RAV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(31, propOffset);
            Assert.Equal(14, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_8_RAV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_8_RAV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(16383, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_8_RAV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_8_RAV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0.01, scaleMin);
            Assert.Equal(163.83, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_9_MID_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_9_MID_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_9_AMV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_9_AMV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_9_AMV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_9_AMV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(15, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_9_AMV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_9_AMV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(15, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_9_PRV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_9_PRV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(15, propOffset);
            Assert.Equal(10, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_9_PRV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_9_PRV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(1023, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_9_PRV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_9_PRV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(500, scaleMin);
            Assert.Equal(1150, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_9_HYV_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_9_HYV_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_9_HYV_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_9_HYV_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(200, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_9_HYV_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_9_HYV_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(1, scaleMin);
            Assert.Equal(100, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_9_INT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_9_INT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(33, propOffset);
            Assert.Equal(11, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_9_INT_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_9_INT_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(1, rangeMin);
            Assert.Equal(500, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_33_00_9_INT_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_33_00_9_INT_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0.1, scaleMin);
            Assert.Equal(50.0, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_1_CHN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_1_CHN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_1_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_1_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_2_TMP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_2_TMP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_2_SP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_2_SP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(9, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_2_SP_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_2_SP_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(400, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_2_SP_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_2_SP_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_2_OPM_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_2_OPM_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(18, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_2_CHN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_2_CHN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(22, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_2_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_2_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(28, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_3_CFG_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_3_CFG_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_3_DUR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_3_DUR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(2, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_3_SHF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_3_SHF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_3_SHF_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_3_SHF_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_3_SHF_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_3_SHF_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(10, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_3_OVR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_3_OVR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(15, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_3_OVR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_3_OVR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(400, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_3_OVR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_3_OVR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_3_CHN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_3_CHN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_3_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_3_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(36, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_4_CHN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_4_CHN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_4_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_4_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_5_CFG_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_5_CFG_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_5_DUR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_5_DUR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(2, propOffset);
            Assert.Equal(6, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_5_PNL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_5_PNL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_5_PNL_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_5_PNL_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(400, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_5_PNL_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_5_PNL_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_5_SHF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_5_SHF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_5_SHF_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_5_SHF_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(100, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_5_SHF_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_5_SHF_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(10, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_5_OVR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_5_OVR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(9, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_5_OVR_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_5_OVR_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(400, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_5_OVR_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_5_OVR_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(40, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_5_CHN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_5_CHN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(33, propOffset);
            Assert.Equal(5, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_34_00_5_CMD_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_34_00_5_CMD_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(44, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_40_00_1_OUTEN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_40_00_1_OUTEN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_40_00_1_DRA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_40_00_1_DRA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_40_00_1_DHAR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_40_00_1_DHAR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(2, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_40_00_1_OCC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_40_00_1_OCC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(3, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_40_00_1_SREAS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_40_00_1_SREAS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(5, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_40_00_1_MI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_40_00_1_MI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_40_00_1_DLVL_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_40_00_1_DLVL_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_40_01_2_OUTEN_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_40_01_2_OUTEN_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_40_01_2_DRA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_40_01_2_DRA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(1, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_40_01_2_DHAR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_40_01_2_DHAR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(2, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_40_01_2_OCC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_40_01_2_OCC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(3, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_40_01_2_SREAS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_40_01_2_SREAS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(5, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_40_01_2_MI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_40_01_2_MI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_40_01_2_DLVLR_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_40_01_2_DLVLR_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_40_01_2_DLVLG_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_40_01_2_DLVLG_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_40_01_2_DLVLB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_40_01_2_DLVLB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(8, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_1_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_1_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_1_RMT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_1_RMT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(5, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_2_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_2_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_2_DOMC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_2_DOMC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_2_OMC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_2_OMC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(8, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_2_HBC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_2_HBC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(10, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_2_TOMC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_2_TOMC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_2_COT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_2_COT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_2_HT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_2_HT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_2_AQT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_2_AQT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(33, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_2_RTT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_2_RTT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(41, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_OMS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_OMS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(4, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_SMS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_SMS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(12, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_HBS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_HBS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(13, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_SFP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_SFP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(14, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_EFP_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_EFP_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(15, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_DMS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_DMS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_CPS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_CPS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(17, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_OHS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_OHS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(18, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_SHS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_SHS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(19, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_DHS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_DHS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(20, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_TOMS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_TOMS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(21, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_FMS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_FMS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(22, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_WTPS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_WTPS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(23, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_RTCS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_RTCS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(24, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_AQS1_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_AQS1_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(25, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_MSS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_MSS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(1, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_AQS2_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_AQS2_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(33, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_OUTT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_OUTT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(40, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_OUTT_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_OUTT_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(127, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_OUTT_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_OUTT_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-64, scaleMin);
            Assert.Equal(63, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_SPLYT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_SPLYT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(47, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_SPLYT_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_SPLYT_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(127, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_SPLYT_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_SPLYT_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-64, scaleMin);
            Assert.Equal(63, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_INT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_INT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(54, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_INT_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_INT_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(127, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_INT_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_INT_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-64, scaleMin);
            Assert.Equal(63, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_EXHT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_EXHT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(61, propOffset);
            Assert.Equal(7, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_EXHT_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_EXHT_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(127, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_EXHT_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_EXHT_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(-64, scaleMin);
            Assert.Equal(63, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_SPLYFF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_SPLYFF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(68, propOffset);
            Assert.Equal(10, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_SPLYFF_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_SPLYFF_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(1023, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_SPLYFF_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_SPLYFF_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(1023, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_EXHFF_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_EXHFF_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(78, propOffset);
            Assert.Equal(10, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_EXHFF_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_EXHFF_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(1023, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_EXHFF_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_EXHFF_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(1023, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_SPLYFS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_SPLYFS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(88, propOffset);
            Assert.Equal(12, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_SPLYFS_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_SPLYFS_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4095, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_SPLYFS_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_SPLYFS_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(4095, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_EXHFS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_EXHFS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(100, propOffset);
            Assert.Equal(12, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_EXHFS_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_EXHFS_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4095, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_3_EXHFS_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_3_EXHFS_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(4095, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_4_MT_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_4_MT_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(3, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_4_SVI_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_4_SVI_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(4, propOffset);
            Assert.Equal(12, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_4_SVI_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_4_SVI_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(4095, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_4_SVI_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_4_SVI_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(4095, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_4_OHC_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_4_OHC_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(16, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_4_OHC_Range()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_4_OHC_Guid);

            var rangeMin = dataField.GetPropertyValueLong("enocean-range-min");
            var rangeMax = dataField.GetPropertyValueLong("enocean-range-max");

            Assert.Equal(0, rangeMin);
            Assert.Equal(65535, rangeMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_4_OHC_Scale()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_4_OHC_Guid);

            var scaleMin = dataField.GetPropertyValueDouble("enocean-scale-min");
            var scaleMax = dataField.GetPropertyValueDouble("enocean-scale-max");

            Assert.Equal(0, scaleMin);
            Assert.Equal(196605, scaleMax);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_4_DIS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_4_DIS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(32, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_4_DOS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_4_DOS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(48, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_4_IMS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_4_IMS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(64, propOffset);
            Assert.Equal(16, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_50_00_4_FS_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_50_00_4_FS_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(80, propOffset);
            Assert.Equal(32, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_A0_01_1_FDB_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_A0_01_1_FDB_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_A0_01_2_REQ_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_A0_01_2_REQ_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(6, propOffset);
            Assert.Equal(2, propSize);
        }

        [Fact]
        public void Test_DataFieldD2_B0_51_1_WA_DefaultProps()
        {
            var dataField = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_B0_51_1_WA_Guid);

            var propOffset = dataField.GetPropertyValueInt("enocean-bitoffset");
            var propSize = dataField.GetPropertyValueInt("enocean-length");

            Assert.Equal(0, propOffset);
            Assert.Equal(8, propSize);
        }

                }
            }
            