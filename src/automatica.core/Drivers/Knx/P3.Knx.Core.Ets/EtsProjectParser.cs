using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Automatica.Core.Base.Common;

namespace P3.Knx.Core.Ets
{
    public class EtsProjectParser
    {
        public ILogger Logger { get; set; }

        internal class EtsProjectParserContext
        {
            // id -> ETS Entities
            internal IDictionary<String, EtsProject> Projects { get; } = new Dictionary<String, EtsProject>();
            internal IDictionary<String, EtsGroup> Groups { get; } = new Dictionary<String, EtsGroup>();
            internal IDictionary<String, EtsDatapoint> Datapoints { get; } = new Dictionary<String, EtsDatapoint>();

            // possible locations where to find a datapoint's type:
            internal IDictionary<String, String> GroupAddressRefId2DatapointType { get; } = new Dictionary<String, String>();
            internal IDictionary<String, List<String>> GroupAddressRefId2ComObjectInstanceRefId { get; } = new Dictionary<String, List<String>>();
            internal IDictionary<String, String> ComObjectRefId2DatapointType { get; } = new Dictionary<String, String>();
            internal IDictionary<String, String> ComObject2DatapointType { get; } = new Dictionary<String, String>();
        }

        private EtsProjectParserContext Context { get; set; }

        public EtsProjectParser()
        {
            Logger = NullLogger.Instance;
        }

        public EtsProject ParseEtsFile(String file, GroupAddressStyle? expectedGroupAddressStyle = null)
        {
            Logger.LogDebug("Parsing ETS project file {0}", file);

            Context = new EtsProjectParserContext();

            try
            {
                return ParseEtsFileInternal(file, expectedGroupAddressStyle);
            }
            catch (EtsProjectParserException)
            {
                throw;
            }
            catch (Exception e)
            {
                Logger.LogWarning("Could not extract ETS project file: " + e.Message, e);
                throw new EtsProjectParserException(e);
            }
            finally
            {
                Context = null;
            }
        }

        private EtsProject ParseEtsFileInternal(String file, GroupAddressStyle? expectedGroupAddressStyle)
        {
            try
            {
                using (ZipArchive s = ZipFile.OpenRead(file))
                {
                    var tmpPath = Path.Combine(ServerInfo.GetTempPath(), Guid.NewGuid().ToString());
                    s.ExtractToDirectory(tmpPath);

                    if (IsPasswordProtected(tmpPath))
                        throw new EtsProjectParserPasswordRequiredException();

                    foreach (var dir in Directory.GetDirectories(tmpPath))
                    {
                        foreach (var theEntry in Directory.GetFiles(dir))
                        {
                            var fileInfo = new FileInfo(theEntry);

                            if (IsXmlFileWithDirectoryPrefix(fileInfo, "P-"))
                                ParseProjectDirectory(fileInfo);
                            else if (IsXmlFileWithDirectoryPrefix(fileInfo, "M-"))
                                ParseManufacturerDirectory(fileInfo);
                        }
                    }

                    Directory.Delete(tmpPath, true);
                }
            }
            catch (InvalidDataException e)
            {
                throw new EtsProjectParserInvalidZipFileException(e);
            }

            if (Context.Projects.Count == 0)
                throw new EtsProjectParserEmptyProjectFileException();
            if (Context.Projects.Count > 1)
                throw new EtsProjectParserTooManyProjectsInFileException();

            UpdateDatapointTypes();
            UpdateDeviceLinkFlags();

            EtsProject project = Context.Projects.Values.First();

            if (expectedGroupAddressStyle.HasValue && project.GroupAddressStyle != expectedGroupAddressStyle.Value)
                throw new EtsProjectParserWrongGroupAddressStyleException(expectedGroupAddressStyle.Value, project.GroupAddressStyle);

            return project;
        }
        private bool IsXmlFileWithDirectoryPrefix(FileInfo theEntry, String directoryPrefix)
        {
            var directoryInfo = new DirectoryInfo(Path.GetDirectoryName(theEntry.FullName));
            return theEntry.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                && theEntry.Name.IndexOf("UserFiles", StringComparison.OrdinalIgnoreCase) == -1
                && directoryInfo.Name.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase);
        }
        private bool IsPasswordProtected(string directory)
        {
            var files = Directory.GetFiles(directory, "P-*.zip");
            return files.Length > 0;
        }
        private void ParseProjectDirectory(FileInfo file)
        {
            XElement doc = XElement.Load(file.FullName);
            XNamespace ns = doc.GetDefaultNamespace();

            foreach(var p in from el in doc.Descendants(ns + "Project") 
                             where el.Attribute("Id") != null
                             select el)
            {
                String id = GetAttributeValue(p, "Id");
                if (!Context.Projects.ContainsKey(id))
                    Context.Projects[id] = new EtsProject(p);
                var project = Context.Projects[id];

                project.Aggregate(p);

                foreach (var groupRange in from el in p.Descendants(ns + "GroupRange") select el)
                {
                    EtsGroup parent = null;
                    String parentId = GetAttributeValue(groupRange.Parent, "Id");
                    if (!String.IsNullOrEmpty(parentId) && Context.Groups.ContainsKey(parentId))
                        parent = Context.Groups[parentId];

                    EtsGroup newGroup = new EtsGroup(groupRange, parent);
                    Context.Groups[newGroup.Id] = newGroup;

                    if (parent == null) // has to be a top level group
                        project.AddChild(newGroup);
                    else
                        parent.AddChild(newGroup);

                    foreach (var groupAddresse in from el in groupRange.Elements(ns + "GroupAddress") select el)
                    {
                        EtsDatapoint newDatapoint = new EtsDatapoint(groupAddresse, newGroup);
                        newGroup.AddChild(newDatapoint);
                        Context.Datapoints[newDatapoint.Id] = newDatapoint;
                    }
                }

                foreach (var groupAddressConnector in from el in p.Descendants(ns + "Send")
                                                      where el.Parent?.Parent != null && (el.Parent != null && el.Parent.Parent.Name.LocalName == "ComObjectInstanceRef")
                                                      select el)
                {
                    String groupAddressRefId = GetAttributeValue(groupAddressConnector, "GroupAddressRefId");
                    if (String.IsNullOrEmpty(groupAddressRefId))
                        continue;

                    if (groupAddressConnector.Parent != null)
                    {
                        XElement comObjectInstanceRef = groupAddressConnector.Parent.Parent;
                        if (!Context.GroupAddressRefId2ComObjectInstanceRefId.ContainsKey(groupAddressRefId))
                            Context.GroupAddressRefId2ComObjectInstanceRefId[groupAddressRefId] = new List<String>();
                        Context.GroupAddressRefId2ComObjectInstanceRefId[groupAddressRefId].Add(GetAttributeValue(comObjectInstanceRef, "RefId"));

                        if (Context.GroupAddressRefId2DatapointType.ContainsKey(groupAddressRefId))
                            continue;

                        String datapointType = GetAttributeValue(comObjectInstanceRef, "DatapointType");
                        if (!String.IsNullOrEmpty(datapointType))
                        {
                            Context.GroupAddressRefId2DatapointType[groupAddressRefId] = datapointType;
                        }
                    }
                }

                foreach (var groupAddressConnector in from el in p.Descendants(ns + "Receive")
                                                      where el.Parent?.Parent != null && (el.Parent != null && el.Parent.Parent.Name.LocalName == "ComObjectInstanceRef")
                                                      select el)
                {
                    String groupAddressRefId = GetAttributeValue(groupAddressConnector, "GroupAddressRefId");
                    if (String.IsNullOrEmpty(groupAddressRefId))
                        continue;

                    if (groupAddressConnector.Parent != null)
                    {
                        XElement comObjectInstanceRef = groupAddressConnector.Parent.Parent;
                        if (!Context.GroupAddressRefId2ComObjectInstanceRefId.ContainsKey(groupAddressRefId))
                            Context.GroupAddressRefId2ComObjectInstanceRefId[groupAddressRefId] = new List<String>();
                        Context.GroupAddressRefId2ComObjectInstanceRefId[groupAddressRefId].Add(GetAttributeValue(comObjectInstanceRef, "RefId"));

                        if (Context.GroupAddressRefId2DatapointType.ContainsKey(groupAddressRefId))
                            continue;

                        String datapointType = GetAttributeValue(comObjectInstanceRef, "DatapointType");
                        if (!String.IsNullOrEmpty(datapointType))
                        {
                            Context.GroupAddressRefId2DatapointType[groupAddressRefId] = datapointType;
                        }
                    }
                }

                var firstBuildingParts = (from el in p.Descendants(ns + "Locations") select el).Elements();

                foreach (var building in firstBuildingParts)
                {
                    var etsBuilding = new EtsBuilding(building, ns, Context);
                    etsBuilding.Aggregate(building);
                    project.Buildings.Add(etsBuilding);
                }
            }
        }

        private string GetAttributeValue(XElement e, String attributeName)
        {
            XAttribute a = e.Attribute(attributeName);
            return a?.Value;
        }
        private void ParseManufacturerDirectory(FileInfo file)
        {
            XElement doc = XElement.Load(file.FullName);
            XNamespace ns = doc.GetDefaultNamespace();

            foreach (var comObjectRef in from el in doc.Descendants(ns + "ComObjectRef") 
                                         where el.Attribute("Id") != null
                                         select el)
            {
                string id = GetAttributeValue(comObjectRef, "Id");
                Context.ComObjectRefId2DatapointType[id] = GetAttributeValue(comObjectRef, "DatapointType");
                if (String.IsNullOrEmpty(Context.ComObjectRefId2DatapointType[id]))
                    Context.ComObjectRefId2DatapointType[id] = GetAttributeValue(comObjectRef, "ObjectSize");
            }

            foreach (var comObject in from el in doc.Descendants(ns + "ComObject")
                                      where el.Attribute("Id") != null
                                      select el)
            {
                string id = GetAttributeValue(comObject, "Id");
                Context.ComObject2DatapointType[id] = GetAttributeValue(comObject, "DatapointType");
                if (String.IsNullOrEmpty(Context.ComObject2DatapointType[id]))
                    Context.ComObject2DatapointType[id] = GetAttributeValue(comObject, "ObjectSize");
            }
        }
        private void UpdateDatapointTypes()
        {
            foreach (EtsDatapoint dp in Context.Datapoints.Values)
            {
                // check if type is set on ComObjectInstanceRef 
                if (Context.GroupAddressRefId2DatapointType.ContainsKey(dp.Id))
                {
                    dp.AddDatapointTypeOrSize(Context.GroupAddressRefId2DatapointType[dp.Id]);
                    continue;
                }

                // check if set on any of the ComObjectRef from the manufacturer file
                if (Context.GroupAddressRefId2ComObjectInstanceRefId.ContainsKey(dp.Id))
                {
                    foreach (String comObjectInstanceRefId in Context.GroupAddressRefId2ComObjectInstanceRefId[dp.Id])
                        if (Context.ComObjectRefId2DatapointType.ContainsKey(comObjectInstanceRefId))
                            dp.AddDatapointTypeOrSize(GetFormattedDatapointType(Context.ComObjectRefId2DatapointType[comObjectInstanceRefId]));

                    if (dp.DatapointSizeBits == 0 && !dp.DatapointTypes.Any())
                    {
                        // check if set on any of the ComObject from the manufacturer file
                        foreach (String comObjectInstanceRefId in Context.GroupAddressRefId2ComObjectInstanceRefId[dp.Id])
                        {
                            // <ComObjectRef@Id> ::= <ComObjectRef@RefId> _R- UniqueNumber()
                            String comObjectInstanceId = comObjectInstanceRefId.Substring(0, comObjectInstanceRefId.LastIndexOf('_'));
                            if (Context.ComObject2DatapointType.ContainsKey(comObjectInstanceId))
                                dp.AddDatapointTypeOrSize(GetFormattedDatapointType(Context.ComObject2DatapointType[comObjectInstanceId]));
                        }
                    }
                }
            }
        }

        private string GetFormattedDatapointType(string dpt)
        {
            if (String.IsNullOrEmpty(dpt))
                return "";

            return dpt;
        }
        private void UpdateDeviceLinkFlags()
        {
            foreach(EtsDatapoint dp in Context.Datapoints.Values)
                dp.HasDeviceLinked = Context.GroupAddressRefId2ComObjectInstanceRefId.ContainsKey(dp.Id);
        }
    }
}