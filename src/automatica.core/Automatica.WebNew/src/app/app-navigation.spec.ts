import { NavItem, navigation } from "./app-navigation";

function flatten(items: NavItem[]): NavItem[] {
  return items.flatMap(a => [a, ...flatten(a.items ?? [])]);
}

describe("app navigation", () => {
  const entries = flatten(navigation);
  const paths = entries.filter(a => a.path).map(a => a.path);

  it("navigation_HasTheExpectedGroupsInOrder", () => {
    expect(navigation.map(a => a.text)).toEqual(["UI.NAV.OVERVIEW", "UI.NAV.OPERATE", "UI.NAV.CONFIGURE", "UI.NAV.SYSTEM"]);
    expect(navigation.filter(a => a.group).every(a => a.items?.length > 0)).toBeTrue();
  });

  it("navigation_ContainsEveryAdminPage", () => {
    expect(paths).toEqual([
      "/admin/home",
      "/visualization",
      "/admin/logic-editor",
      "/admin/automations",
      "/admin/shading",
      "/admin/config",
      "/admin/area-config",
      "/admin/category-config",
      "/admin/visualisation",
      "/admin/telegram-monitor",
      "/admin/user-config",
      "/admin/usergroup-config",
      "/admin/plugins",
      "/admin/satellites",
      "/admin/logs",
      "/admin/system"
    ]);
  });

  it("navigation_DevicesPageIsUnderConfigure", () => {
    const configure = navigation.find(a => a.text === "UI.NAV.CONFIGURE");

    expect(configure.items.find(a => a.path === "/admin/config")?.text).toBe("UI.NAV.DEVICES");
  });

  it("navigation_EveryEntryHasATextKeyAndAnIcon", () => {
    for (const entry of entries) {
      expect(entry.text).withContext(JSON.stringify(entry)).toMatch(/^UI\.NAV\.[A-Z_]+$/);
      if (!entry.group) {
        expect(entry.icon).withContext(entry.text).toBeTruthy();
      }
    }
  });

  it("navigation_PathsAreUnique", () => {
    expect(new Set(paths).size).toBe(paths.length);
  });
});
