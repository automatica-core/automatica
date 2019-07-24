using System;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Areas;
using Automatica.Core.EF.Models.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;
using Automatica.Core.Base.Visu;
using Automatica.Core.Internals.Areas;
using Automatica.Core.Internals.UserHelper;
using Automatica.Core.Model.Models.User;
using Microsoft.Extensions.Configuration;
using User = Automatica.Core.Model.Models.User.User;
using Automatica.Core.Base.Common;
using Org.BouncyCastle.Crypto.Tls;

namespace Automatica.Core.Runtime.Database
{
    public static class DatabaseInit
    {
        public static void EnusreDatabaseCreated(IServiceProvider services)
        {

            var context = services.GetRequiredService<AutomaticaContext>();
            var visuInitFactory = services.GetRequiredService<IVisualisationFactory>();
            var config = services.GetRequiredService<IConfiguration>();
            context.Database.Migrate();

            bool dbCreated = !context.BoardTypes.Any();

            if (dbCreated)
            {
                context.RuleInterfaceDirections.Add(new EF.Models.RuleInterfaceDirection()
                {
                    ObjId = 1,
                    Name = "Input",
                    Description = "Input",
                    Key = "I"
                });
                context.RuleInterfaceDirections.Add(new EF.Models.RuleInterfaceDirection()
                {
                    ObjId = 2,
                    Name = "Output",
                    Description = "Output",
                    Key = "O"
                });
                context.RuleInterfaceDirections.Add(new EF.Models.RuleInterfaceDirection()
                {
                    ObjId = 3,
                    Name = "Parameter",
                    Description = "Parameter",
                    Key = "P"
                });

                context.RulePageTypes.Add(new RulePageType()
                {
                    ObjId = 1,
                    Name = "Rules",
                    Description = "Rules",
                    Key = "rules"
                });
                context.VisuPageTypes.Add(new VisuPageType()
                {
                    ObjId = 1,
                    Name = "PC",
                    Description = "PC",
                    Key = "pc"
                });
                context.VisuPageTypes.Add(new VisuPageType()
                {
                    ObjId = 2,
                    Name = "Mobile",
                    Description = "Mobile",
                    Key = "mobile"
                });
                context.SaveChanges();

                context.Slaves.Add(new Slave()
                {
                    ObjId = new Guid(ServerInfo.SelfSlaveId),
                    Name = "local",
                    Description = "this is me",
                    ClientId = "",
                    ClientKey = ""
                });


                context.Settings.Add(new Setting
                {
                    ObjId = 1,
                    ValueKey = "ConfigVersion",
                    Type = (long)PropertyTemplateType.Numeric,
                    Value = 0,
                    Group = "ConfigVersion",
                    IsVisible = false,
                    Order = 10
                });
                context.SaveChanges();
            }

            var lat = context.Settings.SingleOrDefault(a => a.ValueKey == "Latitude");

            if (lat == null)
            {
                context.Settings.Add(new Setting
                {
                    ValueKey = "Latitude",
                    Type = (long)PropertyTemplateType.Numeric,
                    Value = 0,
                    Group = "SERVER.SETTINGS",
                    IsVisible = true,
                    Order = 10
                });

                context.Settings.Add(new Setting
                {
                    ValueKey = "Longitude",
                    Type = (long)PropertyTemplateType.Numeric,
                    Value = 0,
                    Group = "SERVER.SETTINGS",
                    IsVisible = true,
                    Order = 11
                });
            }
            else
            {
                var longi = context.Settings.SingleOrDefault(a => a.ValueKey == "Longitude");

                if(lat.ValueDouble == null)
                {
                    lat.ValueDouble = 0;
                }

                if(longi.ValueDouble == null)
                {
                    longi.ValueDouble = 0;
                }

                context.Settings.Update(longi);
                context.Settings.Update(lat);
            }

            var apiKey = context.Settings.SingleOrDefault(a => a.ValueKey == "apiKey");

            if(apiKey == null)
            {
                context.Settings.Add(new Setting
                {
                    ValueKey = "apiKey",
                    Type = (long)PropertyTemplateType.Text,
                    Value = "",
                    Group = "SERVER.SETTINGS",
                    IsVisible = true,
                    Order = 0
                });
            }

            var autoUpdate = context.Settings.SingleOrDefault(a => a.ValueKey == "autoUpdate");
            var autoUpdateTime = context.Settings.SingleOrDefault(a => a.ValueKey == "autoUpdateTime");
            var reportCrashLogs = context.Settings.SingleOrDefault(a => a.ValueKey == "reportCrashLogs");

            if (autoUpdate == null)
            {
                context.Settings.Add(new Setting
                {
                    ValueKey = "autoUpdate",
                    Type = (long) PropertyTemplateType.Bool,
                    Value = false,
                    Group = "SERVER.SETTINGS",
                    IsVisible = true,
                    Order = 20
                });
            }
            else
            {
                autoUpdate.Order = 20;
                context.Update(autoUpdate);
            }

            if (autoUpdateTime == null)
            {
                context.Settings.Add(new Setting
                {
                    ValueKey = "autoUpdateTime",
                    Type = (long) PropertyTemplateType.Time,
                    Value = new DateTime(2000, 12, 31, 2, 0, 0),
                    Group = "SERVER.SETTINGS",
                    IsVisible = true,
                    Order = 21
                });
            }
            else
            {
                autoUpdateTime.Order = 21;
                autoUpdateTime.Type = (long) PropertyTemplateType.Time;
                context.Update(autoUpdateTime);
            }

            if (reportCrashLogs == null)
            {
                context.Settings.Add(new Setting
                {
                    ValueKey = "reportCrashLogs",
                    Type = (long) PropertyTemplateType.Bool,
                    Value = false,
                    Group = "SERVER.SETTINGS",
                    IsVisible = true,
                    Order = 22
                });
            }
            else
            {
                reportCrashLogs.Order = 22;
                context.Update(reportCrashLogs);
            }

            var cloudUrl = context.Settings.SingleOrDefault(a => a.ValueKey == "cloudUrl");

            if (cloudUrl == null)
            {
                context.Settings.Add(new Setting
                {
                    ValueKey = "cloudUrl",
                    Type = (long)PropertyTemplateType.Text,
                    Value = "https://automatica-core-cloud.azurewebsites.net",
                    Group = "SERVER.SETTINGS",
                    IsVisible = true,
                    Order = 1
                });

            }

            var propertyTypes = Enum.GetValues(typeof(PropertyTemplateType));

            foreach (var propertyType in propertyTypes)
            {
                var propertyTypeDb = context.PropertyTypes.SingleOrDefault(a => a.Type == Convert.ToInt64(propertyType));
                var isNewObject = false;
                if (propertyTypeDb == null)
                {
                    propertyTypeDb = new PropertyType {Type = (int) propertyType};
                    isNewObject = true;
                }

                var type = propertyType.GetType();
                var memInfo = type.GetMember(propertyType.ToString());
                var attributes = memInfo[0].GetCustomAttributes(typeof(PropertyTemplateTypeAttribute), false);

                if (attributes.Length > 0 && attributes[0] is PropertyTemplateTypeAttribute attribute)
                {
                    propertyTypeDb.Name = attribute.Name;
                    propertyTypeDb.Description = attribute.Description;
                    propertyTypeDb.Meta = attribute.Meta;
                }
                else
                {
                    propertyTypeDb.Name = propertyType.GetType().Name;
                    propertyTypeDb.Description = propertyType.GetType().Name;
                    propertyTypeDb.Meta = null;
                }

                if (isNewObject)
                {
                    context.PropertyTypes.Add(propertyTypeDb);
                }
                else
                {
                    context.PropertyTypes.Update(propertyTypeDb);
                }
            }

            var nodeDataTypes = Enum.GetValues(typeof(NodeDataType));

            foreach (var nodeDataType in nodeDataTypes)
            {
                var nodeDataTypeDb = context.NodeDataTypes.SingleOrDefault(a => a.Type == Convert.ToInt64(nodeDataType));
                var isNewObject = false;
                if (nodeDataTypeDb == null)
                {
                    nodeDataTypeDb = new EF.Models.NodeDataType();
                    nodeDataTypeDb.Type = (int)nodeDataType;
                    isNewObject = true;
                }

                var type = nodeDataType.GetType();
                var memInfo = type.GetMember(nodeDataType.ToString());
                var attributes = memInfo[0].GetCustomAttributes(typeof(NodeDataTypeEnumAttribute), false);

                if (attributes.Length > 0 && attributes[0] is NodeDataTypeEnumAttribute attribute)
                {
                    nodeDataTypeDb.Name = attribute.Name;
                    nodeDataTypeDb.Description = attribute.Description;
                }
                else
                {
                    nodeDataTypeDb.Name = nodeDataType.GetType().Name;
                    nodeDataTypeDb.Description = nodeDataType.GetType().Name;
                }

                if (isNewObject)
                {
                    context.NodeDataTypes.Add(nodeDataTypeDb);
                }
                else
                {
                    context.NodeDataTypes.Update(nodeDataTypeDb);
                }
            }


            context.SaveChanges();
            visuInitFactory.Initialize(context, config);
            context.SaveChanges();

            CreateInterfaceTypes(context);
            context.SaveChanges();

            AddSystemTemplates(context);
            AddRaspberryPi3Board(context);
            AddAreaData(context);
            CategoryGroup.GenerateDefault(context);
            context.SaveChanges();
            CategoryInstance.GenerateDefault(context);

            context.SaveChanges();


            if (!context.AreaInstances.Any())
            {
                var projectInstance = new AreaInstance
                {
                    ObjId = Guid.NewGuid(),
                    Name = "Project",
                    Description = "",
                    Icon = "home",
                    This2AreaTemplate = AreaTemplateAttribute.GetFromEnum(AreaTemplates.Project),
                    This2Parent = null
                };
                context.AreaInstances.Add(projectInstance);
            }

            if(dbCreated)
            {
                var rootNodeTemplate = context.NodeTemplates.SingleOrDefault(a => a.ObjId == GuidTemplateTypeAttribute.GetFromEnum(BoardTypeEnum.RaspberryPi3));
                var rootNode = NodeInstanceFactory.CreateNodeInstanceFromTemplate(rootNodeTemplate);

                rootNode.Name = "Raspberry PI 3";
                rootNode.Description = "";

                context.NodeInstances.Add(rootNode);

                var childs = context.NodeTemplates.Where(a => a.NeedsInterface2InterfacesType == rootNodeTemplate.ObjId);

                foreach(var child in childs)
                {
                    if (child.NeedsInterface2InterfacesType == child.ProvidesInterface2InterfaceType)
                    {
                        continue;
                    }
                    var node = NodeInstanceFactory.CreateNodeInstanceFromTemplate(child);
                    node.This2ParentNodeInstance = rootNode.ObjId;
                    context.NodeInstances.Add(node);
                }


                var rulePage = new RulePage
                {
                    ObjId = Guid.NewGuid(),
                    Name = "Page1",
                    Description = "",
                    This2RulePageType = 1
                };

                context.RulePages.Add(rulePage);


                var visuPage = new VisuPage
                {
                    ObjId = Guid.NewGuid(),
                    Name = "Page1",
                    Description = "",
                    This2VisuPageType = 2,
                    DefaultPage = true
                };

                context.VisuPages.Add(visuPage);

                AddInitUserManagementData(context);

                context.SaveChanges();
            }


            context.SaveChanges();
        }

        private static void AddInitUserManagementData(AutomaticaContext context)
        {
            var salt = UserHelper.GenerateNewSalt();
            var saUser = new User
            {
                UserName = "sa",
                FirstName = "admin",
                LastName = "admin",
                Salt = salt,
                Password = UserHelper.HashPassword("sa", salt),
                ObjId = Guid.NewGuid()
            };

            context.Add(saUser);


            var adminGroup = new UserGroup
            {
                ObjId = Guid.NewGuid(),
                Name = "Admin"
            };

            context.Add(adminGroup);


            var adminRole = new Role
            {
                ObjId = Guid.NewGuid(),
                Name = "ROLES.ADMINISTRATOR.NAME",
                Description = "ROLES.ADMINISTRATOR.DESCRIPTION",
                Key = Role.AdminRole
            };
            context.Add(adminRole);

            var priviliedge = new Priviledge
            {
                ObjId = Guid.NewGuid(),
                Name = "PRIVILEDGE.ALL.NAME",
                Description = "PRIVILEDGE.ALL.DESCRIPTION",
                Key = "superuser"
            };

            context.Add(priviliedge);


            context.Add(new Priviledge2Role
            {
                This2Priviledge = priviliedge.ObjId,
                This2Role = adminRole.ObjId
            });

            context.Add(new User2Group
            {
                This2User = saUser.ObjId,
                This2UserGroup = adminGroup.ObjId
            });

            context.Add(new User2Role
            {
                This2User = saUser.ObjId,
                This2Role = adminRole.ObjId
            });


            salt = UserHelper.GenerateNewSalt();
            var visuUser = new User
            {
                UserName = "visu",
                FirstName = "Visu",
                LastName = "Visu",
                Salt = salt,
                Password = UserHelper.HashPassword("visu", salt),
                ObjId = Guid.NewGuid()
            };

            context.Add(visuUser);

            var visuGroup = new UserGroup
            {
                ObjId = Guid.NewGuid(),
                Name = "Visu"
            };

            context.Add(visuGroup);

            context.Add(new User2Group
            {
                This2User = visuUser.ObjId,
                This2UserGroup = visuGroup.ObjId
            });


            var visuRole = new Role
            {
                ObjId = Guid.NewGuid(),
                Name = "ROLES.VISUALISATION.NAME",
                Description = "ROLES.VISUALISATION.DESCRIPTION",
                Key = Role.VisuRole
            };
            context.Add(visuRole);

            var visuPriviliedge = new Priviledge
            {
                ObjId = Guid.NewGuid(),
                Name = "PRIVILEDGE.VISU.NAME",
                Description = "PRIVILEDGE.VISU.DESCRIPTION",
                Key = "visu"
            };

            context.Add(visuPriviliedge);


            context.Add(new Priviledge2Role
            {
                This2Priviledge = visuPriviliedge.ObjId,
                This2Role = visuRole.ObjId
            });

            context.Add(new User2Role
            {
                This2User = visuUser.ObjId,
                This2Role = visuRole.ObjId
            });


            context.Add(new UserGroup2Role()
            {
                This2UserGroup = visuGroup.ObjId,
                This2Role = visuRole.ObjId
            });


            context.SaveChanges();

        }

        private static void AddAreaData(AutomaticaContext context)
        {
            var areaTypes = Enum.GetValues(typeof(AreaTypes));

            foreach (var areaType in areaTypes)
            {
                var type = areaType.GetType();
                var memInfo = type.GetMember(areaType.ToString());
                var attributes = memInfo[0].GetCustomAttributes(typeof(AreaTypeAttribute), false);

                if (attributes.Length == 0)
                {
                    continue;
                }

                if (attributes[0] is AreaTypeAttribute attribute)
                {
                    var areaTypeDb = context.AreaTypes.SingleOrDefault(a => a.ObjId == attribute.ObjId);
                    var isNew = false;
                    if (areaTypeDb == null)
                    {
                        areaTypeDb = new AreaType();
                        areaTypeDb.ObjId = attribute.ObjId;

                        isNew = true;
                    }

                    areaTypeDb.Name = attribute.Name;
                    areaTypeDb.Description = attribute.Description;

                    if (isNew)
                    {
                        context.AreaTypes.Add(areaTypeDb);
                    }
                    else
                    {
                        context.AreaTypes.Update(areaTypeDb);
                    }

                }

            }

            context.SaveChanges();

            var areaTemplates = Enum.GetValues(typeof(AreaTemplates));

            foreach (var areaTemplate in areaTemplates)
            {
                var type = areaTemplate.GetType();
                var memInfo = type.GetMember(areaTemplate.ToString());
                var attributes = memInfo[0].GetCustomAttributes(typeof(AreaTemplateAttribute), false);

                if (attributes.Length == 0)
                {
                    continue;
                }

                if (attributes[0] is AreaTemplateAttribute attribute)
                {
                    var areaTemplateDb = context.AreaTemplates.SingleOrDefault(a => a.ObjId == attribute.ObjId);
                    var isNew = false;
                    if (areaTemplateDb == null)
                    {
                        areaTemplateDb = new AreaTemplate();
                        areaTemplateDb.ObjId = attribute.ObjId;

                        isNew = true;
                    }

                    areaTemplateDb.Name = attribute.Name;
                    areaTemplateDb.Description = attribute.Description;
                    areaTemplateDb.This2AreaType = AreaTypeAttribute.GetFromEnum(attribute.IsAreaType);
                    areaTemplateDb.NeedsThis2AreaType = AreaTypeAttribute.GetFromEnum(attribute.NeedsAreaType);
                    areaTemplateDb.ProvidesThis2AreayType = AreaTypeAttribute.GetFromEnum(attribute.ProvidesAreayType);
                    areaTemplateDb.Icon = attribute.Icon;
                    areaTemplateDb.IsDeleteable = attribute.IsDeletable;

                    if (isNew)
                    {
                        context.AreaTemplates.Add(areaTemplateDb);
                    }
                    else
                    {
                        context.AreaTemplates.Update(areaTemplateDb);
                    }

                    context.SaveChanges();
                }

            }
        }

        private static void AddSystemTemplates(AutomaticaContext context)
        {
            var usbIr = new NodeTemplate
            {
                ObjId = new Guid("4d2cf0d47fe947058271787d5626fbe6"),
                Name = "COMMON.INTERFACES.USB_IR.NAME",
                Description = "COMMON.INTERFACES.USB_IR.DESCRIPTION",
                Key = "usbir",
                NeedsInterface2InterfacesType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Usb),
                ProvidesInterface2InterfaceType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.UsbIr),
                This2DefaultMobileVisuTemplate = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.Label),
                IsDeleteable = true,
                DefaultCreated = false,
                IsReadable = false,
                IsReadableFixed = true,
                IsWriteable = false,
                IsWriteableFixed = true,
                MaxInstances = int.MaxValue,
                This2NodeDataType = (int)NodeDataType.NoAttribute,
                IsAdapterInterface = true
            };

            if (context.NodeTemplates.SingleOrDefault(a => a.ObjId == usbIr.ObjId) == null)
            {
                context.NodeTemplates.Add(usbIr);
                context.SaveChanges();
            }

            
            var usbrs485 = new NodeTemplate
            {
                ObjId = new Guid("acf16ee90377485588341fe1e67d6a93"),
                Name = "COMMON.INTERFACES.RS485.NAME",
                Description = "COMMON.INTERFACES.RS485.DESCRIPTION",
                Key = "usbrs485",
                NeedsInterface2InterfacesType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Usb),
                ProvidesInterface2InterfaceType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Rs485),
                This2DefaultMobileVisuTemplate = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.Label),
                IsDeleteable = true,
                DefaultCreated = false,
                IsReadable = false,
                IsReadableFixed = true,
                IsWriteable = false,
                IsWriteableFixed = true,
                MaxInstances = int.MaxValue,
                This2NodeDataType = (int)NodeDataType.NoAttribute,
                IsAdapterInterface = true
            };

            if (context.NodeTemplates.SingleOrDefault(a => a.ObjId == usbrs485.ObjId) == null)
            {
                context.NodeTemplates.Add(usbrs485);
                context.SaveChanges();
            }

            var usbrs232 = new NodeTemplate
            {
                ObjId = new Guid("09d3b6b4391847e091aa5a540a7bd67f"),
                Name = "COMMON.INTERFACES.RS232.NAME",
                Description = "COMMON.INTERFACES.RS232.DESCRIPTION",
                Key = "usbrs232",
                NeedsInterface2InterfacesType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Usb),
                ProvidesInterface2InterfaceType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Rs232),This2DefaultMobileVisuTemplate = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.Label),
                IsDeleteable = true,
                DefaultCreated = false,
                IsReadable = false,
                IsReadableFixed = true,
                IsWriteable = false,
                IsWriteableFixed = true,
                MaxInstances = int.MaxValue,
                This2NodeDataType = (int)NodeDataType.NoAttribute,
                IsAdapterInterface = true
            };

            if (context.NodeTemplates.SingleOrDefault(a => a.ObjId == usbrs232.ObjId) == null)
            {
                context.NodeTemplates.Add(usbrs232);
                context.SaveChanges();
            }

        }

        private static void AddRaspberryPi3Board(AutomaticaContext context)
        {
            var rsp3Board = new BoardTypes.RaspberryPi3.RaspberryPi3();

            var board = context.BoardTypes.SingleOrDefault(a => a.Type == GuidTemplateTypeAttribute.GetFromEnum(rsp3Board.BoardType));
            var boardNodeTemplate = context.NodeTemplates.SingleOrDefault(a => a.ObjId == board.Type);

            var boardInterfaceType = context.InterfaceTypes.SingleOrDefault(a => a.Type == board.Type);

            if(boardInterfaceType == null)
            {
                boardInterfaceType = new InterfaceType
                {
                    Type = board.Type,
                    Name = board.Name,
                    Description = board.Description,
                    IsDriverInterface = false,
                    MaxChilds = int.MaxValue,
                    MaxInstances = 1,
                    CanProvideBoardType = false
                };

                context.InterfaceTypes.Add(boardInterfaceType);
            }

            if(boardNodeTemplate == null)
            {
                boardNodeTemplate = new NodeTemplate
                {
                    ObjId = board.Type,
                    DefaultCreated = false,
                    Description = board.Description,
                    Name = board.Name,
                    IsAdapterInterface = false,
                    IsDeleteable = false,
                    IsReadable = false,
                    IsReadableFixed = true,
                    IsWriteable = false,
                    IsWriteableFixed = true,
                    Key = board.Name.Replace(" ", ""),
                    MaxInstances = 1,
                    NeedsInterface2InterfacesType = board.Type,
                    ProvidesInterface2InterfaceType = board.Type,
                    This2NodeDataType = (int)NodeDataType.NoAttribute,
                    This2DefaultMobileVisuTemplate = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.Label),
                };

                context.NodeTemplates.Add(boardNodeTemplate);
            }
            context.SaveChanges();


            foreach (var boardInterface in rsp3Board.GetBoardInterfaces())
            {
                var boardInt = context.BoardInterfaces.SingleOrDefault(a => a.ObjId == boardInterface.ObjId);
                if (boardInt == null)
                {
                    context.BoardInterfaces.Add(boardInterface);

                    var nodeTemplate = new NodeTemplate
                    {
                        ObjId = boardInterface.ObjId,
                        DefaultCreated = false,
                        Description = boardInterface.Description,
                        Name = boardInterface.Name,
                        IsAdapterInterface = false,
                        IsDeleteable = false,
                        IsReadable = false,
                        IsReadableFixed = true,
                        IsWriteable = false,
                        IsWriteableFixed = true,
                        Key = boardInterface.Name.Replace(" ", ""),
                        MaxInstances = 1,
                        NeedsInterface2InterfacesType = boardNodeTemplate.ObjId,
                        ProvidesInterface2InterfaceType = boardInterface.This2InterfaceType,
                        This2NodeDataType = (int)NodeDataType.NoAttribute,
                        This2DefaultMobileVisuTemplate = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.Label),
                    };

                    context.NodeTemplates.Add(nodeTemplate);
                }
            }
        }

        private static void CreateInterfaceTypes(AutomaticaContext context)
        {
            var boards = Enum.GetValues(typeof(BoardTypeEnum));

            foreach (var boardTypeEnum in boards)
            {
                var type = boardTypeEnum.GetType();
                var memInfo = type.GetMember(boardTypeEnum.ToString());
                var attributes = memInfo[0].GetCustomAttributes(typeof(GuidTemplateTypeAttribute), false);
                if (attributes.Length > 0 && attributes[0] is GuidTemplateTypeAttribute attribute)
                {
                    var boardType = context.BoardTypes.SingleOrDefault(a => a.Type == attribute.Guid);
                    var isNewObject = false;
                    if (boardType == null)
                    {
                        boardType = new BoardType();
                        boardType.Type = attribute.Guid;
                        isNewObject = true;
                    }

                    boardType.Name = boardType.ToString();
                    boardType.Description = boardType.ToString();

                    if (isNewObject)
                    {
                        context.BoardTypes.Add(boardType);
                    }
                    else
                    {
                        context.BoardTypes.Update(boardType);
                    }
                }
            }

            var interfaceTypes = Enum.GetValues(typeof(InterfaceTypeEnum));

            foreach (var interfaceTypeEnum in interfaceTypes)
            {
                var type = interfaceTypeEnum.GetType();
                var memInfo = type.GetMember(interfaceTypeEnum.ToString());
                var attributes = memInfo[0].GetCustomAttributes(typeof(GuidTemplateTypeAttribute), false);
                if (attributes.Length > 0 && attributes[0] is GuidTemplateTypeAttribute attribute)
                {
                    var interfaceType = context.InterfaceTypes.SingleOrDefault(a => a.Type == attribute.Guid);
                    var isNewObject = false;
                    if (interfaceType == null)
                    {
                        interfaceType = new InterfaceType();
                        interfaceType.Type = attribute.Guid;
                        isNewObject = true;
                    }

                    interfaceType.Name = interfaceType.ToString();
                    interfaceType.Description = interfaceType.ToString();
                    interfaceType.IsDriverInterface = false;
                    interfaceType.MaxChilds = attribute.MaxChilds;
                    interfaceType.MaxInstances = 1;

                    if (isNewObject)
                    {
                        context.InterfaceTypes.Add(interfaceType);
                    }
                    else
                    {
                        context.InterfaceTypes.Update(interfaceType);
                    }
                }
            }
            context.SaveChanges();
        }
    }
}
