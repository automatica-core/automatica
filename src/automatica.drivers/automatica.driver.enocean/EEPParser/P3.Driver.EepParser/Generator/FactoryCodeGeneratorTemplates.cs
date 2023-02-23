using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Driver.EepParser.Generator
{
    public static class FactoryCodeGeneratorTemplates
    {

        public static readonly string ClassNameTemplate = @"
        using System;
        using System.Collections.Generic;
        using System.Diagnostics.CodeAnalysis;
        using System.Text;
        using Automatica.Core.Base.Templates;
        using Automatica.Core.EF.Models;
        using P3.Driver.EnOcean.DriverFactory.Templates;
        using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

        namespace P3.Driver.EnOcean.DriverFactory
        {{
                [SuppressMessage(""ReSharper"", ""InconsistentNaming"")]
                public class EnOceanRorg{0}Data : EnOceanRorgData
                {{
                    {1}
                }}
        }}";

        public static readonly string AddRorgMethodName = @"AddRorg{0}Templates";
        public static readonly string RorgName = "ENOCEAN.RORG_{0}";
        public static readonly string RorgStaticGuid = @"public static readonly Guid Rorg{0}Guid = new Guid(""{1}"");";

        public static readonly string AddRorgTemplate =  @"
        public static void "+AddRorgMethodName+ @"(INodeTemplateFactory factory, EnOceanTemplateFactory enoceanFactory)
        {{
            var nodeGuid = Rorg{0}Guid;

            Guid interfaceGuid = GenerateNewGuid(nodeGuid, 1);
            factory.CreateInterfaceType(interfaceGuid, """ + RorgName + @".NAME"", """ + RorgName + @".DESCRIPTION"",Int32.MaxValue, int.MaxValue, false);

           
            factory.CreateNodeTemplate(nodeGuid, """ + RorgName + @".NAME"", """ + RorgName + @".DESCRIPTION"",
                ""enocean-rorg-{0:L}"", EnOceanDriverFactory.InterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            enoceanFactory.AddTemplate(0x{0}, nodeGuid);
            {1}
        }}";

        public static readonly string AddFunctionMethodName = @"AddFunction{0}_{1}Templates";
        public static readonly string FunctionName = "ENOCEAN.RORG_{0}.FUNCTION_{1}";
        public static readonly string FunctionStaticGuid = @"public static readonly Guid Function{0}_{1}Guid = new Guid(""{2}"");";

        public static readonly string AddFunctionTemplate = @"
        private static void "+ AddFunctionMethodName + @"(INodeTemplateFactory factory, Guid rorgInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {{
            Guid interfaceGuid = GenerateNewGuid(Function{0}_{1}Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, """ + FunctionName + @".NAME"", """ + FunctionName + @".DESCRIPTION"", Int32.MaxValue, int.MaxValue, false);

            factory.CreateNodeTemplate(Function{0}_{1}Guid, """ + FunctionName + @".NAME"", """ + FunctionName + @".DESCRIPTION"",
                ""enocean-function-{1:L}"", rorgInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            enoceanFactory.AddTemplate(0x{0}, Function{0}_{1}Guid);
            {2}
        }}";


        public static readonly string AddTypesMethodName = @"AddTypes{0}_{1}_{2}Templates";
        public static readonly string TypeStaticGuid = @"public static readonly Guid Type{0}_{1}_{2}Guid = new Guid(""{3}"");";
        public static readonly string TypeName = "ENOCEAN.RORG_{0}.FUNCTION_{1}.TYPE_{2}";
        public static readonly string AddTypesTemplate = @"
        private static void " + AddTypesMethodName + @"(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {{
            Guid interfaceGuid = GenerateNewGuid(Type{0}_{1}_{2}Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, """ + TypeName + @".NAME"", """ + TypeName + @".DESCRIPTION"", Int32.MaxValue, int.MaxValue, false);

            var nodeGuid = Type{0}_{1}_{2}Guid;
            
            factory.CreateNodeTemplate(nodeGuid, """ + TypeName + @".NAME"", """ + TypeName + @".DESCRIPTION"",
                ""enocean-type-{1:L}"", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), ""ENOCEAN.SERIAL_NUMBER.NAME"", ""ENOCEAN.SERIAL_NUMBER.DESCRIPTION"",
                ""enocean-serialnumber"", PropertyTemplateType.Text, nodeGuid, ""COMMON.CATEGORY.ADDRESS"", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0x{0}, nodeGuid);
            {3}

        }}";

        public static readonly string AddDataFieldMethodName = @"AddDataField{0}_{1}_{2}_{3}Templates";
        public static readonly string DataFieldName = "ENOCEAN.RORG_{0}.FUNCTION_{1}.TYPE_{2}.SHORTCUT_{3}";
        public static readonly string DataFieldStaticGuid = @"public static readonly Guid DataField{0}_{1}_{2}_{3}_Guid = new Guid(""{4}"");";

        public static readonly string AddDataFieldTemplate = @"
        private static void " + AddDataFieldMethodName + @"(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {{
            var nodeGuid = DataField{0}_{1}_{2}_{3}_Guid;
            factory.CreateNodeTemplate(nodeGuid, """ + DataFieldName + @".NAME"", """ + DataFieldName + @".DESCRIPTION"",
                ""enocean-shortcut-{3:L}"", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), {7}, true, false, true, false,
                {8}, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), """ + DataFieldName + @"{3}.OFFSET"", """+ DataFieldName + @"{3}.OFFSET"",
                ""enocean-bitoffset"", PropertyTemplateType.Integer, nodeGuid, ""ENOCEAN"", false, true, null, ""{4}"", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), """ + DataFieldName + @"{3}.LENGTH"", """+ DataFieldName + @"{3}.LENGTH"",
                ""enocean-length"", PropertyTemplateType.Integer, nodeGuid, ""ENOCEAN"", false, true, null, ""{5}"", 0, 0);

            {6}           
        }}";

        public static readonly string AddUnitDataField = @"
                factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 3), """ + DataFieldName + @"{3}.UNIT"", """+ DataFieldName + @"{3}.UNIT"",
                    ""enocean-unit"", PropertyTemplateType.Text, nodeGuid, ""ENOCEAN"", false, true, null, ""{4}"", 0, 0);";

        public static readonly string AddRangeDataField = @"
                factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 4), """ + DataFieldName + @"{3}.RANGE.MIN"", """+ DataFieldName + @"{3}.RANGE.MIN"",
                    ""enocean-range-min"", PropertyTemplateType.Long, nodeGuid, ""ENOCEAN"", false, true, null, ""{4}"", 0, 0);
                factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 5), """ + DataFieldName + @"{3}.RANGE.MAX"", """+ DataFieldName + @"{3}.RANGE.MAX"",
                    ""enocean-range-max"", PropertyTemplateType.Long, nodeGuid, ""ENOCEAN"", false, true, null, ""{5}"", 0, 0);";

        public static readonly string AddScaleDataField = @"
                 factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 6), """ + DataFieldName + @"{3}.SCALE.MIN"", """+ DataFieldName + @"{3}.SCALE.MIN"",
                    ""enocean-scale-min"", PropertyTemplateType.Numeric, nodeGuid, ""ENOCEAN"", false, true, null, ""{4}"", 0, 0);
                factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 7), """ + DataFieldName + @"{3}.SCALE.MAX"", """+ DataFieldName + @"{3}.SCALE.MAX"",
                    ""enocean-scale-max"", PropertyTemplateType.Numeric, nodeGuid, ""ENOCEAN"", false, true, null, ""{5}"", 0, 0);";


        public static readonly string TestDataFieldDefaultProps = @"
        [Fact]
        public void Test_DataField{0}_{1}_{2}_{3}_DefaultProps()
        {{
            var dataField = CreateNodeInstance(EnOceanRorg{0}Data.DataField{0}_{1}_{2}_{3}_Guid);

            var propOffset = dataField.GetPropertyValueInt(""enocean-bitoffset"");
            var propSize = dataField.GetPropertyValueInt(""enocean-length"");

            Assert.Equal({4}, propOffset);
            Assert.Equal({5}, propSize);
        }}";

        public static readonly string TestClassTemplate = @"
            using System;
            using System.Collections.Generic;
            using System.Text;
            using Automatica.Core.UnitTests.Drivers;
            using Xunit;

            namespace P3.Driver.EnOcean.DriverFactory.Tests
            {{
                public class DriverFactoryRorg{0}Tests : DriverFactoryTestBase<EnOceanDriverFactory>
                {{
                    {1}
                }}
            }}
            ";

        public static readonly string TestDataFieldRange = @"
        [Fact]
        public void Test_DataField{0}_{1}_{2}_{3}_Range()
        {{
            var dataField = CreateNodeInstance(EnOceanRorg{0}Data.DataField{0}_{1}_{2}_{3}_Guid);

            var rangeMin = dataField.GetPropertyValueLong(""enocean-range-min"");
            var rangeMax = dataField.GetPropertyValueLong(""enocean-range-max"");

            Assert.Equal({4}, rangeMin);
            Assert.Equal({5}, rangeMax);
        }}";

        public static readonly string TestDataFieldScale = @"
        [Fact]
        public void Test_DataField{0}_{1}_{2}_{3}_Scale()
        {{
            var dataField = CreateNodeInstance(EnOceanRorg{0}Data.DataField{0}_{1}_{2}_{3}_Guid);

            var scaleMin = dataField.GetPropertyValueDouble(""enocean-scale-min"");
            var scaleMax = dataField.GetPropertyValueDouble(""enocean-scale-max"");

            Assert.Equal({4}, scaleMin);
            Assert.Equal({5}, scaleMax);
        }}";
    }
}
