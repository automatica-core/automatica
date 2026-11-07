import { TestBed } from "@angular/core/testing";
import { currentTheme } from "devextreme/viz/themes";

import { ThemeService } from "./theme.service";
import { isolateLocalStorage } from "../testing/test-helpers";

const STORAGE_KEY = "automaticaTheme";

/** a stylesheet link like the ones the build emits; data urls so nothing is fetched */
function addSheet(name: string, rel = "stylesheet") {
  const link = document.createElement("link");
  link.rel = rel;
  link.href = "data:text/css;charset=utf-8," + encodeURIComponent(`/* ${name} */`) + "#" + name;
  link.setAttribute("data-test-sheet", name);
  document.head.appendChild(link);
  return link;
}

describe("ThemeService", () => {
  let service: ThemeService;
  let restore: () => void;
  let previousDataTheme: string;
  let previousDxTheme: string;

  beforeEach(() => {
    restore = isolateLocalStorage(STORAGE_KEY);
    previousDataTheme = document.documentElement.dataset.theme;
    previousDxTheme = currentTheme();
    TestBed.configureTestingModule({ providers: [ThemeService] });
    service = TestBed.inject(ThemeService);
  });

  afterEach(() => {
    document.querySelectorAll("link[data-test-sheet]").forEach(a => a.remove());
    if (previousDataTheme === undefined) {
      delete document.documentElement.dataset.theme;
    } else {
      document.documentElement.dataset.theme = previousDataTheme;
    }
    currentTheme(previousDxTheme);
    restore();
  });

  describe("stylesheet switching", () => {
    let light: HTMLLinkElement;
    let dark: HTMLLinkElement;
    let app: HTMLLinkElement;

    beforeEach(() => {
      // the hashed bundle names of an ng build
      light = addSheet("generic.light-5QHUJ2LX.css");
      dark = addSheet("generic.dark-HQ7W6K3B.css");
      app = addSheet("styles-ABCDEF12.css");
    });

    it("applyTheme_Dark_EnablesOnlyTheDarkBundle", () => {
      service.applyTheme("dark");

      expect(dark.disabled).toBeFalse();
      expect(light.disabled).toBeTrue();
      expect(app.disabled).toBeFalse();
    });

    it("applyTheme_Light_EnablesOnlyTheLightBundle", () => {
      service.applyTheme("light");

      expect(light.disabled).toBeFalse();
      expect(dark.disabled).toBeTrue();
      expect(app.disabled).toBeFalse();
    });

    it("applyTheme_Toggle_SwitchesBackAndForth", () => {
      service.applyTheme("light");
      service.applyTheme("dark");

      expect(dark.disabled).toBeFalse();
      expect(light.disabled).toBeTrue();

      service.applyTheme("light");

      expect(light.disabled).toBeFalse();
      expect(dark.disabled).toBeTrue();
    });

    it("applyTheme_DotSeparatedNamesOnly_OtherSheetsAreNotTouched", () => {
      // the pattern is generic[.](light|dark): a backspace or word boundary typo in it once matched nothing at all
      const similar = addSheet("generic-light.css");
      const compact = addSheet("generic.lightcompact.css");
      similar.disabled = false;

      service.applyTheme("dark");

      expect(similar.disabled).toBeFalse();
      // generic.light... still belongs to the light family
      expect(compact.disabled).toBeTrue();
    });

    it("applyTheme_SheetsInSubfolders_AreMatchedToo", () => {
      const nested = addSheet("themes/generic.dark.css");
      nested.disabled = true;

      service.applyTheme("dark");

      expect(nested.disabled).toBeFalse();
    });

    it("applyTheme_LinksThatAreNoStylesheets_AreIgnored", () => {
      const preload = addSheet("generic.light-5QHUJ2LX.css", "preload");

      service.applyTheme("dark");

      expect(preload.disabled).toBeFalse();
    });
  });

  describe("theme choice", () => {
    it("applyTheme_ValidTheme_IsStoredAndSetOnTheDocument", () => {
      service.applyTheme("light");

      expect(localStorage.getItem(STORAGE_KEY)).toBe("light");
      expect(document.documentElement.dataset.theme).toBe("light");
      expect(document.documentElement.getAttribute("data-theme")).toBe("light");
      expect(service.getCurrentTheme()).toBe("light");
      expect(currentTheme()).toBe("generic.light");
    });

    it("applyTheme_NoTheme_UsesTheStoredOne", () => {
      localStorage.setItem(STORAGE_KEY, "light");

      service.applyTheme();

      expect(document.documentElement.dataset.theme).toBe("light");
      expect(service.getCurrentTheme()).toBe("light");
    });

    it("applyTheme_NothingStored_IsDark", () => {
      service.applyTheme();

      expect(document.documentElement.dataset.theme).toBe("dark");
      expect(localStorage.getItem(STORAGE_KEY)).toBe("dark");
      expect(currentTheme()).toBe("generic.dark");
    });

    it("applyTheme_UnknownTheme_FallsBackToTheStoredOne", () => {
      localStorage.setItem(STORAGE_KEY, "light");

      service.applyTheme("purple");

      expect(service.getCurrentTheme()).toBe("light");
      expect(localStorage.getItem(STORAGE_KEY)).toBe("light");
    });

    it("applyTheme_EmitsThemeChanged", () => {
      const seen: string[] = [];
      service.themeChanged.subscribe(a => seen.push(a));

      service.applyTheme("light");
      service.applyTheme("dark");

      expect(seen).toEqual(["light", "dark"]);
    });
  });
});
