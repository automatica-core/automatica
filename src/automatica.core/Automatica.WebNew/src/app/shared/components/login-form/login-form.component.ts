import { Component, ChangeDetectorRef, ViewChild, Inject, OnInit, OnDestroy, ChangeDetectionStrategy } from "@angular/core";

import { LoginService } from "src/app/services/login.service";
import { Router } from "@angular/router";
import { AppService } from "src/app/services/app.service";
import { DxValidationGroupComponent } from "devextreme-angular";
import { L10N_LOCALE, L10nLocale, L10nTranslationService } from "angular-l10n";
import { BaseServiceHelper } from "src/app/services/base-server-helper";
import { NotifyService } from "src/app/services/notify.service";
import { DeviceService } from "src/app/services/device/device.service";
import { DiscoveredServer, ServerDiscoveryService } from "src/app/services/device/server-discovery.service";
import { ConfigService } from "src/app/services/config.service";


@Component({
    selector: "app-login-form",
    templateUrl: "./login-form.component.html",
    styleUrls: ["./login-form.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class LoginFormComponent implements OnInit, OnDestroy {
    login = "";
    password = "";
    serverIp = "";
    saveLogin: boolean = false;

    isWeb: boolean = false;

    /** the server requires a new password before the login (initial or default password) */
    mustChangePassword = false;
    newPassword = "";
    newPasswordConfirm = "";
    private pendingToken: string;

    /** shown under the fields instead of a toast */
    errorKey: string = void 0;
    /** a second line with what to do; only the UI.LOGIN errors have one */
    errorHint: string = void 0;
    serverVersion: string = void 0;
    isBusy = false;

    discoveredServers: DiscoveredServer[] = [];
    searchingServers = false;
    private stopDiscovery: () => Promise<void>;
    private destroyed = false;

    @ViewChild("validationGroup", { static: true })
    validationGroup: DxValidationGroupComponent;

    constructor(private loginService: LoginService,
        private router: Router,
        private appService: AppService,
        private changeRef: ChangeDetectorRef,
        private notifyService: NotifyService,
        @Inject(L10N_LOCALE) public locale: L10nLocale,
        private deviceService: DeviceService,
        private serverDiscovery: ServerDiscoveryService,
        private configService: ConfigService,
        private translation: L10nTranslationService) {
        // showing the login ends the session (token, cached user, and the plain text password older versions saved)
        this.loginService.logout();

        this.isWeb = deviceService.isWeb;

        this.serverIp = BaseServiceHelper.getSignalRBaseUrl();
        // only the user name is remembered, the password is never stored
        this.login = localStorage.getItem("s1user");

    }

    ngOnInit(): void {
        // whatever was loading before belongs to the old session
        this.appService.resetLoading();
        this.appService.isStarting = false;
        this.changeRef.detectChanges();

        if (!this.isWeb && this.serverDiscovery.isAvailable) {
            this.startDiscovery();
        }

        this.loadVersion();
    }

    /** the version endpoint needs no login; in the app it only works once a server is set */
    private async loadVersion() {
        if (!this.isWeb && !this.serverIp) {
            return;
        }
        try {
            const version = await this.configService.getVersion();
            this.serverVersion = version?.version;
        } catch {
            this.serverVersion = void 0;
        }
        if (!this.destroyed) {
            this.changeRef.detectChanges();
        }
    }

    /** validation messages in the current language */
    requiredMessage(key: string) {
        return this.translation.translate(key);
    }

    clearError() {
        if (this.errorKey) {
            this.errorKey = void 0;
            this.errorHint = void 0;
        }
    }

    private setError(key: string, withHint = true) {
        this.errorKey = key;
        this.errorHint = withHint ? key + "_HINT" : void 0;
    }

    ngOnDestroy(): void {
        this.destroyed = true;
        this.stopDiscovery?.();
    }

    private async startDiscovery() {
        this.searchingServers = true;
        const stop = await this.serverDiscovery.start(servers => {
            this.discoveredServers = servers;

            // nothing entered yet: take the first server found, the user can still pick another one
            if (!this.serverIp && servers.length > 0) {
                this.serverIp = servers[0].url;
            }
            this.changeRef.detectChanges();
        });

        if (this.destroyed) {
            await stop();
            return;
        }
        this.stopDiscovery = stop;
    }

    selectServer(server: DiscoveredServer) {
        this.serverIp = server.url;
        this.clearError();
        this.changeRef.detectChanges();
    }

    async onEnterPressed($event) {
        if (this.mustChangePassword) {
            await this.onChangePasswordClick($event);
            return;
        }
        await this.onLoginClick($event);
    }

    passwordsMatch = () => this.newPassword === this.newPasswordConfirm;

    cancelPasswordChange() {
        this.loginService.logout();
        this.mustChangePassword = false;
        this.pendingToken = void 0;
        this.password = "";
        this.newPassword = "";
        this.newPasswordConfirm = "";
        this.clearError();
        this.changeRef.detectChanges();
    }

    async onChangePasswordClick(args) {
        if (!this.validationGroup.instance.validate().isValid) {
            return;
        }

        const loadingDone = this.appService.beginLoading();
        this.clearError();
        this.isBusy = true;
        this.changeRef.detectChanges();
        try {
            const user = await this.loginService.changePassword(this.pendingToken, this.password, this.newPassword);
            this.pendingToken = void 0;
            this.mustChangePassword = false;
            this.finishLogin(user);
        } catch (error) {
            const key = typeof error?.error === "string" ? error.error : error?.error?.error;
            if (error?.status === 429) {
                this.setError("ERROR.TOO_MANY_LOGIN_ATTEMPTS", false);
            } else if (error?.status === 401 || error?.status === 403) {
                this.cancelPasswordChange();
                this.setError("UI.LOGIN.ERROR_CREDENTIALS");
            } else if (key && /^[A-Z_]+$/.test(key)) {
                this.setError("ERROR." + key, false);
            } else {
                console.error(error);
                this.setError("ERROR.PASSWORD_CHANGE_FAILED", false);
            }
        } finally {
            this.isBusy = false;
            loadingDone();
            this.changeRef.detectChanges();
        }
    }

    private finishLogin(value) {
        this.loginService.saveToLocalStorage(value);

        if (this.saveLogin && !this.isWeb) {
            localStorage.setItem("s1user", this.login);
        }

        this.loginService.userLoggedIn.emit(value);
        this.router.navigate(["/"]);
    }

    async onDemoLogin($event) {
        localStorage.setItem("s1server", "https://demo.automaticacore.com");

        this.serverIp = "https://demo.automaticacore.com";
        this.login = "sa";
        this.password = "sa";
        await this.onLoginClick($event);
    }

    async onLoginClick(args) {
        const loadingDone = this.appService.beginLoading();
        this.clearError();
        this.isBusy = true;
        this.changeRef.detectChanges();

        try {
            if (!this.isWeb) {
                localStorage.setItem("s1server", this.serverIp);
            }

            if (!this.validationGroup.instance.validate().isValid) {
                return;
            }

            try {
                const value = await this.loginService.login(this.login, this.password);
                if (!value) {
                    // older servers answer wrong credentials with 204 instead of 401
                    this.setError("UI.LOGIN.ERROR_CREDENTIALS");
                } else if (value.MustChangePassword) {
                    // the token only allows the password change
                    this.pendingToken = value.Token;
                    this.mustChangePassword = true;
                    this.newPassword = "";
                    this.newPasswordConfirm = "";
                } else {
                    this.finishLogin(value);
                }
            } catch (error) {
                if (error.status === 0) {
                    this.setError("UI.LOGIN.ERROR_SERVER");
                }
                else if (error.status === 401) {
                    this.setError("UI.LOGIN.ERROR_CREDENTIALS");
                }
                else if (error.status === 429) {
                    this.setError("ERROR.TOO_MANY_LOGIN_ATTEMPTS", false);
                }
                else {
                    console.error(error);
                    this.setError("UI.LOGIN.ERROR_FAILED");
                }
            }
        }
        finally {
            this.isBusy = false;
            loadingDone();
            this.changeRef.detectChanges();
        }
    }
}

