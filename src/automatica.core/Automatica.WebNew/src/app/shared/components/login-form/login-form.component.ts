import { Component, ChangeDetectorRef, ViewChild, Inject } from "@angular/core";

import { LoginService } from "src/app/services/login.service";
import { Router } from "@angular/router";
import { AppService } from "src/app/services/app.service";
import { DxValidationGroupComponent } from "devextreme-angular";
import { L10N_LOCALE, L10nLocale } from "angular-l10n";
import { Capacitor } from '@capacitor/core';
import { BaseServiceHelper } from "src/app/services/base-server-helper";
import { NotifyService } from "src/app/services/notify.service";


@Component({
    selector: "app-login-form",
    templateUrl: "./login-form.component.html",
    styleUrls: ["./login-form.component.scss"]
})
export class LoginFormComponent{
    login = "";
    password = "";
    serverIp = "";
    saveLogin: boolean = false;

    isWeb: boolean = false;

    @ViewChild("validationGroup", { static: true })
    validationGroup: DxValidationGroupComponent;

    constructor(private loginService: LoginService,
        private router: Router,
        private appService: AppService,
        private changeRef: ChangeDetectorRef,
        private notifyService: NotifyService,
        @Inject(L10N_LOCALE) public locale: L10nLocale) {
        localStorage.removeItem("jwt");

        this.isWeb = Capacitor.getPlatform() === "web";

        this.serverIp = BaseServiceHelper.getSignalRBaseUrl();
        this.login = localStorage.getItem("s1user");
        this.password = localStorage.getItem("s1pw");

        this.appService.isLoading = false;
    }

    async onEnterPressed($event) {
        await this.onLoginClick($event);
    }

    async onDemoLogin($event) {
        localStorage.setItem("s1server", "https://demo.automaticacore.com");
        
        this.serverIp = "https://demo.automaticacore.com";
        this.login = "sa";
        this.password = "sa";
        await this.onLoginClick($event);
    }

    async onLoginClick(args) {

        if (!this.isWeb) {
            localStorage.setItem("s1server", this.serverIp);
        }
        

        this.appService.isLoading = true;
        if (!this.validationGroup.instance.validate().isValid) {
            this.appService.isLoading = false;
            return;
        }

        try {
            const value = await this.loginService.login(this.login, this.password);
            if (!value) {
                this.notifyService.notifyError("Login failed...");
            } else {
                this.loginService.saveToLocalStorage(value);
                
                if(this.saveLogin && !this.isWeb) {
                    localStorage.setItem("s1user", this.login);
                    localStorage.setItem("s1pw", this.password);
                }
                
                this.router.navigate(["/"]);
            }
        } catch (error) {
            this.notifyService.notifyError("Login failed..." + error.toString());
        }

        this.appService.isLoading = false;
    }
}

