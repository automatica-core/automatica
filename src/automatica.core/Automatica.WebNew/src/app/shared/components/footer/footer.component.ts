import { Component, NgModule, OnInit, ChangeDetectorRef } from "@angular/core";
import { ConfigService } from "src/app/services/config.service";
import { LoginService } from "src/app/services/login.service";
import { Router } from "@angular/router";
import { CommonModule } from "@angular/common";
import { DeviceDetectorService } from "automatica-ngx-device-detector";

@Component({
    selector: "app-footer",
    templateUrl: "./footer.component.html",
    styleUrls: ["./footer.component.scss"]
})

export class FooterComponent implements OnInit {
    version: any = { version: "0" };

    currentYear;

    constructor(private config: ConfigService,
        private deviceService: DeviceDetectorService,
        private changeRef: ChangeDetectorRef) {
        this.currentYear = new Date().getFullYear();
    }

    async ngOnInit() {
        this.version = await this.config.getVersion();

        this.changeRef.detectChanges();
    }

    public get isMobile() {
        return this.deviceService.isMobile();
    }
}

@NgModule({
    imports: [CommonModule],
    declarations: [FooterComponent],
    exports: [FooterComponent]
})
export class FooterModule { }
