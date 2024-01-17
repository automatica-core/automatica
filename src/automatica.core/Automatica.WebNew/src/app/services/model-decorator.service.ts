import { Injectable } from "@angular/core";
import { VisuObjectMobileInstance } from "../base/model/visu";
import { User } from "../base/model/user/user";
import { UserGroup } from "../base/model/user/user-group";
import { Role } from "../base/model/user/role";
import { User2Role } from "../base/model/user/user2role";
import { TelegramMonitorInstance } from "../base/model/telegram-monitor/telegram-monitor-instance";
import { TelegramMessage } from "../base/model/telegram-monitor/telegram-message";
import { WebApiException } from "../base/model/web-api-exception";
import { Setting } from "../base/model/setting";
import { PropertyTemplateConstraint } from "../base/model/property-template-constraint";
import { PropertyTemplateConstraintData } from "../base/model/property-template-constraint-data";
import { VisuObjectTemplate } from "../base/model/visu-object-template";
import { VisuObjectInstance, VisuObjectSourceType } from "../base/model/visu-object-instance";
import { PropertyTemplate } from "../base/model/property-template";
import { AreaInstance, AreaTemplate, AreaType } from "../base/model/areas";
import { CategoryGroup, CategoryInstance } from "../base/model/categories";
import { Link } from "../base/model/link";
import { Trending } from "../base/model/trending/trending";
import { Satellite } from "../base/model/satellites/satellite";
import { VisualizationDataFacade } from "../base/model/visualization-data-facade";
import { CalendarPropertyData, CalendarPropertyDataEntry } from "../base/model/calendar-property-data";
import { ControlConfiguration } from "../base/model/control-configuration";
import { Control } from "../base/model/control";

@Injectable()
export class ModelDecoratorService {

    constructor() {
        // tslint:disable:no-unused-expression
        new Link();
        new PropertyTemplateConstraint();
        new PropertyTemplateConstraintData();

        new VisuObjectTemplate();
        new VisuObjectMobileInstance(VisuObjectSourceType.NodeInstance);
        new VisuObjectInstance();

        new PropertyTemplate();

        new AreaInstance(void 0);
        new AreaTemplate();
        new AreaType();

        new CategoryGroup();
        new CategoryInstance();

        new User();
        new UserGroup();
        new Role();
        new User2Role();
        new TelegramMonitorInstance();
        new TelegramMessage();
        new WebApiException();
        new Setting();
        new VisualizationDataFacade();

        new Trending();
        new Satellite();

        new CalendarPropertyData();
        new CalendarPropertyDataEntry();

        new Control();
        new ControlConfiguration();
        // tslint:enable:no-unused-expression
    }
}
