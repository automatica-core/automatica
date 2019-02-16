import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { TranslationService } from "angular-l10n";
import { Injectable } from "@angular/core";
import { BaseService } from "./base-service";
import { User } from "../base/model/user/user";
import { Role } from "../base/model/user/role";

@Injectable()
export class UsersService extends BaseService {
    constructor(httpService: HttpClient, pRouter: Router, translationService: TranslationService) {
        super(httpService, pRouter, translationService);

    }

    getUsers(): Promise<User[]> {
        return super.getMultiple<User>("usermgm/users");
    }

    getRoles(): Promise<Role[]> {
        return super.getMultiple<Role>("usermgm/roles");
    }

    saveUsers(ususersrGroups: User[]) {
        const data = new Array<any>();
        for (const set of ususersrGroups) {
            data.push(set.toJson());
        }
        return super.postMultiple("usermgm/users", data);
    }
}
