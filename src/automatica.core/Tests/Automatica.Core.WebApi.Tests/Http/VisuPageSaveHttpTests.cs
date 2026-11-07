using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Visualization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Http
{
    /// <summary>
    /// POST webapi/visualization/pages, the save of the visu editor (it posts all pages it shows, a page or object that
    /// is not posted any more was deleted in the editor).
    /// </summary>
    public class VisuPageSaveHttpTests : IClassFixture<WebApiHostFixture>
    {
        private const string Url = "webapi/visualization/pages";
        private readonly WebApiHost _host;

        public VisuPageSaveHttpTests(WebApiHostFixture fixture)
        {
            _host = fixture.Host;
        }

        private (Guid template, Guid propertyTemplate) EnsureTemplate()
        {
            using var db = _host.CreateDbContext();
            var existing = db.PropertyTemplates.AsNoTracking().FirstOrDefault(a => a.This2VisuObjectTemplate != null);
            if (existing != null)
            {
                return (existing.This2VisuObjectTemplate!.Value, existing.ObjId);
            }

            var template = new VisuObjectTemplate
            {
                ObjId = Guid.NewGuid(),
                Name = "test-label",
                Description = "",
                Key = "label",
                Group = "test",
                Width = 1,
                Height = 1,
                This2VisuPageType = db.VisuPageTypes.AsNoTracking().First().ObjId,
                IsVisibleForUser = true
            };
            var property = new PropertyTemplate
            {
                ObjId = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.Now,
                ModifiedAt = DateTimeOffset.Now,
                Name = "TEXT",
                Description = "",
                Key = "text",
                This2PropertyType = db.PropertyTypes.AsNoTracking().First().Type,
                This2VisuObjectTemplate = template.ObjId,
                Group = "",
                IsVisible = true,
                Meta = "",
                DefaultValue = ""
            };
            db.VisuObjectTemplates.Add(template);
            db.PropertyTemplates.Add(property);
            db.SaveChanges();
            return (template.ObjId, property.ObjId);
        }

        private static JObject Page(Guid id, string name, params JObject[] objects) => new()
        {
            ["ObjId"] = id,
            ["Name"] = name,
            ["Description"] = null,
            ["This2VisuPageType"] = 1,
            ["Height"] = 10,
            ["Width"] = 10,
            ["VisuObjectInstances"] = new JArray(objects.Cast<object>().ToArray())
        };

        private static JObject VisuObject(Guid id, Guid template, Guid propertyTemplate, Guid propertyId, string text) => new()
        {
            ["ObjId"] = id,
            ["This2VisuObjectTemplate"] = template,
            ["Name"] = "label",
            ["X"] = 1,
            ["Y"] = 2,
            ["Width"] = 1,
            ["Height"] = 1,
            ["PropertyInstance"] = new JArray(new JObject
            {
                ["ObjId"] = propertyId,
                ["This2PropertyTemplate"] = propertyTemplate,
                ["ValueString"] = text,
                ["CreatedAt"] = DateTimeOffset.Now,
                ["ModifiedAt"] = DateTimeOffset.Now
            })
        };

        /// <summary>
        /// The pages that are stored now (like the editor loads them), without the given ones
        /// </summary>
        private async Task<JArray> CurrentPagesWithout(params Guid[] without)
        {
            var pages = (JArray)await (await Admin().GetAsync(Url)).ReadJson();
            return new JArray(pages.Where(a => !without.Contains((Guid)a["objId"])).Cast<object>().ToArray());
        }

        private System.Net.Http.HttpClient Admin() => _host.CreateClient(_host.CreateAdminToken());

        private async Task<System.Net.Http.HttpResponseMessage> Save(JArray pages) => await Admin().PostJson(Url, pages);

        [Fact]
        public async Task Save_NewPageWithObjectAndProperty_IsStoredAndListed()
        {
            var (template, propertyTemplate) = EnsureTemplate();
            var pageId = Guid.NewGuid();
            var objectId = Guid.NewGuid();
            var propertyId = Guid.NewGuid();
            var pages = await CurrentPagesWithout();
            pages.Add(Page(pageId, "new page", VisuObject(objectId, template, propertyTemplate, propertyId, "hello")));

            var response = await Save(pages);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains((JArray)await response.ReadJson(), a => (Guid)a["objId"] == pageId);
            await using var db = _host.CreateDbContext();
            var page = await db.VisuPages.SingleAsync(a => a.ObjId == pageId);
            Assert.Equal("", page.Description);
            var visuObject = await db.VisuObjectInstances.SingleAsync(a => a.ObjId == objectId);
            Assert.Equal(pageId, visuObject.This2VisuPage);
            var property = await db.PropertyInstances.SingleAsync(a => a.ObjId == propertyId);
            Assert.Equal(objectId, property.This2VisuObjectInstance);
            Assert.Equal("hello", property.ValueString);
        }

        [Fact]
        public async Task Save_ExistingPage_IsUpdated_AndADroppedObjectIsDeletedWithItsProperties()
        {
            var (template, propertyTemplate) = EnsureTemplate();
            var pageId = Guid.NewGuid();
            var keptObject = Guid.NewGuid();
            var keptProperty = Guid.NewGuid();
            var droppedObject = Guid.NewGuid();
            var droppedProperty = Guid.NewGuid();
            var pages = await CurrentPagesWithout();
            pages.Add(Page(pageId, "before",
                VisuObject(keptObject, template, propertyTemplate, keptProperty, "one"),
                VisuObject(droppedObject, template, propertyTemplate, droppedProperty, "two")));
            Assert.Equal(HttpStatusCode.OK, (await Save(pages)).StatusCode);

            var second = await CurrentPagesWithout(pageId);
            second.Add(Page(pageId, "after", VisuObject(keptObject, template, propertyTemplate, keptProperty, "changed")));
            var response = await Save(second);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await using var db = _host.CreateDbContext();
            Assert.Equal("after", (await db.VisuPages.SingleAsync(a => a.ObjId == pageId)).Name);
            Assert.Equal("changed", (await db.PropertyInstances.SingleAsync(a => a.ObjId == keptProperty)).ValueString);
            Assert.False(await db.VisuObjectInstances.AnyAsync(a => a.ObjId == droppedObject));
            Assert.False(await db.PropertyInstances.AnyAsync(a => a.ObjId == droppedProperty));
        }

        [Fact]
        public async Task Save_WithoutAPage_DeletesThePageAndItsObjects()
        {
            var (template, propertyTemplate) = EnsureTemplate();
            var pageId = Guid.NewGuid();
            var objectId = Guid.NewGuid();
            var propertyId = Guid.NewGuid();
            var pages = await CurrentPagesWithout();
            pages.Add(Page(pageId, "to delete", VisuObject(objectId, template, propertyTemplate, propertyId, "x")));
            Assert.Equal(HttpStatusCode.OK, (await Save(pages)).StatusCode);

            var response = await Save(await CurrentPagesWithout(pageId));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await using var db = _host.CreateDbContext();
            Assert.False(await db.VisuPages.AnyAsync(a => a.ObjId == pageId));
            Assert.False(await db.VisuObjectInstances.AnyAsync(a => a.ObjId == objectId));
            Assert.False(await db.PropertyInstances.AnyAsync(a => a.ObjId == propertyId));
        }

        [Fact]
        public async Task Save_ClearsTheVisuCache_SoTheViewerSeesTheChange()
        {
            var pageId = Guid.NewGuid();
            Assert.Null(_host.GetService<IVisualizationCache>().PageCache.All().FirstOrDefault(a => a.ObjId == pageId));
            var pages = await CurrentPagesWithout();
            pages.Add(Page(pageId, "cached"));

            Assert.Equal(HttpStatusCode.OK, (await Save(pages)).StatusCode);

            var response = await _host.CreateClient(_host.CreateVisuToken()).GetAsync($"webapi/visualization/page/{pageId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Save_ByVisu_IsForbidden()
        {
            var response = await _host.CreateClient(_host.CreateVisuToken()).PostJson(Url, new JArray());
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Save_NullBody_Returns400()
        {
            var response = await Admin().PostAsync(Url, HttpTestExtensions.RawJson("null"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Save_DuplicatePageIds_Returns400_AndChangesNothing()
        {
            var pageId = Guid.NewGuid();
            var pages = await CurrentPagesWithout();
            pages.Add(Page(pageId, "a"));
            pages.Add(Page(pageId, "b"));

            var response = await Save(pages);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await using var db = _host.CreateDbContext();
            Assert.False(await db.VisuPages.AnyAsync(a => a.ObjId == pageId));
        }

        [Fact]
        public async Task Save_APropertyOfANodeInstance_Returns400_AndLeavesItUnchanged()
        {
            var (template, propertyTemplate) = EnsureTemplate();
            Guid nodePropertyId;
            string before;
            await using (var db = _host.CreateDbContext())
            {
                var nodeProperty = await db.PropertyInstances.AsNoTracking().FirstOrDefaultAsync(a => a.This2NodeInstance != null);
                if (nodeProperty == null)
                {
                    var node = await db.NodeInstances.AsNoTracking().FirstAsync();
                    nodeProperty = new PropertyInstance
                    {
                        ObjId = Guid.NewGuid(),
                        CreatedAt = DateTimeOffset.Now,
                        ModifiedAt = DateTimeOffset.Now,
                        This2NodeInstance = node.ObjId,
                        This2PropertyTemplate = propertyTemplate,
                        ValueString = "node value"
                    };
                    db.PropertyInstances.Add(nodeProperty);
                    await db.SaveChangesAsync();
                }
                nodePropertyId = nodeProperty.ObjId;
                before = nodeProperty.ValueString;
            }

            var pages = await CurrentPagesWithout();
            pages.Add(Page(Guid.NewGuid(), "hijack", VisuObject(Guid.NewGuid(), template, propertyTemplate, nodePropertyId, "overwritten")));
            var response = await Save(pages);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await using var check = _host.CreateDbContext();
            var after = await check.PropertyInstances.AsNoTracking().SingleAsync(a => a.ObjId == nodePropertyId);
            Assert.Equal(before, after.ValueString);
            Assert.NotNull(after.This2NodeInstance);
        }
    }
}
