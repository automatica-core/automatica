import { Component, NgModule, Input, Output, EventEmitter } from "@angular/core";
import { CommonModule } from "@angular/common";

import { UserPanelModule } from "../user-panel/user-panel.component";
import { DxButtonModule } from "devextreme-angular/ui/button";
import { DxToolbarModule } from "devextreme-angular/ui/toolbar";
import { DxPopupModule } from "devextreme-angular/ui/popup";
import { Router, ActivatedRoute } from "@angular/router";
import { LoginFormModule } from "../login-form/login-form.module";
import { AppService } from "src/app/services/app.service";
import { TranslationModule } from "angular-l10n";
import { LoginService } from "src/app/services/login.service";
import { HubConnectionService } from "src/app/base/communication/hubs/hub-connection.service";

@Component({
    selector: "app-header",
    templateUrl: "header.component.html",
    styleUrls: ["./header.component.scss"]
})

export class HeaderComponent {
    @Output()
    menuToggle = new EventEmitter<boolean>();

    @Input()
    menuToggleEnabled = false;

    constructor(private router: Router,
        private activatedRoute: ActivatedRoute,
        public appService: AppService,
        private loginService: LoginService,
        private hubService: HubConnectionService) { }

    get title() {
        return this.appService.title;
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
    async logout($event) {
        await this.hubService.stop();
        await this.loginService.logout();
        this.router.navigate(["/login"]);
    }

    navigateToAdminPage($event) {
        this.router.navigate(["admin", "home"]);
    }

    navigateToVisualization($event) {
        this.router.navigate(["visualization"]);
    }
}

@NgModule({
    imports: [
        CommonModule,
        DxPopupModule,
        DxButtonModule,
        DxToolbarModule,
        TranslationModule
    ],
    declarations: [HeaderComponent],
    exports: [HeaderComponent]
})
export class HeaderModule { }
