import { Component, OnInit } from "@angular/core";
import { ConfigService } from "src/app/services/config.service";
import { LoginService } from "src/app/services/login.service";
import { Router } from "@angular/router";
import { AppService } from "src/app/services/app.service";

@Component({
    templateUrl: "home.component.html",
    styleUrls: ["./home.component.scss"]
})

export class HomeComponent implements OnInit {
    public version: any = {};
    constructor(private config: ConfigService, private loginService: LoginService, private router: Router, appService: AppService) {
        appService.setAppTitle("DASHBOARD.NAME");
    }

    async ngOnInit() {
        this.version = await this.config.getVersion();

        if (!this.loginService.getUserFromLocalStore()) {
            this.router.navigateByUrl("/login");
        }
    }

    infoPageOnClick($event) {
        window.open("http://www.automaticacore.com", "_blank")
    }
}
