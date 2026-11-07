import { Component, OnInit, ChangeDetectionStrategy } from "@angular/core";
import { ConfigService } from "src/app/services/config.service";
import { LoginService } from "src/app/services/login.service";
import { Router } from "@angular/router";
import { AppService } from "src/app/services/app.service";
import { PluginsService, PluginState } from "src/app/services/plugins.service";
import { SatelliteService } from "src/app/services/satellite.services";
import { LogsService } from "src/app/services/logs.service";
import { SystemService } from "src/app/services/system.service";
import { RunState } from "src/app/services/server-state.service";
import { NodeInstance, NodeInstanceState } from "src/app/base/model/node-instance";
import { PluginStateInstance } from "../plugins/plugins.component";
import { describeLicenseErrors, parseLicense, LicenseInfo } from "../system/license-info";
import { L10nTranslationService } from "angular-l10n";

type Tone = "ok" | "warn" | "bad" | "off";

interface DriverProblem {
  name: string;
  stateKey: string;
}

interface RecentError {
  time: string;
  date: string;
  level: string;
  facility: string;
  message: string;
}

const RUNNING_STATES = [NodeInstanceState.Loaded, NodeInstanceState.Initialized, NodeInstanceState.InUse, NodeInstanceState.Remote];

const STATE_KEYS: { [state: number]: string } = {
  [NodeInstanceState.New]: "UI.HOME.DRIVER_STATE_NEW",
  [NodeInstanceState.Saved]: "UI.HOME.DRIVER_STATE_NEW",
  [NodeInstanceState.OutOfDataPoints]: "UI.HOME.DRIVER_STATE_DATAPOINTS",
  [NodeInstanceState.UnknownError]: "UI.HOME.DRIVER_STATE_ERROR",
  [NodeInstanceState.Unloaded]: "UI.HOME.DRIVER_STATE_UNLOADED",
  [NodeInstanceState.Unknown]: "UI.HOME.DRIVER_STATE_UNKNOWN",
  [NodeInstanceState.OutOfSatelliteLicenses]: "UI.HOME.DRIVER_STATE_SATELLITES"
};

// "2026-11-06 21:48:19.695 +01:00 [ERR] facility: message"
const LOG_LINE = /^(\d{4}-\d{2}-\d{2}) (\d{2}:\d{2}:\d{2})\S* \S+ \[(ERR|FTL)\] ([^:]+): (.*)$/;

@Component({
    templateUrl: "home.component.html",
    styleUrls: ["./home.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class HomeComponent implements OnInit {
    public version: any = {};

    serverState: RunState = void 0;
    serverStateFailed = false;

    driversLoaded = false;
    driversFailed = false;
    driversRunning = 0;
    driversDisabled = 0;
    driverProblems: DriverProblem[] = [];

    satellitesLoaded = false;
    satellitesFailed = false;
    satellitesOnline = 0;
    satellitesOffline = 0;

    pluginsLoaded = false;
    pluginsFailed = false;
    pluginUpdates = 0;
    pluginsInstalled = 0;
    pluginErrors = 0;

    licenseLoaded = false;
    licenseInfo: LicenseInfo;
    licenseProblems = 0;

    errorsLoaded = false;
    errorsFailed = false;
    recentErrors: RecentError[] = [];

    readonly year = new Date().getFullYear();

    constructor(private config: ConfigService,
        private loginService: LoginService,
        private router: Router,
        private pluginsService: PluginsService,
        private satelliteService: SatelliteService,
        private logsService: LogsService,
        private systemService: SystemService,
        private translate: L10nTranslationService,
        appService: AppService) {
        appService.setAppTitle("UI.HOME.TITLE");
    }

    async ngOnInit() {
        if (!this.loginService.getUserFromLocalStore()) {
            this.router.navigateByUrl("/login");
            return;
        }

        // every block loads on its own, a slow cloud call must not hold back the rest
        this.loadVersion();
        this.loadServerState();
        this.loadDrivers();
        this.loadSatellites();
        this.loadLicense();
        this.loadPlugins();
        this.loadRecentErrors();
    }

    get serverTone(): Tone {
        if (this.serverStateFailed) {
            return "bad";
        }
        if (this.serverState === RunState.Started) {
            return "ok";
        }
        if (this.serverState === RunState.Stopped) {
            return "bad";
        }
        return this.serverState === void 0 ? "off" : "warn";
    }

    get serverStateKey(): string {
        if (this.serverStateFailed) {
            return "UI.HOME.SERVER_UNREACHABLE";
        }
        switch (this.serverState) {
            case RunState.Started: return "UI.HOME.SERVER_RUNNING";
            case RunState.Stopped: return "UI.HOME.SERVER_STOPPED";
            case void 0: return "UI.HOME.LOADING";
            default: return "UI.HOME.SERVER_STARTING";
        }
    }

    get driverTone(): Tone {
        if (this.driversFailed) {
            return "bad";
        }
        if (!this.driversLoaded) {
            return "off";
        }
        return this.driverProblems.length > 0 ? "bad" : "ok";
    }

    get satelliteTone(): Tone {
        if (this.satellitesFailed) {
            return "bad";
        }
        if (!this.satellitesLoaded || this.satellitesOnline + this.satellitesOffline === 0) {
            return "off";
        }
        return this.satellitesOffline > 0 ? "warn" : "ok";
    }

    get pluginTone(): Tone {
        if (this.pluginsFailed) {
            return "warn";
        }
        if (!this.pluginsLoaded) {
            return "off";
        }
        if (this.pluginErrors > 0) {
            return "bad";
        }
        if (this.pluginsInstalled === 0 && this.pluginUpdates === 0) {
            return "off";
        }
        return this.pluginUpdates > 0 ? "warn" : "ok";
    }

    get licenseTone(): Tone {
        if (!this.licenseLoaded) {
            return "off";
        }
        if (!this.licenseInfo) {
            return "warn";
        }
        return this.licenseProblems > 0 || this.licenseInfo.isExpired ? "bad" : "ok";
    }

    get licenseStateKey(): string {
        if (!this.licenseLoaded) {
            return "UI.HOME.LOADING";
        }
        if (!this.licenseInfo) {
            return "UI.SYSTEM.LICENSE_NONE";
        }
        if (this.licenseInfo.isExpired) {
            return "UI.SYSTEM.LICENSE_EXPIRED";
        }
        return this.licenseProblems > 0 ? "UI.SYSTEM.LICENSE_INVALID" : "UI.SYSTEM.LICENSE_VALID";
    }

    formatDate(d: Date) {
        if (!d) {
            return "";
        }
        return d.toLocaleDateString(this.translate.getLocale()?.language || undefined, { year: "numeric", month: "2-digit", day: "2-digit" });
    }

    private async loadVersion() {
        try {
            this.version = await this.config.getVersion();
        } catch {
            this.version = {};
        }
    }

    private async loadServerState() {
        try {
            const state = await this.config.getServerState();
            this.serverState = state?.status;
        } catch {
            this.serverStateFailed = true;
        }
    }

    private async loadDrivers() {
        try {
            const roots: NodeInstance[] = await this.config.getNodeInstances() || [];
            let running = 0;
            let disabled = 0;
            const problems: DriverProblem[] = [];

            // root -> interfaces (Ethernet, USB, Virtual, Remote, ...) -> driver instances
            for (const root of roots) {
                for (const iface of root.Children || []) {
                    for (const driver of iface.Children || []) {
                        if (driver.IsAnyDisabled || (driver.State as number) === 11) {
                            disabled++;
                        } else if (RUNNING_STATES.includes(driver.State)) {
                            running++;
                        } else {
                            problems.push({
                                name: driver.DisplayName || driver.Name,
                                stateKey: STATE_KEYS[driver.State] || "UI.HOME.DRIVER_STATE_UNKNOWN"
                            });
                        }
                    }
                }
            }
            this.driversRunning = running;
            this.driversDisabled = disabled;
            this.driverProblems = problems;
        } catch {
            this.driversFailed = true;
        }
        this.driversLoaded = true;
    }

    private async loadSatellites() {
        try {
            const satellites = await this.satelliteService.getAll() || [];
            this.satellitesOnline = satellites.filter(a => a.Connected).length;
            this.satellitesOffline = satellites.length - this.satellitesOnline;
        } catch {
            this.satellitesFailed = true;
        }
        this.satellitesLoaded = true;
    }

    private async loadPlugins() {
        try {
            const plugins: PluginState[] = await this.pluginsService.getPlugins() || [];
            const states = plugins.filter(p => p?.cloudPlugin).map(p => new PluginStateInstance(p));
            this.pluginsInstalled = states.filter(a => a.isInstalled).length;
            this.pluginUpdates = states.filter(a => a.isInstalled && a.cloudIsNewer).length;
        } catch {
            this.pluginsFailed = true;
        }
        this.pluginsLoaded = true;
    }

    private async loadLicense() {
        try {
            this.licenseInfo = parseLicense(await this.systemService.getLicense());
        } catch {
            this.licenseInfo = void 0;
        }
        try {
            this.licenseProblems = describeLicenseErrors(await this.systemService.getLicenseErrors()).length;
        } catch {
            this.licenseProblems = 0;
        }
        this.licenseLoaded = true;
    }

    /** the last errors from today's combined log file ("all<date>.log") */
    private async loadRecentErrors() {
        try {
            const files: string[] = await this.logsService.getLogFiles() || [];
            const all = files.filter(f => /\/all\d{8}(_\d+)?\.log$/i.test(f)).sort();
            const latest = all[all.length - 1];
            if (!latest) {
                this.errorsLoaded = true;
                return;
            }
            const text = String(await this.logsService.getLogFile(encodeURIComponent(latest)) ?? "");
            const lines = text.split(/\r?\n/);
            const found: RecentError[] = [];
            for (let i = lines.length - 1; i >= 0 && found.length < 6; i--) {
                const m = LOG_LINE.exec(lines[i]);
                if (m) {
                    found.push({ date: m[1], time: m[2], level: m[3], facility: m[4], message: m[5] });
                }
            }
            this.recentErrors = found;
        } catch {
            this.errorsFailed = true;
        }
        this.errorsLoaded = true;
    }

    shortFacility(f: string) {
        const parts = (f || "").split(".");
        return parts[parts.length - 1] || f;
    }
}
