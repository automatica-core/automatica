
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { Injectable } from "@angular/core";
import { BaseService } from "./base-service";
import { UserGroup } from "../base/model/user/user-group";
import { Role } from "../base/model/user/role";

@Injectable()
export class GroupsService extends BaseService {
    constructor(httpService: HttpClient, pRouter: Router, translationService: L10nTranslationService) {
        super(httpService, pRouter, translationService);

    }

    getUserGroups(): Promise<UserGroup[]> {
        return super.getMultiple<UserGroup>("usermgm/usergroups");
    }

    getRoles(): Promise<Role[]> {
        return super.getMultiple<Role>("usermgm/roles");
    }

    saveUserGroups(userGroups: UserGroup[]) {
        const data = new Array<any>();
        for (const set of userGroups) {
            data.push(set.toJson());
        }
        return super.postMultiple("usermgm/usergroups", data);
    }
}
