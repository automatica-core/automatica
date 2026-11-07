import { Component, OnDestroy, Input, Output, EventEmitter, ChangeDetectionStrategy } from "@angular/core";
import { AggregatedValueRecord } from "src/app/base/model/aggregated-record-value";

/** press longer than this opens the details popup instead of toggling */
const LONG_PRESS_MS = 500;
/** finger moved further than this (px): the user scrolls, no long press */
const LONG_PRESS_SLOP = 10;

interface SparkPath {
  line: string;
  area: string;
  x: number;
  y: number;
}

/**
 * The shared tile frame of every visualization control.
 *
 * A tile shows its state with the whole surface: `active` turns it amber (a light that is on),
 * `action` makes it an accent-colored action (push button, scene). With `pressable` the tile itself is a
 * `<button>` that emits `press`; long press, right click or the small details button open the popup.
 */
@Component({
    selector: "app-visualization-control",
    templateUrl: "./base-control.component.html",
    styleUrls: ["./base-control.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class BaseControlComponent implements OnDestroy {

  /** 1 = one grid column, 2 = spans two (media player) */
  @Input()
  width: number = 1;

  @Input()
  icon: any = "question";

  /** area / room, shown as a small muted label above the name */
  @Input()
  location: string;

  /** the tile's name (title) */
  @Input()
  hint: string;

  /** a measured value, shown large in the data font */
  @Input()
  value: any;

  /** unit next to the large value */
  @Input()
  unit: string;

  /** short state text in the top right corner ("Ein", "60 %") */
  @Input()
  state: string;

  @Input()
  subValue: string;

  /** 0..100: draws the level bar (dimmer brightness, shutter position) */
  @Input()
  level: number;

  /** color of the level bar: lamp (brightness) or accent (position) */
  @Input()
  levelTone: "lamp" | "accent" = "lamp";

  /** the tile is switched on (lamp colors) */
  @Input()
  active: boolean = false;

  /** an action tile (push button, scene): accent colors */
  @Input()
  action: boolean = false;

  /** the whole tile is a button that emits `press` */
  @Input()
  pressable: boolean = false;

  /** aria-pressed for toggles; leave undefined for plain actions */
  @Input()
  pressed: boolean;

  @Input()
  disabled: boolean = false;

  @Input()
  hasPopup: boolean = false;

  @Input()
  showIcon: boolean = true;

  /** the control renders everything itself inside the tile surface (media player) */
  @Input()
  useFullContainer: boolean = false;

  @Input()
  aggregatedValues: AggregatedValueRecord[] = undefined;

  @Input()
  showIconInPopup: boolean = true;

  @Input()
  showLocationInPopup: boolean = true;

  /** large popup (charts) instead of the compact one (switch, slider) */
  @Input()
  popupLarge: boolean = false;

  @Output()
  press: EventEmitter<void> = new EventEmitter<void>();

  @Output()
  onPopupShowing: EventEmitter<any> = new EventEmitter<any>();

  @Output()
  onPopupHiding: EventEmitter<any> = new EventEmitter<any>();

  popupVisible = false;

  /** briefly set after a press, gives action tiles a visible confirmation */
  firing = false;

  private longPressTimer: any;
  private longPressFired = false;
  private pressStart: { x: number, y: number };
  private firingTimer: any;

  private cachedSparkSource: AggregatedValueRecord[];
  private cachedSpark: SparkPath;

  public get valueHidden(): boolean {
    return this.value === void 0 || this.value === null || this.value === "";
  }

  /** the room is not repeated when the tile is named like the room ("Living Room / Living Room") */
  public get showLocation(): boolean {
    if (!this.location) {
      return false;
    }
    const norm = (s: any) => (s ?? "").toString().trim().toLocaleLowerCase();
    return norm(this.location) !== norm(this.hint);
  }

  public get displayValue(): string {
    const v = this.value;
    if (typeof v === "number") {
      return new Intl.NumberFormat(void 0, { maximumFractionDigits: 2 }).format(v);
    }
    if (typeof v === "boolean") {
      return v ? "1" : "0";
    }
    return v;
  }

  /** long texts (dates, strings) get a smaller size so they fit the tile */
  public get valueSize(): string {
    const length = (this.displayValue ?? "").toString().length + (this.unit ? this.unit.length : 0);
    if (length > 12) {
      return "is-longer";
    }
    return length > 7 ? "is-long" : "";
  }

  public get hasLevel(): boolean {
    return typeof this.level === "number" && !isNaN(this.level);
  }

  public get levelWidth(): number {
    if (!this.hasLevel) {
      return 0;
    }
    return Math.max(0, Math.min(100, this.level));
  }

  /** sparkline of the trend values as svg paths in a 120 x 26 box */
  public get sparkPath(): SparkPath {
    if (this.cachedSparkSource === this.aggregatedValues) {
      return this.cachedSpark;
    }
    this.cachedSparkSource = this.aggregatedValues;
    this.cachedSpark = void 0;

    const values = (this.aggregatedValues ?? [])
      .map((a: any) => Number(a.averageValue ?? a.AvergeValue))
      .filter(a => !isNaN(a));

    if (values.length < 3) {
      return void 0;
    }

    const w = 120, h = 26, pad = 3;
    const min = Math.min(...values);
    const max = Math.max(...values);
    const range = max - min || 1;
    const points = values.map((v, i) => [
      (i / (values.length - 1)) * w,
      h - pad - ((v - min) / range) * (h - pad * 2)
    ]);
    const line = points.map((p, i) => `${i === 0 ? "M" : "L"}${p[0].toFixed(1)} ${p[1].toFixed(1)}`).join(" ");
    const last = points[points.length - 1];

    this.cachedSpark = { line, area: `${line} L${w} ${h} L0 ${h} Z`, x: last[0], y: last[1] };
    return this.cachedSpark;
  }

  public get ariaLabel(): string {
    const value = this.valueHidden ? void 0 : `${this.displayValue}${this.unit ? " " + this.unit : ""}`;
    return [this.showLocation ? this.location : void 0, this.hint, this.state, value]
      .filter(a => a !== void 0 && a !== null && a !== "")
      .join(", ");
  }

  ngOnDestroy() {
    this.cancelLongPress();
    clearTimeout(this.firingTimer);
  }

  preventDefault($event) {
    $event.stopPropagation();
  }

  onTileClick($event: Event) {
    if (this.longPressFired) {
      // the long press already opened the popup
      this.longPressFired = false;
      $event.preventDefault();
      return;
    }

    if (this.pressable) {
      if (this.disabled) {
        return;
      }
      this.press.emit();
      this.flash();
    } else if (this.hasPopup) {
      this.openPopup();
    }
  }

  onPointerDown($event: PointerEvent) {
    this.longPressFired = false;
    if (!this.hasPopup || !this.pressable || ($event.pointerType === "mouse" && $event.button !== 0)) {
      return;
    }
    this.pressStart = { x: $event.clientX, y: $event.clientY };
    this.cancelLongPress();
    this.longPressTimer = setTimeout(() => {
      this.longPressTimer = void 0;
      this.longPressFired = true;
      this.openPopup();
    }, LONG_PRESS_MS);
  }

  onPointerMove($event: PointerEvent) {
    if (this.longPressTimer && this.pressStart) {
      if (Math.abs($event.clientX - this.pressStart.x) > LONG_PRESS_SLOP || Math.abs($event.clientY - this.pressStart.y) > LONG_PRESS_SLOP) {
        this.cancelLongPress();
      }
    }
  }

  cancelLongPress() {
    if (this.longPressTimer) {
      clearTimeout(this.longPressTimer);
      this.longPressTimer = void 0;
    }
  }

  onContextMenu($event: Event) {
    if (!this.hasPopup) {
      return;
    }
    // right click, or the browser's own long press menu: open the details instead
    $event.preventDefault();
    this.cancelLongPress();
    if (!this.popupVisible) {
      this.longPressFired = this.pressable;
      this.openPopup();
    }
  }

  openPopup($event?: Event) {
    $event?.stopPropagation();
    this.popupVisible = true;
  }

  private flash() {
    if (!this.action) {
      return;
    }
    this.firing = true;
    clearTimeout(this.firingTimer);
    this.firingTimer = setTimeout(() => this.firing = false, 600);
  }

  async onPopupHidingCall($event) {
    await this.onPopupHiding.emit($event);
    this.popupVisible = false;
  }
  async onPopupShowingCall($event) {
    await this.onPopupShowing.emit($event);
  }

}
