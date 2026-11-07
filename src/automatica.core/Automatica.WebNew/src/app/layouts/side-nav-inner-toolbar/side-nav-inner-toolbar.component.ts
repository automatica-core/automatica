import { Component, OnInit, NgModule, Input, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from "@angular/core";
import { BreakpointObserver } from "@angular/cdk/layout";
import { CommonModule } from "@angular/common";
import { Subscription } from "rxjs";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";
import { L10nTranslationModule } from "angular-l10n";
import { SideNavigationMenuModule } from "src/app/shared/components/side-navigation-menu/side-navigation-menu.component";
import { HeaderModule } from "src/app/shared/components/header/header.component";
import { NavItem } from "src/app/app-navigation";

const MOBILE_QUERY = "(max-width: 767.98px)";
const WIDE_QUERY = "(min-width: 1280px)";
const COLLAPSED_KEY = "automaticaNavCollapsed";

@Component({
    selector: "app-side-nav-inner-toolbar",
    templateUrl: "./side-nav-inner-toolbar.component.html",
    styleUrls: ["./side-nav-inner-toolbar.component.scss"],
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class SideNavInnerToolbarComponent implements OnInit, OnDestroy {

    @Input()
    menuItems: NavItem[];

    isMobileLayout = false;
    mobileOpen = false;
    collapsed = false;

    /** set after the first frame, so the phone menu does not slide out on load */
    animated = false;

    private breakpointSub: Subscription;

    constructor(private breakpointObserver: BreakpointObserver, private changeRef: ChangeDetectorRef) { }

    ngOnInit() {
        this.collapsed = this.readCollapsed();

        this.breakpointSub = this.breakpointObserver.observe([MOBILE_QUERY]).subscribe(state => {
            this.isMobileLayout = state.matches;
            if (!this.isMobileLayout) {
                this.mobileOpen = false;
            }
            this.changeRef.markForCheck();
        });
    }

    ngAfterViewInit() {
        setTimeout(() => {
            this.animated = true;
            this.changeRef.markForCheck();
        }, 300);
    }

    ngOnDestroy() {
        this.breakpointSub?.unsubscribe();
    }

    setCollapsed(value: boolean) {
        this.collapsed = value;
        try {
            window.localStorage[COLLAPSED_KEY] = value ? "1" : "0";
        } catch {
            // private mode, the choice is just not remembered
        }
        this.changeRef.markForCheck();
    }

    onNavigated() {
        if (this.isMobileLayout) {
            this.mobileOpen = false;
            this.changeRef.markForCheck();
        }
    }

    private readCollapsed() {
        try {
            const stored = window.localStorage[COLLAPSED_KEY];
            if (stored === "1" || stored === "0") {
                return stored === "1";
            }
        } catch {
        }
        // icons only on small laptops and tablets until the user decides otherwise
        return !window.matchMedia(WIDE_QUERY).matches;
    }
}

@NgModule({
    imports: [SideNavigationMenuModule, HeaderModule, CommonModule, FontAwesomeModule, L10nTranslationModule],
    exports: [SideNavInnerToolbarComponent],
    declarations: [SideNavInnerToolbarComponent]
})
export class SideNavInnerToolbarModule { }
