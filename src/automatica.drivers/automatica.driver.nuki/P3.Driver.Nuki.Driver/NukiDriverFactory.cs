using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Nuki.Driver
{
    public class NukiDriverFactory : DriverFactory
    {
        public override string DriverName => "Nuki";

        public override Guid DriverGuid => new Guid("da739d07-56a6-4cd3-a3dc-ac7c5d2d4fe5");

        public override Version DriverVersion => new Version(1, 0, 0, 1);

        public override string ImageName => "automaticacore/plugin-p3.driver.nuki";

        public override bool InDevelopmentMode => false;
        public override IDriver CreateDriver(IDriverContext config)
        {
            return new NukiDriver(config);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="factory"></param>9
        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "NUKI.NAME", "NUKI.DESCRIPTION", int.MaxValue, 1, true);

            factory.CreateNodeTemplate(DriverGuid, "NUKI.NAME", "NUKI.DESCRIPTION", "NUKI", 
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, 1, false);

            factory.CreatePropertyTemplate(new Guid("243b2e63-21b4-40e0-8e4d-295bb0a4894c"), "NUKI.POLL_INTERVAL.NAME", "NUKI.POLL_INTERVAL.DESCRIPTION", "poll",
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(TimeSpan.FromSeconds(1).TotalSeconds, TimeSpan.FromHours(2).TotalSeconds), TimeSpan.FromSeconds(2).TotalSeconds, 1, 1);

            factory.CreatePropertyTemplate(new Guid("d6d2f651-70e5-4bbc-9a7a-baf5576cfbae"), "NUKI.IP_ADDRESS.NAME", "NUKI.IP_ADDRESS.DESCRIPTION", "ip",
                PropertyTemplateType.Ip, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 1, 2);

            factory.CreatePropertyTemplate(new Guid("d24e0269-305d-47c0-b10c-1d13397e4e94"), "NUKI.PORT.NAME", "NUKI.PORT.DESCRIPTION", "port",
                PropertyTemplateType.Integer, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, 8080, 1, 3);

            factory.CreatePropertyTemplate(new Guid("2260ec26-9f88-4c90-91f8-072a852c92a3"), "NUKI.ACCESS_TOKEN.NAME", "NUKI.ACCESS_TOKEN.DESCRIPTION", "token",
                PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 1, 4);

            CreateSmartLock(factory);
        }

        private void CreateSmartLock(INodeTemplateFactory factory)
        {
            var uid = new Guid("6a557613-b20c-4dcd-b58f-1654dc11d8b2");
            factory.CreateInterfaceType(uid, "NUKI.SMART_LOCK.NAME", "NUKI.SMART_LOCK.DESCRIPTION", int.MaxValue, int.MaxValue, false);

            var smartLockUid = new Guid("58e1dddc-03d4-4e5a-8a4b-164e9d183cbe");
            factory.CreateNodeTemplate(smartLockUid, "NUKI.SMART_LOCK.NAME", "NUKI.SMART_LOCK.DESCRIPTION", "smart_lock", DriverGuid,
                uid, true, true, true, false, true, NodeDataType.Date, 1, false);

            factory.CreatePropertyTemplate(new Guid("b5d42877-0fba-4a5b-be5e-47e6cbba2adf"), "NUKI.SMART_LOCK.ID.NAME", "NUKI.SMART_LOCK.ID.DESCRIPTION", "id",
                PropertyTemplateType.Integer, smartLockUid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 1, 1);

            factory.CreateNodeTemplate(new Guid("d4122ed5-f1c1-4ec0-baf0-2d1cbbe13c5d"), "NUKI.SMART_LOCK.STATE.NAME", "NUKI.SMART_LOCK.STATE.DESCRIPTION", "state", uid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);
            factory.CreateNodeTemplate(new Guid("546a0769-5fbf-4054-bbdb-717ac184855e"), "NUKI.SMART_LOCK.STATE_TEXT.NAME", "NUKI.SMART_LOCK.STATE_TEXT.DESCRIPTION", "stateName", uid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.String, 1, false);


            factory.CreateNodeTemplate(new Guid("5195bf0b-eb1a-4cc0-ae3f-72e97be2e01e"), "NUKI.SMART_LOCK.BATTERY_CRITICAL.NAME", "NUKI.SMART_LOCK.BATTERY_CRITICAL.DESCRIPTION", "batteryCritical", uid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Boolean, 1, false);

            factory.CreateNodeTemplate(new Guid("26af095b-8eef-4299-a171-fdb962eb4c25"), "NUKI.SMART_LOCK.BATTERY_CHARGE_STATE.NAME", "NUKI.SMART_LOCK.BATTERY_CHARGE_STATE.DESCRIPTION", "batteryChargeState", uid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);

            factory.CreateNodeTemplate(new Guid("14986357-750a-45ca-83ed-917413c19cd9"), "NUKI.SMART_LOCK.TIMESTAMP.NAME", "NUKI.SMART_LOCK.TIMESTAMP.DESCRIPTION", "timestamp", uid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.DateTime, 1, false);
        }

    }
}
