import { Component, NgModule, OnInit } from "@angular/core";
import { ConfigService } from "src/app/services/config.service";
import { LoginService } from "src/app/services/login.service";
import { Router } from "@angular/router";
import { CommonModule } from "@angular/common";

@Component({
    selector: "app-footer",
    template: `
        <footer><ng-content></ng-content></footer>
    `,
    templateUrl: "./footer.component.html",
    styleUrls: ["./footer.component.scss"]
})

export class FooterComponent implements OnInit {
    version: any;

    currentYear;

    constructor(private config: ConfigService, private login: LoginService, private router: Router) {
        this.currentYear = new Date().getFullYear();
    }

    async ngOnInit() {
        this.version = await this.config.getVersion();
    }
}

@NgModule({
    imports: [CommonModule],
    declarations: [FooterComponent],
    exports: [FooterComponent]
})
export class FooterModule { }
