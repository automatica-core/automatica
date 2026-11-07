using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Core;
using Automatica.Core.Runtime.Core.Plugins.Drivers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Automatica.Core.Tests.Database
{
    /// <summary>
    /// The node template cache loads with one query per collection and once per load; a cleared cache must not answer with old templates
    /// </summary>
    public sealed class NodeTemplateCacheTests : IDisposable, IClassFixture<SeededTemplateFixture>
    {
        private readonly SqliteTestDatabase _database;

        public NodeTemplateCacheTests(SeededTemplateFixture template)
        {
            _database = new SqliteTestDatabase(copyFrom: template.Template.File);
        }

        public void Dispose() => _database.Dispose();

        private NodeTemplate AddTemplateWithConstraintData()
        {
            using var db = _database.CreateContext();
            var existing = db.NodeTemplates.AsNoTracking().First();
            var nodeTemplate = new NodeTemplate();
            db.Entry(nodeTemplate).CurrentValues.SetValues(db.Entry(existing).CurrentValues.Clone());
            nodeTemplate.ObjId = Guid.NewGuid();
            nodeTemplate.Key = "cache-" + nodeTemplate.ObjId.ToString("N");
            nodeTemplate.Name = "cache test";
            db.NodeTemplates.Add(nodeTemplate);

            var propertyType = db.PropertyTypes.AsNoTracking().First().Type;
            for (var i = 0; i < 3; i++)
            {
                var propertyTemplate = new PropertyTemplate
                {
                    ObjId = Guid.NewGuid(), Name = "p" + i, Key = "p" + i, This2PropertyType = propertyType, This2NodeTemplate = nodeTemplate.ObjId, Group = "g", Meta = "", DefaultValue = "", Description = ""
                };
                db.PropertyTemplates.Add(propertyTemplate);

                for (var c = 0; c < i; c++)
                {
                    var constraint = new PropertyTemplateConstraint { ObjId = Guid.NewGuid(), Name = "c" + c, Description = "", This2PropertyTemplate = propertyTemplate.ObjId };
                    db.PropertyTemplateConstraints.Add(constraint);
                    for (var d = 0; d < 2; d++)
                    {
                        db.PropertyTemplateConstraintData.Add(new PropertyTemplateConstraintData { ObjId = Guid.NewGuid(), Factor = d, PropertyKey = "p" + i, This2PropertyTemplateConstraint = constraint.ObjId });
                    }
                }
            }
            db.SaveChanges();
            return nodeTemplate;
        }

        [Fact]
        public void Query_LoadsTheSameAsOneJoinedQuery()
        {
            AddTemplateWithConstraintData();

            using var db = _database.CreateContext();
            var split = NodeTemplateCache.Query(db).ToList();
            var joined = NodeTemplateCache.Query(db).AsSingleQuery().ToList();

            Assert.Equal(joined.Count, split.Count);
            Assert.Equal(db.NodeTemplates.Count(), split.Count);

            var byId = joined.ToDictionary(a => a.ObjId);
            var constraintData = 0;
            foreach (var template in split)
            {
                var expected = byId[template.ObjId];
                Assert.NotNull(template.NeedsInterface2InterfacesTypeNavigation);
                Assert.NotNull(template.ProvidesInterface2InterfaceTypeNavigation);
                Assert.Equal(expected.This2NodeDataTypeNavigation?.Type, template.This2NodeDataTypeNavigation?.Type);
                Assert.Equal(expected.PropertyTemplate.Select(a => a.ObjId).OrderBy(a => a), template.PropertyTemplate.Select(a => a.ObjId).OrderBy(a => a));

                foreach (var property in template.PropertyTemplate)
                {
                    var expectedProperty = expected.PropertyTemplate.Single(a => a.ObjId == property.ObjId);
                    Assert.NotNull(property.This2PropertyTypeNavigation);
                    Assert.Equal(expectedProperty.Constraints.Select(a => a.ObjId).OrderBy(a => a), property.Constraints.Select(a => a.ObjId).OrderBy(a => a));
                    foreach (var constraint in property.Constraints)
                    {
                        var expectedConstraint = expectedProperty.Constraints.Single(a => a.ObjId == constraint.ObjId);
                        Assert.Equal(expectedConstraint.ConstraintData.Select(a => a.ObjId).OrderBy(a => a), constraint.ConstraintData.Select(a => a.ObjId).OrderBy(a => a));
                        constraintData += constraint.ConstraintData.Count;
                    }
                }
            }

            Assert.True(constraintData >= 6);
        }

        [Fact]
        public void Clear_ForgetsTheOldTemplatesByIdAndKey()
        {
            var template = AddTemplateWithConstraintData();
            var cache = new NodeTemplateCache(_database.Configuration, null!, new DriverFactoryStore());

            Assert.Equal("cache test", cache.GetByKey(template.Key)?.Name);
            Assert.Equal("cache test", cache.GetByKey(template.ObjId)?.Name);
            Assert.Equal(3, cache.Get(template.ObjId).PropertyTemplate.Count);

            using (var db = _database.CreateContext())
            {
                var row = db.NodeTemplates.Single(a => a.ObjId == template.ObjId);
                row.Name = "renamed";
                db.SaveChanges();
            }

            cache.ClearAndLoad();

            Assert.Equal("renamed", cache.Get(template.ObjId).Name);
            Assert.Equal("renamed", cache.GetByKey(template.Key)?.Name);
            Assert.Equal("renamed", cache.GetByKey(template.ObjId)?.Name);

            // a plain Clear (plugin loaded at runtime) loads again on the next lookup by key
            cache.Clear();
            Assert.Equal("renamed", cache.GetByKey(template.Key)?.Name);

            using (var db = _database.CreateContext())
            {
                Assert.Equal(db.NodeTemplates.Count(), cache.All().Count);
            }
        }
    }
}
