import { ChangeDetectionStrategy, Component, NO_ERRORS_SCHEMA, ViewChild } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { L10nTranslationService } from "angular-l10n";

import { LogicBlockPickerComponent, normalizeSearch } from "./logic-block-picker.component";
import { RuleTemplate } from "src/app/base/model/rule-template";
import { FakeTranslateAsyncPipe, createTranslateSpy, key, pointer } from "src/app/testing/test-helpers";

function template(id: string, name: string, group: string, description?: string) {
  const t = new RuleTemplate();
  t.ObjId = id;
  t.Name = name;
  t.Group = group;
  t.Description = description;
  return t;
}

@Component({
  template: `<button id="anchor" style="position: fixed; left: 100px; top: 20px; height: 30px;">Blocks</button>
    <p3-logic-block-picker [templates]="templates" [anchorElement]="anchor" (pick)="picked.push($event)" (close)="closed = closed + 1"></p3-logic-block-picker>`,
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: false
})
class HostComponent {
  templates: RuleTemplate[] = [];
  anchor: HTMLElement;
  picked: RuleTemplate[] = [];
  closed = 0;
  @ViewChild(LogicBlockPickerComponent) picker: LogicBlockPickerComponent;
}

describe("LogicBlockPickerComponent", () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let picker: LogicBlockPickerComponent;

  const and = template("t-and", "AND.NAME", "GROUP.LOGIC", "AND.DESCRIPTION");
  const or = template("t-or", "OR.NAME", "GROUP.LOGIC");
  const twilight = template("t-twilight", "TWILIGHT.NAME", "GROUP.TIME", "TWILIGHT.DESCRIPTION");
  const timer = template("t-timer", "TIMER.NAME", "GROUP.TIME", "TIMER.DESCRIPTION");
  const add = template("t-add", "ADD.NAME", "GROUP.MATH", "ADD.DESCRIPTION");

  beforeEach(() => {
    const translate = createTranslateSpy({
      "AND.NAME": "Und", "AND.DESCRIPTION": "Alle Eingänge müssen wahr sein",
      "OR.NAME": "Oder",
      "TWILIGHT.NAME": "Dämmerungsschalter", "TWILIGHT.DESCRIPTION": "Schaltet bei Sonnenuntergang",
      "TIMER.NAME": "Zeitschaltuhr", "TIMER.DESCRIPTION": "Schaltet zu festen Zeiten",
      "ADD.NAME": "Addition", "ADD.DESCRIPTION": "Addiert zwei Werte und den Timer-Offset",
      "GROUP.LOGIC": "Logik", "GROUP.TIME": "Zeit", "GROUP.MATH": "Mathematik"
    });

    TestBed.configureTestingModule({
      declarations: [HostComponent, LogicBlockPickerComponent],
      imports: [FakeTranslateAsyncPipe],
      providers: [{ provide: L10nTranslationService, useValue: translate }],
      schemas: [NO_ERRORS_SCHEMA]
    });

    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    host.templates = [timer, and, add, twilight, or];
    fixture.detectChanges();
    host.anchor = fixture.nativeElement.querySelector("#anchor");
    fixture.detectChanges();
    picker = host.picker;
  });

  function input(): HTMLInputElement {
    return fixture.nativeElement.querySelector(".search input");
  }

  function type(value: string) {
    input().value = value;
    input().dispatchEvent(new Event("input"));
    fixture.detectChanges();
  }

  function names() {
    return [...fixture.nativeElement.querySelectorAll(".item .name")].map((a: HTMLElement) => a.textContent.trim());
  }

  it("normalizeSearch_IgnoresCaseAccentsAndSharpS", () => {
    expect(normalizeSearch(" Dämmerung ")).toBe("dammerung");
    expect(normalizeSearch("Straße")).toBe("strasse");
    expect(normalizeSearch(undefined)).toBe("");
  });

  it("open_FocusesTheSearch", () => {
    expect(document.activeElement).toBe(input());
  });

  it("emptySearch_ShowsAllBlocksGroupedAndSorted", () => {
    const groups = [...fixture.nativeElement.querySelectorAll(".group")].map((a: HTMLElement) => a.textContent.trim());

    expect(groups).toEqual(["Logik", "Mathematik", "Zeit"]);
    expect(names()).toEqual(["Oder", "Und", "Addition", "Dämmerungsschalter", "Zeitschaltuhr"]);
    expect(fixture.nativeElement.querySelector(".item.active .name").textContent.trim()).toBe("Oder");
  });

  it("search_MatchesTheNameWithoutAccents", () => {
    type("dammer");

    expect(names()).toEqual(["Dämmerungsschalter"]);
    expect(fixture.nativeElement.querySelector(".group")).toBeNull();
    expect(fixture.nativeElement.querySelector(".item .tag").textContent.trim()).toBe("Zeit");
  });

  it("search_MatchesGroupAndDescription", () => {
    type("zeit");
    // the name first, then the blocks whose group or description matches
    expect(names()).toEqual(["Zeitschaltuhr", "Dämmerungsschalter"]);

    type("sonnenuntergang");
    expect(names()).toEqual(["Dämmerungsschalter"]);
  });

  it("search_EveryWordHasToMatch", () => {
    type("schalt sonne");

    expect(names()).toEqual(["Dämmerungsschalter"]);
  });

  it("search_NameStartRanksBeforeAMatchInTheDescription", () => {
    type("timer");

    // "Timer" is the untranslated key of the clock and in the description of Addition
    expect(names()).toEqual(["Addition", "Zeitschaltuhr"]);

    type("add");
    expect(names()[0]).toBe("Addition");
  });

  it("search_UntranslatedKeyFindsTheBlock", () => {
    type("and.name");

    expect(names()).toEqual(["Und"]);
  });

  it("search_NoMatch_SaysSo", () => {
    type("xyz");

    expect(names()).toEqual([]);
    expect(fixture.nativeElement.querySelector(".none")).not.toBeNull();
  });

  it("description_MissingTranslation_IsNotShown", () => {
    const rows = [...fixture.nativeElement.querySelectorAll(".item")] as HTMLElement[];
    const oder = rows.find(a => a.textContent.includes("Oder"));
    const und = rows.find(a => a.textContent.includes("Und"));

    expect(oder.querySelector(".desc")).toBeNull();
    expect(und.querySelector(".desc").textContent.trim()).toBe("Alle Eingänge müssen wahr sein");
  });

  it("arrowKeysAndEnter_PickTheActiveBlock", () => {
    type("schalt");   // Dämmerungsschalter, Zeitschaltuhr
    input().dispatchEvent(key("ArrowDown"));
    input().dispatchEvent(key("ArrowDown"));   // stays on the last one
    input().dispatchEvent(key("ArrowUp"));
    input().dispatchEvent(key("ArrowDown"));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector(".item.active .name").textContent.trim()).toBe("Zeitschaltuhr");

    input().dispatchEvent(key("Enter"));

    expect(host.picked).toEqual([timer]);
  });

  it("enter_WithoutResults_PicksNothing", () => {
    type("xyz");
    input().dispatchEvent(key("Enter"));

    expect(host.picked).toEqual([]);
  });

  it("typing_StartsAgainAtTheFirstResult", () => {
    input().dispatchEvent(key("ArrowDown"));
    type("z");

    expect(picker.active).toBe(0);
  });

  it("escape_Closes", () => {
    input().dispatchEvent(key("Escape"));

    expect(host.closed).toBe(1);
  });

  it("click_PicksTheBlock", () => {
    type("add");
    (fixture.nativeElement.querySelector(".item") as HTMLElement).click();

    expect(host.picked).toEqual([add]);
  });

  it("pointerDown_OutsideClosesButInsideAndOnTheAnchorDoNot", () => {
    input().dispatchEvent(pointer("pointerdown"));
    host.anchor.dispatchEvent(pointer("pointerdown"));
    expect(host.closed).toBe(0);

    document.body.dispatchEvent(pointer("pointerdown"));
    expect(host.closed).toBe(1);
  });

  it("place_OpensUnderTheAnchorInsideTheWindow", () => {
    const panel: HTMLElement = fixture.nativeElement.querySelector(".panel");
    const anchor = host.anchor.getBoundingClientRect();

    expect(panel.style.top).toBe(`${anchor.bottom + 4}px`);
    expect(parseFloat(panel.style.left)).toBe(Math.max(8, Math.min(anchor.left, window.innerWidth - parseFloat(panel.style.width) - 8)));
    expect(parseFloat(panel.style.width)).toBeLessThanOrEqual(380);
  });

  it("destroy_StopsListeningForOutsideClicks", () => {
    fixture.destroy();

    document.body.dispatchEvent(pointer("pointerdown"));

    expect(host.closed).toBe(0);
  });
});
