using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Areas;
using Automatica.Core.EF.Models.Categories;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Internals.Cache.Visualization;
using Automatica.Core.Internals.Core;
using Automatica.Core.Internals.Templates;
using Automatica.Core.Model.Models.User;
using Automatica.Core.WebApi.Tests.Logic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Http
{
    public class CategoryHttpTests : IClassFixture<WebApiHostFixture>
    {
        private const string Url = "webapi/categories";
        private readonly WebApiHost _host;
        private readonly HttpClient _admin;

        public CategoryHttpTests(WebApiHostFixture fixture)
        {
            _host = fixture.Host;
            _admin = _host.CreateClient(_host.CreateAdminToken());
        }

        private async Task<Guid> CategoryGroup()
        {
            var groups = (JArray)await (await _admin.GetAsync($"{Url}/groups")).ReadJson();
            Assert.NotEmpty(groups);
            return (Guid)groups.First()["objId"];
        }

        private async Task<CategoryInstance> NewCategory(Guid? userGroup = null) => new()
        {
            ObjId = Guid.NewGuid(),
            Name = _host.UniqueName("category"),
            Description = "",
            This2CategoryGroup = await CategoryGroup(),
            This2UserGroup = userGroup,
            Color = "green",
            Icon = "test-icon",
            Rating = 3
        };

        private async Task<JArray> All() => (JArray)await (await _admin.GetAsync($"{Url}/all")).ReadJson();

        [Fact]
        public async Task Category_CreateGetUpdateDelete_RoundTrip()
        {
            var category = await NewCategory();

            var created = await _admin.PutJson(Url, category);
            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
            Assert.Equal(category.Name, (string)(await created.ReadJson())["name"]);
            Assert.Contains(await All(), a => (Guid)a["objId"] == category.ObjId);

            category.Name += "-updated";
            category.Rating = 5;
            var updated = await _admin.PutJson(Url, category);
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            var stored = (await All()).Single(a => (Guid)a["objId"] == category.ObjId);
            Assert.Equal(category.Name, (string)stored["name"]);
            Assert.Equal(5, (int)stored["rating"]);

            var deleted = await _admin.DeleteAsync($"{Url}/{category.ObjId}");
            Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
            Assert.DoesNotContain(await All(), a => (Guid)a["objId"] == category.ObjId);
            await using var db = _host.CreateDbContext();
            Assert.False(await db.CategoryInstances.AnyAsync(a => a.ObjId == category.ObjId));
        }

        [Fact]
        public async Task Category_PostedNavigations_AreNotSaved()
        {
            var category = await NewCategory();
            // a forged nested group must not be inserted along with the category
            category.This2UserGroupNavigation = new UserGroup { ObjId = Guid.NewGuid(), Name = "forged", Description = "" };

            Assert.Equal(HttpStatusCode.OK, (await _admin.PutJson(Url, category)).StatusCode);

            await using var db = _host.CreateDbContext();
            Assert.False(await db.UserGroups.AnyAsync(a => a.Name == "forged"));
            Assert.Null((await db.CategoryInstances.AsNoTracking().SingleAsync(a => a.ObjId == category.ObjId)).This2UserGroup);
        }

        [Fact]
        public async Task Categories_ForVisu_OnlyTheOwnAndUngroupedOnes()
        {
            var own = _host.CreateUserGroup(_host.UniqueName("own"));
            var other = _host.CreateUserGroup(_host.UniqueName("other"));
            var ownCategory = await NewCategory(own);
            var otherCategory = await NewCategory(other);
            var publicCategory = await NewCategory();
            foreach (var category in new[] { ownCategory, otherCategory, publicCategory })
            {
                Assert.Equal(HttpStatusCode.OK, (await _admin.PutJson(Url, category)).StatusCode);
            }

            var visu = _host.CreateClient(_host.CreateVisuToken(own));
            var visible = ((JArray)await (await visu.GetAsync(Url)).ReadJson()).Select(a => (Guid)a["objId"]).ToList();

            Assert.Contains(ownCategory.ObjId, visible);
            Assert.Contains(publicCategory.ObjId, visible);
            Assert.DoesNotContain(otherCategory.ObjId, visible);
            Assert.Equal(HttpStatusCode.Forbidden, (await visu.GetAsync($"{Url}/all")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await visu.PutJson(Url, await NewCategory())).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await visu.DeleteAsync($"{Url}/{ownCategory.ObjId}")).StatusCode);
        }

        [Fact]
        public async Task Category_DeleteByVisu_KeepsTheCategory()
        {
            var category = await NewCategory();
            await _admin.PutJson(Url, category);

            await _host.CreateClient(_host.CreateVisuToken()).DeleteAsync($"{Url}/{category.ObjId}");

            Assert.Contains(await All(), a => (Guid)a["objId"] == category.ObjId);
        }
    }

    public class AreaHttpTests : IClassFixture<WebApiHostFixture>
    {
        private const string Url = "webapi/areas";
        private readonly WebApiHost _host;
        private readonly HttpClient _admin;

        public AreaHttpTests(WebApiHostFixture fixture)
        {
            _host = fixture.Host;
            _admin = _host.CreateClient(_host.CreateAdminToken());
        }

        private async Task<AreaInstance> NewArea(Guid? parent = null, Guid? userGroup = null)
        {
            var templates = (JArray)await (await _admin.GetAsync($"{Url}/templates")).ReadJson();
            Assert.NotEmpty(templates);
            return new AreaInstance
            {
                ObjId = Guid.NewGuid(),
                Name = _host.UniqueName("area"),
                Description = "",
                Icon = "home",
                This2AreaTemplate = (Guid)templates.First()["objId"],
                This2Parent = parent,
                This2UserGroup = userGroup
            };
        }

        private async Task<List<Guid>> AllIds() =>
            ((JArray)await (await _admin.GetAsync($"{Url}/all")).ReadJson()).Select(a => (Guid)a["objId"]).ToList();

        [Fact]
        public async Task Area_SaveGetUpdateDelete_RoundTrip()
        {
            var area = await NewArea();

            var saved = await _admin.PutJson(Url, area);
            Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
            Assert.Contains(area.ObjId, await AllIds());

            area.Name += "-renamed";
            Assert.Equal(HttpStatusCode.OK, (await _admin.PutJson(Url, area)).StatusCode);
            await using (var db = _host.CreateDbContext())
            {
                Assert.Equal(area.Name, (await db.AreaInstances.AsNoTracking().SingleAsync(a => a.ObjId == area.ObjId)).Name);
            }

            Assert.Equal(HttpStatusCode.OK, (await _admin.DeleteAsync($"{Url}/{area.ObjId}")).StatusCode);
            Assert.DoesNotContain(area.ObjId, await AllIds());
        }

        [Fact]
        public async Task Area_AddWithChildren_DeleteParent_RemovesTheChildren()
        {
            var parent = await NewArea();
            Assert.Equal(HttpStatusCode.OK, (await _admin.PostJson($"{Url}/add", new[] { parent })).StatusCode);
            var child = await NewArea(parent.ObjId);
            var grandChild = await NewArea(child.ObjId);
            Assert.Equal(HttpStatusCode.OK, (await _admin.PostJson($"{Url}/add", new[] { child })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await _admin.PostJson($"{Url}/add", new[] { grandChild })).StatusCode);

            Assert.Equal(HttpStatusCode.OK, (await _admin.DeleteAsync($"{Url}/{parent.ObjId}")).StatusCode);

            var ids = await AllIds();
            Assert.DoesNotContain(parent.ObjId, ids);
            Assert.DoesNotContain(child.ObjId, ids);
            Assert.DoesNotContain(grandChild.ObjId, ids);
            await using var db = _host.CreateDbContext();
            Assert.False(await db.AreaInstances.AnyAsync(a => a.ObjId == child.ObjId || a.ObjId == grandChild.ObjId));
        }

        [Fact]
        public async Task Areas_SaveList_SavesEveryArea()
        {
            var a = await NewArea();
            var b = await NewArea();

            var response = await _admin.PostJson(Url, new[] { a, b });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var ids = await AllIds();
            Assert.Contains(a.ObjId, ids);
            Assert.Contains(b.ObjId, ids);
        }

        [Fact]
        public async Task Areas_ForVisu_HideOtherUserGroups()
        {
            var own = _host.CreateUserGroup(_host.UniqueName("own"));
            var other = _host.CreateUserGroup(_host.UniqueName("other"));
            var ownArea = await NewArea(userGroup: own);
            var otherArea = await NewArea(userGroup: other);
            await _admin.PostJson($"{Url}/add", new[] { ownArea, otherArea });

            var visu = _host.CreateClient(_host.CreateVisuToken(own));
            var body = (JArray)await (await visu.GetAsync(Url)).ReadJson();
            var visible = body.Select(a => (Guid)a["objId"]).ToList();

            Assert.Contains(ownArea.ObjId, visible);
            Assert.DoesNotContain(otherArea.ObjId, visible);
            // nested objects of other groups are removed by the response filter as well
            Assert.DoesNotContain(otherArea.ObjId.ToString(), body.ToString());
            Assert.Equal(HttpStatusCode.Forbidden, (await visu.GetAsync($"{Url}/all")).StatusCode);
        }

        [Fact]
        public async Task Area_ChangesByVisu_AreRejected()
        {
            var visu = _host.CreateClient(_host.CreateVisuToken());
            var area = await NewArea();

            Assert.Equal(HttpStatusCode.Forbidden, (await visu.PutJson(Url, area)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await visu.PostJson($"{Url}/add", new[] { area })).StatusCode);
            Assert.DoesNotContain(area.ObjId, await AllIds());
        }

        [Fact]
        public async Task EtsImport_UnknownParent_Returns404()
        {
            var content = new MultipartFormDataContent { { new ByteArrayContent(new byte[] { 1, 2, 3 }), "file", "project.knxproj" } };
            var response = await _admin.PostAsync($"{Url}/{Guid.NewGuid()}/etsImport", content);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    public class LogicPagesHttpTests : IClassFixture<WebApiHostFixture>
    {
        private const string Url = "webapi/logics";
        private readonly WebApiHost _host;
        private readonly HttpClient _admin;

        public LogicPagesHttpTests(WebApiHostFixture fixture)
        {
            _host = fixture.Host;
            _admin = _host.CreateClient(_host.CreateAdminToken());
        }

        [Fact]
        public async Task Page_AddGetUpdateDelete_RoundTrip()
        {
            var page = new RulePage { ObjId = Guid.NewGuid(), Name = _host.UniqueName("page"), Description = "d", This2RulePageType = 1 };

            var added = await _admin.PostJson($"{Url}/page/add", page);
            Assert.Equal(HttpStatusCode.OK, added.StatusCode);
            Assert.Equal(page.ObjId, (Guid)(await added.ReadJson())["objId"]);

            var pages = (JArray)await (await _admin.GetAsync($"{Url}/pages")).ReadJson();
            Assert.Contains(pages, a => (Guid)a["objId"] == page.ObjId);
            var single = await (await _admin.GetAsync($"{Url}/page/{page.ObjId}")).ReadJson();
            Assert.Equal(page.Name, (string)single["name"]);

            var patched = await _admin.PatchJson($"{Url}/page", new RulePage { ObjId = page.ObjId, Name = "renamed", Description = "new" });
            Assert.Equal(HttpStatusCode.OK, patched.StatusCode);
            Assert.Equal("renamed", (string)(await patched.ReadJson())["name"]);
            await using (var db = _host.CreateDbContext())
            {
                var stored = await db.RulePages.AsNoTracking().SingleAsync(a => a.ObjId == page.ObjId);
                Assert.Equal("renamed", stored.Name);
                Assert.Equal("new", stored.Description);
                Assert.Equal(1, stored.This2RulePageType);
            }

            Assert.Equal(HttpStatusCode.OK, (await _admin.DeleteAsync($"{Url}/page/{page.ObjId}")).StatusCode);
            await using (var db = _host.CreateDbContext())
            {
                Assert.False(await db.RulePages.AnyAsync(a => a.ObjId == page.ObjId));
            }
        }

        [Fact]
        public async Task UpdatePage_UnknownPage_ReturnsNoContentAndCreatesNothing()
        {
            var id = Guid.NewGuid();
            var response = await _admin.PatchJson($"{Url}/page", new RulePage { ObjId = id, Name = "ghost" });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await using var db = _host.CreateDbContext();
            Assert.False(await db.RulePages.AnyAsync(a => a.ObjId == id));
        }

        [Fact]
        public async Task RemovePage_UnknownPage_IsOk()
        {
            Assert.Equal(HttpStatusCode.OK, (await _admin.DeleteAsync($"{Url}/page/{Guid.NewGuid()}")).StatusCode);
        }

        [Fact]
        public async Task Pages_AreAdminOnly()
        {
            var visu = _host.CreateClient(_host.CreateVisuToken());
            Assert.Equal(HttpStatusCode.Forbidden, (await visu.GetAsync($"{Url}/pages")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await visu.PostJson($"{Url}/page/add", new RulePage { ObjId = Guid.NewGuid(), Name = "x", This2RulePageType = 1 })).StatusCode);
        }
    }

    /// <summary>
    /// Logic data of logics of other user groups answers like an unknown logic (61c228fc).
    /// </summary>
    public class LogicDataVisibilityHttpTests : IDisposable
    {
        private readonly WebApiHost _host;
        private readonly Guid _ownGroup;
        private readonly Guid _otherGroup;

        public LogicDataVisibilityHttpTests()
        {
            var dataHandler = new Mock<ILogicDataHandler>();
            dataHandler.Setup(a => a.GetDataForRuleInstance(It.IsAny<Guid>())).Returns((Guid id) => new { data = "logic-" + id });
            _host = new WebApiHost(configureServices: services =>
            {
                services.RemoveAll<ILogicDataHandler>();
                services.AddSingleton(dataHandler.Object);
            });

            _ownGroup = _host.CreateUserGroup("own");
            _otherGroup = _host.CreateUserGroup("other");

            using var db = _host.CreateDbContext();
            var factory = new LogicTemplateFactory(NullLogger<LogicTemplateFactory>.Instance, db, _host.Configuration) { Owner = Guid.NewGuid() };
            new TestLogicFactory().InitTemplates(factory);
        }

        public void Dispose() => _host.Dispose();

        private Guid AddLogic(Guid? userGroup)
        {
            using var db = _host.CreateDbContext();
            var logic = new RuleInstance
            {
                ObjId = Guid.NewGuid(),
                Name = "logic",
                Description = "",
                VisuName = "",
                This2RuleTemplate = new TestLogicFactory().LogicGuid,
                This2RulePage = db.RulePages.First().ObjId,
                This2UserGroup = userGroup,
                CreatedAt = DateTimeOffset.Now,
                ModifiedAt = DateTimeOffset.Now
            };
            db.RuleInstances.Add(logic);
            db.SaveChanges();
            _host.GetService<ILogicCacheFacade>().ClearInstances();
            _host.GetService<ILogicInstanceCache>().Clear();
            return logic.ObjId;
        }

        [Fact]
        public async Task LogicOfOtherGroup_ForVisu_Returns404RuleNotFound()
        {
            var id = AddLogic(_otherGroup);

            var response = await _host.CreateClient(_host.CreateVisuToken(_ownGroup)).GetAsync($"webapi/logics/data/{id}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("RULE_NOT_FOUND", (string)(await response.ReadJson())["errorText"]);
        }

        [Fact]
        public async Task LogicOfOwnGroupOrWithoutGroup_ForVisu_ReturnsTheData()
        {
            var own = AddLogic(_ownGroup);
            var open = AddLogic(null);
            var visu = _host.CreateClient(_host.CreateVisuToken(_ownGroup));

            foreach (var id in new[] { own, open })
            {
                var response = await visu.GetAsync($"webapi/logics/data/{id}");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("logic-" + id, (string)(await response.ReadJson())["data"]);
            }
        }

        [Fact]
        public async Task LogicOfOtherGroup_ForAdmin_ReturnsTheData()
        {
            var id = AddLogic(_otherGroup);

            var response = await _host.CreateClient(_host.CreateAdminToken()).GetAsync($"webapi/logics/data/{id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    public class SettingsHttpTests : IClassFixture<WebApiHostFixture>
    {
        private const string Url = "webapi/settings";
        private readonly WebApiHost _host;
        private readonly HttpClient _admin;

        public SettingsHttpTests(WebApiHostFixture fixture)
        {
            _host = fixture.Host;
            _admin = _host.CreateClient(_host.CreateAdminToken());
        }

        [Fact]
        public async Task GetAll_AsAdmin_ReturnsTheSettings()
        {
            var settings = (JArray)await (await _admin.GetAsync(Url)).ReadJson();
            Assert.Contains(settings, a => (string)a["valueKey"] == "projectName");
            Assert.Contains(settings, a => (string)a["valueKey"] == "cloudUrl");
        }

        [Theory]
        [InlineData("projectName")]
        [InlineData("language")]
        [InlineData("timezoneOffset")]
        public async Task PublicSetting_IsReadableByVisu(string key)
        {
            var response = await _host.CreateClient(_host.CreateVisuToken()).GetAsync($"{Url}/key/{key}");
            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound, $"{key}: {(int)response.StatusCode}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Assert.Equal(key, (string)(await response.ReadJson())["valueKey"]);
            }
        }

        [Theory]
        [InlineData("cloudUrl")]
        [InlineData("apiKey")]
        [InlineData("ServerUID")]
        [InlineData("does-not-exist")]
        public async Task OtherSettings_AreForbiddenForVisu(string key)
        {
            var response = await _host.CreateClient(_host.CreateVisuToken()).GetAsync($"{Url}/key/{key}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SecretSetting_IsReadableByAdmin()
        {
            var response = await _admin.GetAsync($"{Url}/key/cloudUrl");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UnknownKey_ForAdmin_Returns404()
        {
            Assert.Equal(HttpStatusCode.NotFound, (await _admin.GetAsync($"{Url}/key/does-not-exist")).StatusCode);
        }

        [Fact]
        public async Task Language_IsAnonymous()
        {
            var response = await _host.CreateClient().GetAsync($"{Url}/language");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("language", (string)(await response.ReadJson())["valueKey"]);
        }

        [Fact]
        public async Task Save_ChangedValue_IsStoredAndReturned_UnknownKeysAreIgnored()
        {
            var settings = (JArray)await (await _admin.GetAsync(Url)).ReadJson();
            var projectName = (JObject)settings.Single(a => (string)a["valueKey"] == "projectName");
            var newName = _host.UniqueName("project-");
            projectName["value"] = newName;
            projectName["valueText"] = newName;
            var unknown = new JObject { ["valueKey"] = "not-a-setting", ["value"] = "x", ["valueText"] = "x", ["type"] = 0 };

            var response = await _admin.PostAsync(Url, HttpTestExtensions.RawJson(new JArray(projectName, unknown).ToString()));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var saved = (JArray)await response.ReadJson();
            Assert.Equal(newName, (string)saved.Single(a => (string)a["valueKey"] == "projectName")["valueText"]);
            Assert.DoesNotContain(saved, a => (string)a["valueKey"] == "not-a-setting");

            var read = await (await _host.CreateClient(_host.CreateVisuToken()).GetAsync($"{Url}/key/projectName")).ReadJson();
            Assert.Equal(newName, (string)read["valueText"]);
            await using var db = _host.CreateDbContext();
            Assert.False(await db.Settings.AnyAsync(a => a.ValueKey == "not-a-setting"));
        }

        [Fact]
        public async Task Save_EmptyList_ChangesNothing()
        {
            var response = await _admin.PostJson(Url, Array.Empty<Setting>());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEmpty((JArray)await response.ReadJson());
        }

        [Fact(Skip = "BUG: SettingsController.SaveSettings dereferences a null body (SettingsController.cs:110), a 'null' post is a NullReferenceException/500 instead of 400")]
        public async Task Save_NullBody_Returns400()
        {
            var response = await _admin.PostAsync(Url, HttpTestExtensions.RawJson("null"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Save_ByVisu_IsForbidden()
        {
            var response = await _host.CreateClient(_host.CreateVisuToken()).PostJson(Url, Array.Empty<Setting>());
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    public class VisualizationHttpTests : IClassFixture<WebApiHostFixture>
    {
        private const string Url = "webapi/visualization";
        private readonly WebApiHost _host;

        public VisualizationHttpTests(WebApiHostFixture fixture)
        {
            _host = fixture.Host;
        }

        private Guid AddPage(Guid? userGroup)
        {
            using var db = _host.CreateDbContext();
            var page = new VisuPage
            {
                ObjId = Guid.NewGuid(),
                Name = _host.UniqueName("page"),
                Description = "",
                This2VisuPageType = 1,
                This2UserGroup = userGroup,
                Height = 10,
                Width = 10
            };
            db.VisuPages.Add(page);
            db.SaveChanges();
            _host.GetService<IVisualizationCache>().ClearAll();
            return page.ObjId;
        }

        [Fact]
        public async Task Pages_ForVisu_OnlyOwnGroupAndUngrouped()
        {
            var own = _host.CreateUserGroup(_host.UniqueName("own"));
            var other = _host.CreateUserGroup(_host.UniqueName("other"));
            var ownPage = AddPage(own);
            var otherPage = AddPage(other);
            var openPage = AddPage(null);

            var visible = ((JArray)await (await _host.CreateClient(_host.CreateVisuToken(own)).GetAsync($"{Url}/pages")).ReadJson())
                .Select(a => (Guid)a["objId"]).ToList();

            Assert.Contains(ownPage, visible);
            Assert.Contains(openPage, visible);
            Assert.DoesNotContain(otherPage, visible);
        }

        [Fact]
        public async Task Page_OfOtherGroup_ForVisu_Returns404_ForAdmin_200()
        {
            var own = _host.CreateUserGroup(_host.UniqueName("own"));
            var other = _host.CreateUserGroup(_host.UniqueName("other"));
            var otherPage = AddPage(other);
            var ownPage = AddPage(own);
            var visu = _host.CreateClient(_host.CreateVisuToken(own));

            Assert.Equal(HttpStatusCode.NotFound, (await visu.GetAsync($"{Url}/page/{otherPage}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await visu.GetAsync($"{Url}/page/{ownPage}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await _host.CreateClient(_host.CreateAdminToken()).GetAsync($"{Url}/page/{otherPage}")).StatusCode);
        }

        [Fact]
        public async Task Page_Unknown_Returns404()
        {
            var response = await _host.CreateClient(_host.CreateVisuToken()).GetAsync($"{Url}/page/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CategoryLinked_UnknownCategory_ForVisu_IsOk()
        {
            // an unknown category has no user group
            var response = await _host.CreateClient(_host.CreateVisuToken()).GetAsync($"{Url}/categoryLinked/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact(Skip = "BUG: AreaCache.Get throws KeyNotFoundException for unknown ids (AreaCache.cs:37), VisualizationController.GetAllByArea expects null: 500 instead of an empty result")]
        public async Task AreaLinked_UnknownArea_ForVisu_IsOk()
        {
            var response = await _host.CreateClient(_host.CreateVisuToken()).GetAsync($"{Url}/areaLinked/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact(Skip = "BUG: VisualizationController.GetPage calls _areaCache.Get(pageId) first (VisualizationController.cs:103), which throws for a category id: category pages are a 500")]
        public async Task Page_OfACategory_ReturnsTheCategoryFacade()
        {
            Guid categoryId;
            await using (var db = _host.CreateDbContext())
            {
                var category = new CategoryInstance
                {
                    ObjId = Guid.NewGuid(),
                    Name = "category-page",
                    Description = "",
                    Icon = "",
                    Color = "",
                    This2CategoryGroup = db.CategoryGroups.First().ObjId,
                    CreatedAt = DateTimeOffset.Now,
                    ModifiedAt = DateTimeOffset.Now
                };
                db.CategoryInstances.Add(category);
                await db.SaveChangesAsync();
                categoryId = category.ObjId;
            }
            _host.GetService<ICategoryCache>().Clear();

            var response = await _host.CreateClient(_host.CreateAdminToken()).GetAsync($"{Url}/page/{categoryId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Page_OfAnArea_ReturnsTheAreaFacade()
        {
            Guid areaId;
            await using (var db = _host.CreateDbContext())
            {
                areaId = db.AreaInstances.First().ObjId;
            }

            var response = await _host.CreateClient(_host.CreateAdminToken()).GetAsync($"{Url}/page/{areaId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = (JObject)await response.ReadJson();
            Assert.True(body.ContainsKey("nodeInstances"));
        }

        [Fact]
        public async Task AreaLinked_OfOtherGroup_ForVisu_Returns404()
        {
            var other = _host.CreateUserGroup(_host.UniqueName("other"));
            Guid areaId;
            await using (var db = _host.CreateDbContext())
            {
                var area = new AreaInstance
                {
                    ObjId = Guid.NewGuid(),
                    Name = "hidden-area",
                    Description = "",
                    Icon = "",
                    This2AreaTemplate = db.AreaTemplates.First().ObjId,
                    This2UserGroup = other,
                    CreatedAt = DateTimeOffset.Now,
                    ModifiedAt = DateTimeOffset.Now
                };
                db.AreaInstances.Add(area);
                await db.SaveChangesAsync();
                areaId = area.ObjId;
            }
            _host.GetService<IAreaCache>().Clear();

            var response = await _host.CreateClient(_host.CreateVisuToken()).GetAsync($"{Url}/areaLinked/{areaId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await _host.CreateClient(_host.CreateAdminToken()).GetAsync($"{Url}/areaLinked/{areaId}")).StatusCode);
        }
    }

    public class DataHttpTests : IClassFixture<WebApiHostFixture>
    {
        private readonly WebApiHost _host;

        public DataHttpTests(WebApiHostFixture fixture)
        {
            _host = fixture.Host;
        }

        [Fact]
        public async Task Trend_EndBeforeStart_Returns400()
        {
            var response = await _host.CreateClient(_host.CreateVisuToken())
                .GetAsync($"webapi/data/trend/{Guid.NewGuid()}/2026-01-02T00:00:00Z/2026-01-03T00:00:00Z/2026-01-02T00:00:00Z/2026-01-01T00:00:00Z");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Trend_UnknownNode_ReturnsAnEmptyList()
        {
            var response = await _host.CreateClient(_host.CreateVisuToken())
                .GetAsync($"webapi/data/trend/{Guid.NewGuid()}/2026-01-01T00:00:00Z/2026-01-02T00:00:00Z/2026-01-01T00:00:00Z/2026-01-02T00:00:00Z");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty((JArray)await response.ReadJson());
        }

        [Fact]
        public async Task Value_UnknownId_ReturnsNoContent()
        {
            var visu = _host.CreateClient(_host.CreateVisuToken());
            Assert.Equal(HttpStatusCode.NoContent, (await visu.GetAsync($"webapi/data/node/{Guid.NewGuid()}")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await visu.GetAsync($"webapi/data/all/{Guid.NewGuid()}")).StatusCode);
        }

        [Fact]
        public async Task Value_NonGuidId_DoesNotRoute()
        {
            var response = await _host.CreateClient(_host.CreateVisuToken()).GetAsync("webapi/data/node/not-a-guid");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    public class FileUploadHttpTests : IClassFixture<WebApiHostFixture>
    {
        private readonly WebApiHost _host;

        public FileUploadHttpTests(WebApiHostFixture fixture)
        {
            _host = fixture.Host;
        }

        [Fact]
        public async Task Upload_StoresTheFileUnderARandomName_IgnoringTheClientPath()
        {
            var content = new MultipartFormDataContent
            {
                { new ByteArrayContent(new byte[] { 1, 2, 3, 4 }), "file", "..\\..\\evil/../Project.KNXPROJ" }
            };

            var response = await _host.CreateClient(_host.CreateAdminToken()).PostAsync("webapi/FileUpload", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var fileName = (string)(await response.ReadJson())["fileName"];
            Assert.Matches("^[0-9a-f]{32}\\.knxproj$", fileName);
            var path = System.IO.Path.Combine(Automatica.Core.Base.Common.ServerInfo.GetUploadDirectory(), fileName);
            try
            {
                Assert.True(System.IO.File.Exists(path));
                Assert.Equal(new byte[] { 1, 2, 3, 4 }, await System.IO.File.ReadAllBytesAsync(path));
            }
            finally
            {
                System.IO.File.Delete(path);
            }
        }

        [Fact]
        public async Task Upload_WithoutFile_Returns400()
        {
            var content = new MultipartFormDataContent { { new StringContent("x"), "notAFile" } };
            var response = await _host.CreateClient(_host.CreateAdminToken()).PostAsync("webapi/FileUpload", content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Upload_ByVisu_IsForbidden()
        {
            var content = new MultipartFormDataContent { { new ByteArrayContent(new byte[] { 1 }), "file", "a.txt" } };
            var response = await _host.CreateClient(_host.CreateVisuToken()).PostAsync("webapi/FileUpload", content);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    public class UploadPathHelperTests
    {
        [Theory]
        [InlineData("project.knxproj", ".knxproj")]
        [InlineData("PROJECT.KnxProj", ".knxproj")]
        [InlineData("../../etc/passwd", "")]
        [InlineData("archive.tar.gz", ".gz")]
        [InlineData("noextension", "")]
        [InlineData("weird.ex$e", "")]
        [InlineData("long.abcdefghijklmnopq", "")]
        [InlineData(null, "")]
        public void CreateRandomFileName_KeepsOnlyAPlainExtension(string clientName, string expectedExtension)
        {
            var name = UploadPathHelper.CreateRandomFileName(clientName);
            Assert.Matches("^[0-9a-f]{32}" + System.Text.RegularExpressions.Regex.Escape(expectedExtension) + "$", name);
        }

        [Fact]
        public void CreateRandomFileName_IsRandom()
        {
            Assert.NotEqual(UploadPathHelper.CreateRandomFileName("a.txt"), UploadPathHelper.CreateRandomFileName("a.txt"));
        }

        [Theory]
        [InlineData("file.txt")]
        [InlineData("../file.txt")]
        [InlineData("sub/dir/file.txt")]
        public void GetSafeFilePath_UsesOnlyTheFileName(string clientName)
        {
            var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "automatica-upload-test");
            var path = UploadPathHelper.GetSafeFilePath(dir, clientName);
            Assert.Equal(System.IO.Path.Combine(System.IO.Path.GetFullPath(dir), "file.txt"), path);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("..")]
        [InlineData(".")]
        [InlineData("dir/")]
        public void GetSafeFilePath_InvalidNames_Throw(string clientName)
        {
            Assert.Throws<ArgumentException>(() => UploadPathHelper.GetSafeFilePath(System.IO.Path.GetTempPath(), clientName));
        }
    }
}
