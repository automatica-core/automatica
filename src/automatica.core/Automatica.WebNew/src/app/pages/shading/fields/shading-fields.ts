import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from "@angular/core";
import { SOURCE_TYPES_BY_KIND, Signal, SignalKind, SignalSource, SignalSourceType, newSource } from "src/app/base/model/shading/shading";
import { ShadingContext, shadingOptions } from "../shading-context";

/** shows the chosen datapoint and opens the datapoint picker */
@Component({
    selector: "shading-node-field",
    template: `
    <div class="node-row">
      <button type="button" class="pick" [class.missing]="!!node && !name" (click)="pick()"
        [attr.aria-label]="'UI.SHADING.FIELD.DATAPOINT' | translateAsync">
        <fa-icon [icon]="['fas', 'circle-nodes']"></fa-icon>
        <span class="txt">
          @if (name) {
            <span class="nm">{{ name }}</span>
            @if (path) { <span class="path">{{ path }}</span> }
          } @else if (node) {
            <span class="nm">{{ 'UI.SHADING.FIELD.UNKNOWN_NODE' | translateAsync }}</span>
            <span class="path mono">{{ node }}</span>
          } @else {
            <span class="nm placeholder">{{ 'UI.SHADING.FIELD.PICK_DATAPOINT' | translateAsync }}</span>
          }
        </span>
        <fa-icon [icon]="['fas', 'ellipsis']"></fa-icon>
      </button>
      @if (clearable && node) {
        <dx-button icon="clear" stylingMode="text" [hint]="'UI.SHADING.FIELD.CLEAR' | translateAsync" (onClick)="clear()"></dx-button>
      }
    </div>`,
    styleUrls: ["./shading-fields.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class ShadingNodeFieldComponent {
    @Input() node: string | null | undefined;
    @Input() clearable = false;
    @Output() nodeChange = new EventEmitter<string | null>();

    constructor(private ctx: ShadingContext) {
    }

    get name(): string | undefined {
        return this.node ? this.ctx.lookups.nodeName(this.node) : void 0;
    }

    get path(): string {
        return this.node ? this.ctx.lookups.nodePath(this.node) : "";
    }

    async pick() {
        const id = await this.ctx.pickNode(this.node);
        if (id && id !== this.node) {
            this.node = id;
            this.nodeChange.emit(id);
        }
    }

    clear() {
        this.node = null;
        this.nodeChange.emit(null);
    }
}

/**
 * The sources of a signal in priority order (the first one with a fresh value wins): datapoints, the calculation,
 * a pyranometer or a PV system, depending on what the signal is for.
 */
@Component({
    selector: "shading-signal-editor",
    templateUrl: "./shading-signal-editor.component.html",
    styleUrls: ["./shading-fields.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class ShadingSignalEditorComponent {
    @Input() signal: Signal;
    @Input() kind: SignalKind = "measured";
    /** unit of a datapoint value after the factor, e.g. "lx", "m/s", "°C" */
    @Input() unit = "";
    /** text when there is no source */
    @Input() emptyText = "";
    /** nested editor (cloud cover): no frame */
    @Input() nested = false;

    constructor(public ctx: ShadingContext) {
    }

    get sources(): SignalSource[] {
        if (!this.signal) {
            return [];
        }
        this.signal.sources = this.signal.sources || [];
        return this.signal.sources;
    }

    /** the calculation only once, a second one would never be used */
    get addItems() {
        const hasCalculated = this.sources.some(a => a.type === "calculated");
        return shadingOptions("SOURCE", SOURCE_TYPES_BY_KIND[this.kind].filter(a => !(a === "calculated" && hasCalculated)));
    }

    add(type: SignalSourceType) {
        if (!this.signal) {
            return;
        }
        this.sources.push(newSource(type));
    }

    remove(index: number) {
        this.sources.splice(index, 1);
    }

    move(index: number, delta: number) {
        const to = index + delta;
        if (to < 0 || to >= this.sources.length) {
            return;
        }
        const [item] = this.sources.splice(index, 1);
        this.sources.splice(to, 0, item);
    }

    /** a calculated brightness has an optional cloud cover */
    cloudCover(source: any): Signal {
        if (!source.cloudCover) {
            source.cloudCover = { sources: [] };
        }
        return source.cloudCover;
    }

    trackIndex(index: number) {
        return index;
    }
}

const R = 100;

/**
 * Compass with the sun path of a day, the sun and the facades. Azimuth clockwise from north (up), elevation as the
 * distance from the rim (horizon) to the centre (zenith).
 */
@Component({
    selector: "shading-compass",
    template: `
    <svg viewBox="-130 -130 260 260" role="img" [attr.aria-label]="label">
      <circle class="rim" r="100"></circle>
      <circle class="ring" r="66.7"></circle>
      <circle class="ring" r="33.3"></circle>
      @for (t of ticks; track t.a) {
        <line class="tick" [class.major]="t.major" [attr.x1]="t.x1" [attr.y1]="t.y1" [attr.x2]="t.x2" [attr.y2]="t.y2"></line>
      }
      @for (l of letters; track l.key) {
        <text class="dir" [attr.x]="l.x" [attr.y]="l.y">{{ l.key | translateAsync }}</text>
      }
      @for (f of facadeShapes; track f.id) {
        <g class="facade" [class.sunny]="f.sunny" [class.lit]="f.lit" [class.off]="!f.enabled" [class.selected]="f.id === selected">
          <path class="wedge" [attr.d]="f.wedge"></path>
          <line class="wall" [attr.x1]="f.wx1" [attr.y1]="f.wy1" [attr.x2]="f.wx2" [attr.y2]="f.wy2"></line>
          <line class="normal" [attr.x1]="f.nx1" [attr.y1]="f.ny1" [attr.x2]="f.nx2" [attr.y2]="f.ny2"></line>
          <text class="fname" [attr.x]="f.tx" [attr.y]="f.ty">{{ f.name }}</text>
        </g>
      }
      @if (pathD) {
        <path class="sunpath" [attr.d]="pathD"></path>
      }
      @if (sunXY) {
        <circle class="sun" [attr.cx]="sunXY.x" [attr.cy]="sunXY.y" r="7"></circle>
      }
    </svg>`,
    styleUrls: ["./shading-compass.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class ShadingCompassComponent {
    @Input() sun: { azimuth: number; elevation: number } | null | undefined;
    @Input() path: { azimuth: number; elevation: number }[] = [];
    @Input() facades: { id: string; name: string; azimuth: number; enabled: boolean; sunny: boolean; lit: boolean; acceptance: number }[] = [];
    @Input() selected: string | null = null;
    @Input() label = "";

    readonly ticks = Array.from({ length: 16 }, (_, i) => {
        const a = i * 22.5;
        const major = i % 4 === 0;
        const p1 = ShadingCompassComponent.point(a, R);
        const p2 = ShadingCompassComponent.point(a, major ? R - 8 : R - 4);
        return { a, major, x1: p1.x, y1: p1.y, x2: p2.x, y2: p2.y };
    });

    readonly letters = ["N", "E", "S", "W"].map((point, i) => {
        const p = ShadingCompassComponent.point(i * 90, R + 18);
        return { key: "UI.SHADING.DIR." + point, x: p.x, y: p.y + 4 };
    });

    static point(azimuth: number, radius: number): { x: number; y: number } {
        const r = azimuth * Math.PI / 180;
        return { x: +(Math.sin(r) * radius).toFixed(2), y: +(-Math.cos(r) * radius).toFixed(2) };
    }

    static sunPoint(azimuth: number, elevation: number): { x: number; y: number } {
        return ShadingCompassComponent.point(azimuth, R * (1 - Math.max(0, Math.min(90, elevation)) / 90));
    }

    get sunXY(): { x: number; y: number } | undefined {
        return this.sun && this.sun.elevation > -1 ? ShadingCompassComponent.sunPoint(this.sun.azimuth, this.sun.elevation) : void 0;
    }

    private pathCache: { key: string; d: string } = { key: "", d: "" };

    get pathD(): string {
        const list = this.path || [];
        const key = list.length + ":" + (list[0]?.azimuth ?? "") + ":" + (list[list.length - 1]?.azimuth ?? "");
        if (key !== this.pathCache.key) {
            const d = list.map((p, i) => {
                const q = ShadingCompassComponent.sunPoint(p.azimuth, p.elevation);
                return (i === 0 ? "M" : "L") + q.x + " " + q.y;
            }).join(" ");
            this.pathCache = { key, d };
        }
        return this.pathCache.d;
    }

    private facadeCache: { key: string; shapes: any[] } = { key: "", shapes: [] };

    /** each facade as a wall across its direction, a short normal and the wedge of its acceptance angle */
    get facadeShapes(): any[] {
        const list = this.facades || [];
        const key = JSON.stringify(list);
        if (key === this.facadeCache.key) {
            return this.facadeCache.shapes;
        }

        const shapes = list.map(f => {
            const wall = 44;
            const center = ShadingCompassComponent.point(f.azimuth, 52);
            const left = ShadingCompassComponent.point(f.azimuth - 90, wall / 2);
            const n2 = ShadingCompassComponent.point(f.azimuth, 70);
            const label = ShadingCompassComponent.point(f.azimuth, 84);
            const acc = Math.max(1, Math.min(90, f.acceptance || 80));
            const a1 = ShadingCompassComponent.point(f.azimuth - acc, R);
            const a2 = ShadingCompassComponent.point(f.azimuth + acc, R);
            const large = acc * 2 > 180 ? 1 : 0;
            return {
                id: f.id,
                name: f.name,
                enabled: f.enabled,
                sunny: f.sunny,
                lit: f.lit,
                wx1: +(center.x + left.x).toFixed(2), wy1: +(center.y + left.y).toFixed(2),
                wx2: +(center.x - left.x).toFixed(2), wy2: +(center.y - left.y).toFixed(2),
                nx1: center.x, ny1: center.y, nx2: n2.x, ny2: n2.y,
                tx: label.x, ty: label.y + 4,
                wedge: `M0 0 L${a1.x} ${a1.y} A${R} ${R} 0 ${large} 1 ${a2.x} ${a2.y} Z`
            };
        });
        this.facadeCache = { key, shapes };
        return shapes;
    }
}
