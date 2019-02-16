using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using P3.Knx.Core.Ets;

namespace P3.Driver.Knx.DriverFactory
{
    public static class EtsProjectToNodeConverter
    {
        public static Task<IList<NodeInstance>> ConvertToNodeInstances(INodeTemplateFactory factory, EtsProject project, NodeInstance knxInterface)
        {
            IList<NodeInstance> nodes = new List<NodeInstance>();

            if(project.GroupAddressStyle != GroupAddressStyle.ThreeLevel)
            {
                throw new NotImplementedException();
            }

            var mainGroupItems = knxInterface.InverseThis2ParentNodeInstanceNavigation;
            foreach(var item in project.Children)
            {
                var mainAddress = ((EtsGroup)item).GroupIndex;
                
                var mainGroup = FindByAddressProperty(mainAddress, mainGroupItems);
                if(mainGroup == null)
                {
                    mainGroup = factory.CreateNodeInstance(KnxIpDriverFactory.KnxIp3LevelMainAddress);
                    mainGroup.This2ParentNodeInstance = knxInterface.ObjId;
                }

                mainGroup.Name = item.Name;
                mainGroup.Description = item.Description;
                mainGroup.SetProperty("knx-address", item.GetMainGroup());
                nodes.Add(mainGroup);


                var middleGroupItems = mainGroup.InverseThis2ParentNodeInstanceNavigation;
                foreach (var middle in ((EtsGroup)item).Children)
                {
                    var middleGroup = FindByAddressProperty(((EtsGroup)middle).GroupIndex, middleGroupItems);

                    if(middleGroup == null)
                    {
                        middleGroup = factory.CreateNodeInstance(KnxIpDriverFactory.KnxIp3LevelMiddleAddress);
                       
                        middleGroup.This2ParentNodeInstance = mainGroup.ObjId;
                    }

                    middleGroup.Name = middle.Name;
                    middleGroup.Description = middle.Description;
                    middleGroup.SetProperty("knx-address", middle.GetMiddleGroup());
                  

                    mainGroup.InverseThis2ParentNodeInstanceNavigation.Add(middleGroup);
                    var dpItems = middleGroup.InverseThis2ParentNodeInstanceNavigation;

                    foreach(var datapoint in ((EtsGroup)middle).Children)
                    {
                        var dp = (EtsDatapoint)datapoint;
                        if(!dp.HasDeviceLinked) // ignore not linked addresses -> we maybe don't know which data type is used
                        {
                            continue;
                        }
                        var dpNode = FindByAddressProperty(dp.Address, dpItems);
                        
                        if(dpNode == null) // already existing
                        {
                            dpNode = CreateNodeTemplateFromEtsDatapoint(dp, factory);
                            if (dpNode == null) // not supported
                            {
                                continue;
                            }
                            dpNode.This2ParentNodeInstance = middleGroup.ObjId;

                           
                        }
                        middleGroup.InverseThis2ParentNodeInstanceNavigation.Add(dpNode);
    
                    }
                }

            }

            return Task.FromResult(nodes);
        }

        private static NodeInstance CreateNodeTemplateFromEtsDatapoint(EtsDatapoint datapoint, INodeTemplateFactory factory)
        {
            if(!datapoint.DatapointTypesSplitted.Any())
            {
                return null;
            }
            var mainDpt = datapoint.DatapointTypesSplitted.First().Item1;
            var subDpt = datapoint.DatapointTypesSplitted.First().Item2;

            NodeInstance nodeInstance = null;
            switch(mainDpt)
            {
                case 1:
                    nodeInstance = factory.CreateNodeInstanceByKey("knx-dpt1");
                    nodeInstance.SetProperty("knx-dpt", mainDpt);
                    break;
                case 2:
                    nodeInstance = factory.CreateNodeInstanceByKey("knx-dpt2");
                    nodeInstance.SetProperty("knx-dpt", mainDpt);
                    break;
                case 3:
                    nodeInstance = factory.CreateNodeInstanceByKey("knx-dpt3");
                    nodeInstance.SetProperty("knx-dpt", mainDpt);
                    break;
                case 5:
                    nodeInstance = factory.CreateNodeInstanceByKey("knx-dpt5");
                    nodeInstance.SetProperty("knx-dpt", subDpt);
                    break;
                case 6:
                    if(subDpt == 20)
                    {
                        nodeInstance = factory.CreateNodeInstanceByKey("knx-dpt6");
                        nodeInstance.SetProperty("knx-dpt", subDpt);
                    }
                    else
                    {
                        nodeInstance = factory.CreateNodeInstanceByKey("knx-dpt6.020");
                        nodeInstance.SetProperty("knx-dpt", mainDpt);
                    }
                    break;
                case 7:
                    nodeInstance = factory.CreateNodeInstanceByKey("knx-dpt7");
                    nodeInstance.SetProperty("knx-dpt", subDpt);
                    break;
                case 8:
                    nodeInstance = factory.CreateNodeInstanceByKey("knx-dpt8");
                    nodeInstance.SetProperty("knx-dpt", subDpt);
                    break;
                case 9:
                    nodeInstance = factory.CreateNodeInstanceByKey("knx-dpt9");
                    nodeInstance.SetProperty("knx-dpt", subDpt);
                    break;
                case 10:
                    nodeInstance = factory.CreateNodeInstanceByKey("knx-dpt10");
                    nodeInstance.SetProperty("knx-dpt", subDpt);
                    break;
                case 11:
                    nodeInstance = factory.CreateNodeInstanceByKey("knx-dpt11");
                    nodeInstance.SetProperty("knx-dpt", subDpt);
                    break;

            }

            if (nodeInstance != null)
            {
                nodeInstance.Name = datapoint.Name;
                nodeInstance.Description = datapoint.Description;
                nodeInstance.SetProperty("knx-address", datapoint.GetAddress());
            }
            return nodeInstance;
        }

        private static NodeInstance FindByAddressProperty(int value, ICollection<NodeInstance> instances)
        {
            foreach(var instance in instances)
            {
                if(instance.GetPropertyValueInt("knx-address") == value)
                {
                    return instance;
                }
            }
            return null;
        }
    }
}
