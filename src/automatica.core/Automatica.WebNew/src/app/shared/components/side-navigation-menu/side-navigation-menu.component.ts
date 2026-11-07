import { Component, NgModule, Output, Input, EventEmitter, ChangeDetectionStrategy, ChangeDetectorRef, OnDestroy, OnInit } from "@angular/core";
import { L10nTranslationModule } from "angular-l10n";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";
import { CommonModule } from "@angular/common";
import { NavigationEnd, Router } from "@angular/router";
import { DxLoadIndicatorModule } from "devextreme-angular";
import { Subscription } from "rxjs";
import { NavItem } from "src/app/app-navigation";

@Component({
    selector: "app-side-navigation-menu",
    templateUrl: "./side-navigation-menu.component.html",
    styleUrls: ["./side-navigation-menu.component.scss"],
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class SideNavigationMenuComponent implements OnInit, OnDestroy {

    private _items: NavItem[];

    @Input()
    get items(): NavItem[] {
        return this._items;
    }
    set items(v: NavItem[]) {
        this._items = v;
        this.expandActive();
    }

    @Input()
    compactMode = false;

    /** an item was chosen, the layout closes the mobile menu */
    @Output()
    navigated = new EventEmitter<NavItem>();

    /** a parent was clicked while compact, the layout opens the menu to show its children */
    @Output()
    expandRequested = new EventEmitter<void>();

    private url = "";
    private routerSub: Subscription;

    constructor(private router: Router, private changeRef: ChangeDetectorRef) { }

    ngOnInit() {
        this.url = this.router.url.split("?")[0];
        this.routerSub = this.router.events.subscribe(e => {
            if (e instanceof NavigationEnd) {
                this.url = e.urlAfterRedirects.split("?")[0];
                this.expandActive();
                this.changeRef.markForCheck();
            }
        });
        this.expandActive();
    }

    ngOnDestroy() {
        this.routerSub?.unsubscribe();
    }

    isActive(item: NavItem) {
        if (!item.path) {
            return false;
        }
        // the visualization entry in the admin menu leaves the admin, it is never the current page
        if (item.path === "/visualization") {
            return false;
        }
        // /admin/logic-editor/<page> keeps "Logik" active
        return this.url === item.path || this.url.startsWith(item.path + "/");
    }

    onItemClick(event: MouseEvent, item: NavItem) {
        event.preventDefault();

        if (item.items?.length && this.compactMode) {
            item.expanded = true;
            this.expandRequested.emit();
            return;
        }
        if (item.path) {
            this.router.navigateByUrl(item.path);
            this.navigated.emit(item);
        } else if (item.items?.length) {
            item.expanded = !item.expanded;
        }
    }

    toggle(event: MouseEvent, item: NavItem) {
        event.preventDefault();
        event.stopPropagation();
        item.expanded = !item.expanded;
    }

    /** opens the parents of the current page */
    private expandActive() {
        const visit = (items: NavItem[]): boolean => {
            let found = false;
            for (const item of items ?? []) {
                const childActive = visit(item.items);
                if (childActive && !item.group) {
                    item.expanded = true;
                }
                found = found || childActive || this.isActive(item);
            }
            return found;
        };
        visit(this._items);
    }
}

@NgModule({
    imports: [CommonModule, L10nTranslationModule, FontAwesomeModule, DxLoadIndicatorModule],
    declarations: [SideNavigationMenuComponent],
    exports: [SideNavigationMenuComponent]
})
export class SideNavigationMenuModule { }
