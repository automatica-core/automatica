import { Component, OnInit } from "@angular/core";

import { LoginService } from "src/app/services/login.service";
import { Router } from "@angular/router";
import { AppService } from "src/app/services/app.service";


@Component({
    selector: "app-login-form",
    templateUrl: "./login-form.component.html",
    styleUrls: ["./login-form.component.scss"]
})
export class LoginFormComponent implements OnInit {
    login = "";
    password = "";
    isLoading = true;

    constructor(private loginService: LoginService, private router: Router, private appService: AppService) {
        localStorage.removeItem("jwt");
    }

    ngOnInit() {
        this.isLoading = false;
    }

    async onLoginClick(args) {
        this.isLoading = true;
        if (!args.validationGroup.validate().isValid) {
            return;
        }

        try {
            const value = await this.loginService.login(this.login, this.password);
            this.loginService.saveToLocalStorage(value);

            if (value) {
                this.router.navigate(["/"]);
            }
        } catch (error) {

        }

        this.isLoading = false;
    }
}

