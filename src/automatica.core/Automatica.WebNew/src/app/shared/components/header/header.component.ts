import { Component, NgModule, Input, Output, EventEmitter, OnInit, OnDestroy, ChangeDetectorRef } from "@angular/core";
import { CommonModule } from "@angular/common";

import { DxButtonModule } from "devextreme-angular/ui/button";
import { DxToolbarModule } from "devextreme-angular/ui/toolbar";
import { DxPopupModule } from "devextreme-angular/ui/popup";
import { Router, ActivatedRoute } from "@angular/router";
import { AppService } from "src/app/services/app.service";
import { L10nTranslationModule } from "angular-l10n";
import { LoginService } from "src/app/services/login.service";
import { HubConnectionService } from "src/app/base/communication/hubs/hub-connection.service";
import { DxSpeedDialActionModule } from "devextreme-angular";

import config from "devextreme/core/config";
import { SettingsService } from "src/app/services/settings.service";
import { ThemeService } from "src/app/services/theme.service";

config({
    floatingActionButtonConfig: {
        icon: "more"
    }
});

@Component({
    selector: "app-header",
    templateUrl: "header.component.html",
    styleUrls: ["./header.component.scss"]
})

export class HeaderComponent implements OnInit, OnDestroy {

    get title() {
        return this.projectName;
    }

    @Output()
    menuToggle = new EventEmitter<boolean>();

    @Input()
    menuToggleEnabled = false;

    time: string = "00:00:00";
    ticker: NodeJS.Timeout;

    private projectName: string;

    public themes = [
        {
            name: "Dark",
            theme: "dark"
        }, {
            name: "Light",
            theme: "light"
        }

    ]

    public currentTheme = "";

    selectBoxOptions: any;

    constructor(private router: Router,
        private activatedRoute: ActivatedRoute,
        public appService: AppService,
        private loginService: LoginService,
        private hubService: HubConnectionService,
        private changeRef: ChangeDetectorRef,
        private settingsService: SettingsService,
        private themeService: ThemeService) { }

    async ngOnInit() {

        const projectName = await this.settingsService.getByKey("projectName");
        this.projectName = projectName.Value;
        document.title = this.projectName;

        this.ticker = setInterval(() => {
            const date = new Date();
            this.time = date.toLocaleTimeString();

            this.changeRef.detectChanges();
        }, 1000);

        this.currentTheme = this.themeService.getCurrentTheme();

        this.themeService.themeChanged.subscribe((a) => {
            this.currentTheme = a;
        });

        this.selectBoxOptions = {
            width: 140,
            items: this.themes,
            valueExpr: 'theme',
            displayExpr: 'name',
            value: this.currentTheme,
            onValueChanged: (args) => {
                this.themeService.applyTheme(args.value);
            }
        };
    }

    ngOnDestroy() {
        clearInterval(this.ticker);
    }

    toggleMenu = () => {
        this.menuToggle.emit();
    }
    isAdminPage() {
        if (this.activatedRoute.snapshot.url.length > 0) {
            return this.activatedRoute.snapshot.url[0].path === "admin";
        }
        return false;
    }
    async logout() {
        await this.hubService.stop();
        await this.loginService.logout();
        this.router.navigate(["/login"]);
    }

    navigateToAdminPage() {
        this.router.navigate(["admin", "home"]);
    }

    navigateToVisualization() {
        this.router.navigate(["visualization"]);
    }
}

@NgModule({
    imports: [
        CommonModule,
        DxPopupModule,
        DxButtonModule,
        DxToolbarModule,
        L10nTranslationModule,
        DxSpeedDialActionModule
    ],
    declarations: [HeaderComponent],
    exports: [HeaderComponent]
})
export class HeaderModule { }
