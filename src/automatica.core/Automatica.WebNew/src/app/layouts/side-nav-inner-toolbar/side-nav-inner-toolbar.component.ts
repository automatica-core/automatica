import { Component, OnInit, NgModule, Input, OnDestroy, Output, EventEmitter, ChangeDetectionStrategy, ChangeDetectorRef } from "@angular/core";
import { BreakpointObserver, Breakpoints } from "@angular/cdk/layout";
import { DxDrawerModule } from "devextreme-angular/ui/drawer";
import { DxScrollViewModule } from "devextreme-angular/ui/scroll-view";
import { DxToolbarModule } from "devextreme-angular/ui/toolbar";
import { CommonModule } from "@angular/common";
import { Router, NavigationEnd, ActivatedRoute } from "@angular/router";
import { SideNavigationMenuModule } from "src/app/shared/components/side-navigation-menu/side-navigation-menu.component";
import { HeaderModule } from "src/app/shared/components/header/header.component";
import { FooterModule } from "src/app/shared/components/footer/footer.component";
import { Subscription } from "rxjs";
import { DeviceService } from "src/app/services/device/device.service";
import { DxTooltipModule } from "devextreme-angular";
import { ThemeService } from "src/app/services/theme.service";

@Component({
    selector: "app-side-nav-inner-toolbar",
    templateUrl: "./side-nav-inner-toolbar.component.html",
    styleUrls: ["./side-nav-inner-toolbar.component.scss"],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class SideNavInnerToolbarComponent implements OnInit, OnDestroy {
    selectedRoute = "";

    @Input()
    menuItems: any[];

    private _menuOpened: boolean = false;

    @Input()
    public get menuOpened(): boolean {
        return this._menuOpened;
    }
    public set menuOpened(v: boolean) {
        this._menuOpened = v;
        this.menuOpenedChanged.emit(v);
    }

    @Input()
    backgroundColor = "white";
    @Output()
    backgroundColorChanged = new EventEmitter<any>();


    @Output()
    menuOpenedChanged = new EventEmitter<any>();

    @Input()
    title: string;

    menuMode = "overlap";
    menuRevealMode = "expand";
    minMenuSize = 0;
    shaderEnabled = true;
    routerSub: Subscription;

    themeChangedSub: Subscription;

    public get isMobile() {
        return this.deviceService.isMobile();
    }

    constructor(private breakpointObserver: BreakpointObserver,
        private router: Router,
        private activatedRoute: ActivatedRoute,
        private deviceService: DeviceService,
        private changeRef: ChangeDetectorRef,
        private themeService: ThemeService) { }

    ngOnInit() {
        this.backgroundColor = this.themeService.getBackgroundColor();
        this.themeChangedSub = this.themeService.themeChanged.subscribe(a => {
            this.backgroundColor = this.themeService.getBackgroundColor();
            this.backgroundColorChanged.emit(this.backgroundColor);

            this.changeRef.detectChanges();
        });

        this.selectedRoute = "";
        this.routerSub = this.router.events.subscribe(val => {
            if (val instanceof NavigationEnd) {
                this.selectedRoute = val.urlAfterRedirects.split("?")[0];
            }
        });

        this.breakpointObserver
            .observe([Breakpoints.XSmall, Breakpoints.Small, Breakpoints.Medium, Breakpoints.Large])
            .subscribe(() => this.updateDrawer());

        this.updateDrawer();
    }

    ngOnDestroy() {
        this.routerSub.unsubscribe();
        this.themeChangedSub.unsubscribe();
    }


    updateDrawer() {
        const isXSmall = this.breakpointObserver.isMatched(Breakpoints.XSmall);

        this.menuMode = this.isLargeScreen ? "shrink" : "overlap";
        this.menuRevealMode = isXSmall ? "slide" : "expand";
        this.minMenuSize = isXSmall ? 0 : 60;
        this.shaderEnabled = !this.isLargeScreen;

        this.changeRef.detectChanges();
    }

    toggleMenu = (e) => {
        this.menuOpened = !this.menuOpened;
        e.event.stopPropagation();

        this.changeRef.detectChanges();
    }

    get isLargeScreen() {
        const isLarge = this.breakpointObserver.isMatched(Breakpoints.Large);
        const isXLarge = this.breakpointObserver.isMatched(Breakpoints.XLarge);

        return isLarge || isXLarge;
    }

    get sizeClasses() {
        return {
            "screen-x-small": this.breakpointObserver.isMatched(Breakpoints.XSmall),
            "screen-small": this.breakpointObserver.isMatched(Breakpoints.Small),
            "screen-medium": this.breakpointObserver.isMatched(Breakpoints.Medium),
            "screen-large": this.isLargeScreen,
        };
    }

    get hideMenuAfterNavigation() {
        return this.menuMode === "overlap";
    }

    get showMenuAfterClick() {
        if (this.isLargeScreen) {
            return false;
        }
        return !this.menuOpened;
    }

    navigationChanged(event) {
        const path = event.itemData.path;
        const pointerEvent = event.event;

        if (path) {
            if (event.node.selected) {
                pointerEvent.preventDefault();
            } else {
                this.router.navigate([path], { relativeTo: this.activatedRoute });
            }

            if (this.hideMenuAfterNavigation) {
                if (!this.isLargeScreen)
                    this.menuOpened = false;
                pointerEvent.stopPropagation();
            }
        } else {
            this.menuOpened = true;
            pointerEvent.preventDefault();
        }

        setTimeout(() => {
            if (this.showMenuAfterClick) {
                this.menuOpened = true;
            }
        });
    }

    navigationClick($event) {
        // console.log($event);
        // if (this.showMenuAfterClick) {

        //     this.menuOpened = true;
        // }
    }
}

@NgModule({
    imports: [SideNavigationMenuModule, DxDrawerModule, HeaderModule, DxToolbarModule, DxScrollViewModule, CommonModule, FooterModule, DxTooltipModule],
    exports: [SideNavInnerToolbarComponent],
    declarations: [SideNavInnerToolbarComponent]
})
export class SideNavInnerToolbarModule { }
