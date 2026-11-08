import {
  AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, Input,
  NgZone, OnChanges, OnDestroy, Output, ViewChild
} from "@angular/core";
import { L10nTranslationService } from "angular-l10n";
import { RuleTemplate } from "src/app/base/model/rule-template";

export interface BlockEntry {
  template: RuleTemplate;
  name: string;
  group: string;
  description: string;
  /** name, normalized for matching */
  key: string;
  /** name, group and description, normalized for matching */
  text: string;
}

export interface BlockGroup {
  name: string;
  items: BlockEntry[];
}

const WIDTH = 380;
const MARGIN = 8;

/** lower case, without accents, ß as ss: "Dämmerung" is found with "dammerung" */
export function normalizeSearch(value: string) {
  return (value ?? "").normalize("NFD").replace(/[̀-ͯ]/g, "").replace(/ß/g, "ss").toLowerCase().trim();
}

/**
 * The list of logic blocks behind "Logic blocks" in the menu: a search over name, group and description,
 * grouped while the search is empty, ranked by how well the name matches while typing.
 */
@Component({
  selector: "p3-logic-block-picker",
  templateUrl: "./logic-block-picker.component.html",
  styleUrls: ["./logic-block-picker.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class LogicBlockPickerComponent implements OnChanges, AfterViewInit, OnDestroy {

  @Input() templates: RuleTemplate[] = [];
  /** the menu item the panel opens under; a click on it is not an outside click (the menu toggles the panel itself) */
  @Input() anchorElement: HTMLElement;

  @Output() pick = new EventEmitter<RuleTemplate>();
  @Output() close = new EventEmitter<void>();

  @ViewChild("search", { static: true }) searchRef: ElementRef<HTMLInputElement>;
  @ViewChild("list", { static: true }) listRef: ElementRef<HTMLElement>;

  query = "";
  active = 0;
  /** the grouped view of an empty search */
  groups: BlockGroup[] = [];
  /** what is shown, in keyboard order */
  results: BlockEntry[] = [];

  top = 0;
  left = 0;
  width = WIDTH;
  maxHeight = 480;

  private entries: BlockEntry[] = [];

  constructor(private translate: L10nTranslationService,
    private host: ElementRef<HTMLElement>,
    private ngZone: NgZone,
    private changeRef: ChangeDetectorRef) {
  }

  ngOnChanges() {
    this.entries = (this.templates ?? []).map(t => this.entry(t));
    this.filter();
    this.place();
  }

  ngAfterViewInit() {
    this.searchRef.nativeElement.focus();
    this.ngZone.runOutsideAngular(() => document.addEventListener("pointerdown", this.onDocumentPointerDown, true));
  }

  ngOnDestroy() {
    document.removeEventListener("pointerdown", this.onDocumentPointerDown, true);
  }

  private entry(template: RuleTemplate): BlockEntry {
    const name = this.text(template.Name) || template.Name || "";
    const group = this.text(template.Group);
    const description = this.text(template.Description);
    return {
      template,
      name,
      group,
      description,
      key: normalizeSearch(name),
      // the untranslated names too: "and" finds the AND block in every language
      text: normalizeSearch([name, group, description, template.Name, template.Group].join(" "))
    };
  }

  /** a translation, or nothing when there is none (the service answers with the key itself) */
  private text(key: string) {
    if (!key) {
      return "";
    }
    const value = this.translate.translate(key);
    return !value || value === key || /^[A-Z0-9_.-]+$/.test(value) ? "" : value;
  }

  // ---------- search ----------

  onInput(value: string) {
    this.query = value;
    this.filter();
  }

  private filter() {
    const terms = normalizeSearch(this.query).split(/\s+/).filter(a => a);
    const byName = (a: BlockEntry, b: BlockEntry) => a.name.localeCompare(b.name);

    if (terms.length === 0) {
      const map = new Map<string, BlockEntry[]>();
      for (const entry of this.entries) {
        if (!map.has(entry.group)) {
          map.set(entry.group, []);
        }
        map.get(entry.group).push(entry);
      }
      this.groups = [...map.entries()]
        .map(([name, items]) => ({ name, items: items.sort(byName) }))
        .sort((a, b) => a.name.localeCompare(b.name));
      this.results = this.groups.flatMap(g => g.items);
    } else {
      const rank = (e: BlockEntry) => e.key.startsWith(terms[0]) ? 0 : terms.every(t => e.key.includes(t)) ? 1 : 2;
      this.groups = [];
      this.results = this.entries
        .filter(e => terms.every(t => e.text.includes(t)))
        .map(e => ({ e, r: rank(e) }))
        .sort((a, b) => a.r - b.r || byName(a.e, b.e))
        .map(a => a.e);
    }
    this.active = 0;
    if (this.listRef) {
      this.listRef.nativeElement.scrollTop = 0;
    }
  }

  // ---------- keyboard / mouse ----------

  onKeyDown(event: KeyboardEvent) {
    switch (event.key) {
      case "ArrowDown":
        event.preventDefault();
        this.setActive(Math.min(this.results.length - 1, this.active + 1));
        break;
      case "ArrowUp":
        event.preventDefault();
        this.setActive(Math.max(0, this.active - 1));
        break;
      case "Enter":
        event.preventDefault();
        if (this.results[this.active]) {
          this.choose(this.results[this.active]);
        }
        break;
      case "Escape":
        event.preventDefault();
        event.stopPropagation();
        this.close.emit();
        break;
    }
  }

  setActive(index: number) {
    this.active = index;
    // keep the active row in view after the view has moved it
    requestAnimationFrame(() => this.listRef.nativeElement.querySelector(`[data-index="${index}"]`)?.scrollIntoView({ block: "nearest" }));
  }

  indexOf(entry: BlockEntry) {
    return this.results.indexOf(entry);
  }

  choose(entry: BlockEntry) {
    this.pick.emit(entry.template);
  }

  private onDocumentPointerDown = (event: PointerEvent) => {
    const target = event.target as Node;
    if (this.host.nativeElement.contains(target) || this.anchorElement?.contains(target)) {
      return;
    }
    this.ngZone.run(() => {
      this.close.emit();
      this.changeRef.markForCheck();
    });
  }

  // ---------- placement ----------

  /** fixed under the menu item, inside the window: the menu bar clips everything that overflows it */
  @HostListener("window:resize")
  place() {
    const rect = this.anchorElement?.getBoundingClientRect();
    const viewW = window.innerWidth;
    const viewH = window.innerHeight;
    this.width = Math.min(WIDTH, viewW - MARGIN * 2);
    this.top = (rect?.bottom ?? 0) + 4;
    this.left = Math.max(MARGIN, Math.min(rect?.left ?? MARGIN, viewW - this.width - MARGIN));
    this.maxHeight = Math.max(200, Math.min(520, viewH - this.top - MARGIN * 2));
  }
}
