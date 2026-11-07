import { Component, NgModule, Input, Output, EventEmitter, OnInit, OnDestroy, ChangeDetectorRef, ChangeDetectionStrategy, ElementRef, HostListener } from "@angular/core";
import { CommonModule } from "@angular/common";
import { Router, ActivatedRoute } from "@angular/router";
import { AppService } from "src/app/services/app.service";
import { L10nTranslationModule } from "angular-l10n";
import { LoginService } from "src/app/services/login.service";
import { HubConnectionService } from "src/app/base/communication/hubs/hub-connection.service";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";
import { SettingsService, Language as SettingsLanguage } from "src/app/services/settings.service";
import { ThemeService } from "src/app/services/theme.service";
import { TranslationConfigService } from "src/app/services/translation-config.service";
import { App } from "@capacitor/app";
import { DeviceService } from "src/app/services/device/device.service";
import { Subscription } from "rxjs";

export enum Language {
    German = 0,
    English = 1
}

@Component({
    selector: "app-header",
    templateUrl: "header.component.html",
    styleUrls: ["./header.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class HeaderComponent implements OnInit, OnDestroy {

    @Output()
    menuToggle = new EventEmitter<void>();

    @Input()
    menuToggleEnabled = false;

    projectName: string;
    version: string;
    isAdminAvailable = false;
    connected = true;
    menuOpen = false;
    currentTheme = "dark";
    userName = "";
    initials = "";

    private destroyed = false;
    private subs: Subscription[] = [];

    constructor(private router: Router,
        private activatedRoute: ActivatedRoute,
        public appService: AppService,
        private loginService: LoginService,
        private hubService: HubConnectionService,
        private dataHub: DataHubService,
        private changeRef: ChangeDetectorRef,
        private settingsService: SettingsService,
        private themeService: ThemeService,
        private translate: TranslationConfigService,
        private deviceService: DeviceService,
        private element: ElementRef<HTMLElement>) {
    }

    @HostListener("document:click", ["$event"])
    onDocumentClick(event: MouseEvent) {
        if (this.menuOpen && !this.element.nativeElement.querySelector(".user")?.contains(event.target as Node)) {
            this.menuOpen = false;
        }
    }

    @HostListener("document:keydown.escape")
    onEscape() {
        this.menuOpen = false;
    }

    async ngOnInit() {
        this.currentTheme = this.themeService.getCurrentTheme();
        this.subs.push(this.themeService.themeChanged.subscribe(a => {
            this.currentTheme = a;
            this.changeRef.markForCheck();
        }));
        this.subs.push(this.dataHub.connectionStateChanged.subscribe(state => {
            this.connected = !!state;
            this.changeRef.markForCheck();
        }));

        const user = this.loginService.getCurrentUser();
        this.isAdminAvailable = user?.InverseThis2Roles?.some(a => a.This2RoleNavigation?.Key === "administrator") ?? false;
        const first = user?.FirstName?.trim() ?? "";
        const last = user?.LastName?.trim() ?? "";
        this.userName = [first, last].filter(a => a).join(" ") || user?.UserName || "";
        this.initials = ((first[0] ?? "") + (last[0] ?? "")).toUpperCase() || (user?.UserName?.substring(0, 2).toUpperCase() ?? "?");

        await this.hubService.init();
        if (this.destroyed) {
            return;
        }
        this.connected = this.dataHub.connected;

        const projectName = await this.settingsService.getByKey("projectName");
        this.projectName = projectName.Value;
        document.title = this.projectName;

        const languageProperty = await this.settingsService.getByKey("language");
        const language = <Language>languageProperty.Value;
        // the translations are loaded by the app initializer, only the server language has to be applied
        await this.translate.applyLanguage(language == Language.German ? SettingsLanguage.German : SettingsLanguage.English);

        if (!this.deviceService.isWeb) {
            const appInfo = await App.getInfo();
            this.version = appInfo.version + "." + appInfo.build;
        }
        this.changeRef.markForCheck();
    }

    ngOnDestroy() {
        this.destroyed = true;
        this.subs.forEach(a => a.unsubscribe());
    }

    toggleMenu() {
        this.menuToggle.emit();
    }

    setTheme(theme: string) {
        this.themeService.applyTheme(theme);
    }

    isAdminPage() {
        return this.router.url.startsWith("/admin");
    }

    async logout() {
        this.menuOpen = false;
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
        L10nTranslationModule,
        FontAwesomeModule
    ],
    declarations: [HeaderComponent],
    exports: [HeaderComponent]
})
export class HeaderModule { }
