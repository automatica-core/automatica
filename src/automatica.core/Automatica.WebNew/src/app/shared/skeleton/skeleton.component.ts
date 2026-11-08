import { ChangeDetectionStrategy, Component, Input } from "@angular/core";

export type SkeletonVariant = "list" | "table" | "tree" | "form" | "cards" | "lines";

// fixed widths (percent) so the placeholders look like real text but never jump between renders
const WIDTHS = [62, 48, 74, 55, 68, 40, 80, 52, 70, 45, 58, 66];
// indent levels of the tree variant: root, a few children, a nested branch, back up
const TREE_LEVELS = [0, 1, 2, 2, 1, 2, 3, 3, 1, 0, 1, 2];

/**
 * Placeholder in the shape of the content that is loading (shimmering blocks instead of a spinner overlay).
 * Usage: `@if (!loaded) { <app-skeleton variant="list" [rows]="8"></app-skeleton> } @else { ...content... }`
 */
@Component({
    selector: "app-skeleton",
    templateUrl: "./skeleton.component.html",
    styleUrls: ["./skeleton.component.scss"],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        "role": "status",
        "aria-busy": "true",
        "class": "skeleton",
        "[attr.data-variant]": "variant"
    },
    standalone: false
})
export class SkeletonComponent {

    @Input()
    public variant: SkeletonVariant = "list";

    @Input()
    public set rows(v: number) {
        this.rowItems = SkeletonComponent.range(v);
    }

    @Input()
    public set columns(v: number) {
        this.columnItems = SkeletonComponent.range(v);
    }

    public rowItems = SkeletonComponent.range(6);
    public columnItems = SkeletonComponent.range(4);

    public width(i: number, offset = 0): string {
        return WIDTHS[(i + offset) % WIDTHS.length] + "%";
    }

    public indent(i: number): number {
        return TREE_LEVELS[i % TREE_LEVELS.length];
    }

    private static range(count: number): number[] {
        const n = Math.max(0, Math.floor(Number(count) || 0));
        return Array.from({ length: n }, (_, i) => i);
    }
}
