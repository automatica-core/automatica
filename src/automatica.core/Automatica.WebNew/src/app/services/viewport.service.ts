import { Injectable, NgZone, signal } from "@angular/core";

/** phone width: the split pages stack their panes instead of putting them side by side */
const NARROW_QUERY = "(max-width: 767.98px)";

@Injectable({ providedIn: "root" })
export class ViewportService {
    readonly narrow = signal(false);

    constructor(zone: NgZone) {
        const query = window.matchMedia?.(NARROW_QUERY);
        if (!query) {
            return;
        }
        this.narrow.set(query.matches);
        query.addEventListener("change", event => zone.run(() => this.narrow.set(event.matches)));
    }

    get splitDirection(): "horizontal" | "vertical" {
        return this.narrow() ? "vertical" : "horizontal";
    }
}
