using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Driver;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Automatica.Core.Tests.Database
{
    /// <summary>
    /// The node instance cache fills all nodes from one query per table, the result must be the same as the per node queries
    /// </summary>
    public sealed class NodeInstanceLookupTests : IDisposable, IClassFixture<SeededTemplateFixture>
    {
        private readonly SqliteTestDatabase _database;

        public NodeInstanceLookupTests(SeededTemplateFixture template)
        {
            _database = new SqliteTestDatabase(copyFrom: template.Template.File);
        }

        public void Dispose() => _database.Dispose();

        [Fact]
        public void Fill_IsTheSameAsThePerNodeQueries()
        {
            using (var db = _database.CreateContext())
            {
                // a node template (copy of a seeded one) with two property templates, one with a constraint
                var existing = db.NodeTemplates.AsNoTracking().First();
                var nodeTemplate = new NodeTemplate();
                db.Entry(nodeTemplate).CurrentValues.SetValues(db.Entry(existing).CurrentValues.Clone());
                nodeTemplate.ObjId = Guid.NewGuid();
                nodeTemplate.Key = "lookup-" + nodeTemplate.ObjId.ToString("N");
                db.NodeTemplates.Add(nodeTemplate);

                var propertyType = db.PropertyTypes.AsNoTracking().First().Type;
                for (var i = 0; i < 2; i++)
                {
                    var propertyTemplate = new PropertyTemplate
                    {
                        ObjId = Guid.NewGuid(), Name = "p" + i, Key = "p" + i, This2PropertyType = propertyType, This2NodeTemplate = nodeTemplate.ObjId, Group = "g", Meta = "", DefaultValue = "", Description = ""
                    };
                    db.PropertyTemplates.Add(propertyTemplate);
                    if (i == 0)
                    {
                        db.PropertyTemplateConstraints.Add(new PropertyTemplateConstraint { ObjId = Guid.NewGuid(), Name = "c", Description = "", This2PropertyTemplate = propertyTemplate.ObjId });
                    }
                }
                db.SaveChanges();

                // a node with all its properties, an area and a category
                var root = db.NodeInstances.AsNoTracking().Single(a => a.This2ParentNodeInstance == null);
                var node = new NodeInstance
                {
                    ObjId = Guid.NewGuid(),
                    Name = "lookup",
                    This2NodeTemplate = nodeTemplate.ObjId,
                    This2ParentNodeInstance = root.ObjId,
                    This2AreaInstance = db.AreaInstances.AsNoTracking().Select(a => (Guid?)a.ObjId).FirstOrDefault(),
                    This2CategoryInstance = db.CategoryInstances.AsNoTracking().Select(a => (Guid?)a.ObjId).FirstOrDefault()
                };
                db.NodeInstances.Add(node);
                foreach (var template in db.PropertyTemplates.AsNoTracking().Where(a => a.This2NodeTemplate == nodeTemplate.ObjId).ToList())
                {
                    db.PropertyInstances.Add(new PropertyInstance { ObjId = Guid.NewGuid(), This2NodeInstance = node.ObjId, This2PropertyTemplate = template.ObjId, ValueString = "x" });
                }
                db.SaveChanges();
            }

            using (var db = _database.CreateContext())
            {
                var nodes = db.NodeInstances.AsNoTracking().Where(a => !a.IsDeleted && a.This2ParentNodeInstance != null).ToList();
                Assert.Contains(nodes, a => a.Name == "lookup");
                var lookup = NodeInstanceLookup.Load(db);

                var compared = 0;
                foreach (var node in nodes)
                {
                    var expected = db.NodeInstances.AsNoTracking().Single(a => a.ObjId == node.ObjId);
                    NodeInstanceCache.FillItem(expected, db);
                    lookup.Fill(node);

                    Assert.Equal(expected.This2NodeTemplateNavigation.ObjId, node.This2NodeTemplateNavigation.ObjId);
                    Assert.Equal(expected.This2NodeTemplateNavigation.NeedsInterface2InterfacesTypeNavigation.Type, node.This2NodeTemplateNavigation.NeedsInterface2InterfacesTypeNavigation.Type);
                    Assert.Equal(expected.This2NodeTemplateNavigation.ProvidesInterface2InterfaceTypeNavigation.Type, node.This2NodeTemplateNavigation.ProvidesInterface2InterfaceTypeNavigation.Type);
                    Assert.Equal(expected.This2AreaInstanceNavigation?.ObjId, node.This2AreaInstanceNavigation?.ObjId);
                    Assert.Equal(expected.This2CategoryInstanceNavigation?.ObjId, node.This2CategoryInstanceNavigation?.ObjId);
                    Assert.Equal(expected.This2SlaveNavigation?.ObjId, node.This2SlaveNavigation?.ObjId);

                    var expectedProperties = expected.PropertyInstance.OrderBy(a => a.ObjId).ToList();
                    var properties = node.PropertyInstance.OrderBy(a => a.ObjId).ToList();
                    Assert.Equal(expectedProperties.Select(a => a.ObjId), properties.Select(a => a.ObjId));
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var e = expectedProperties[i].This2PropertyTemplateNavigation;
                        var a = properties[i].This2PropertyTemplateNavigation;
                        Assert.Equal(e.ObjId, a.ObjId);
                        Assert.Equal(e.This2PropertyTypeNavigation.Type, a.This2PropertyTypeNavigation.Type);
                        Assert.Equal(e.Constraints.Select(c => c.ObjId).OrderBy(c => c), a.Constraints.Select(c => c.ObjId).OrderBy(c => c));
                    }
                    compared++;
                }

                Assert.True(compared > 0);
                Assert.NotEmpty(nodes.Single(a => a.Name == "lookup").PropertyInstance);
            }
        }
    }
}
