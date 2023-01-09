import { Component, OnInit, Input, OnDestroy, ChangeDetectorRef } from "@angular/core";
import { VisuPage, VisuPageGroupType } from "src/app/base/model/visu-page";
import { VisuObjectTemplate } from "src/app/base/model/visu-object-template";
import { BaseComponent } from "src/app/base/base-component";
import { VisuService } from "src/app/services/visu.service";
import { ActivatedRoute, Router } from "@angular/router";
import { NotifyService } from "src/app/services/notify.service";
import { L10nTranslationService } from "angular-l10n";
import { ConfigService } from "src/app/services/config.service";
import { LoginService } from "src/app/services/login.service";
import { AppService } from "src/app/services/app.service";
import { DeviceService } from "src/app/services/device/device.service";
import { VisuObjectInstance, VisuObjectSourceType } from "src/app/base/model/visu-object-instance";
import { VisualizationDataFacade } from "src/app/base/model/visualization-data-facade";
import { NodeDataTypeEnum } from "src/app/base/model/node-data-type";
import { VisuObjectMobileInstance } from "src/app/base/model/visu";
import { DataService } from "src/app/services/data.service";

@Component({
  selector: "app-mobile-container",
  templateUrl: "./mobile-container.component.html",
  styleUrls: ["./mobile-container.component.scss"]
})
export class MobileContainerComponent extends BaseComponent implements OnInit, OnDestroy {


  @Input()
  public page: VisuPage;

  private visuTemplates: VisuObjectTemplate[] = [];
  private visuTemplatesMap = new Map<string, VisuObjectTemplate>();
  version: any;

  public pageGroupType: VisuPageGroupType = VisuPageGroupType.Favorites;

  constructor(private visuService: VisuService,
    private route: ActivatedRoute,
    notify: NotifyService,
    translate: L10nTranslationService,
    private configService: ConfigService,
    appService: AppService,
    private dataService: DataService) {

    super(notify, translate, appService);
  }

  async ngOnInit() {
    this.appService.isLoading = true;

    await this.dataService.getAllValues();

    this.visuTemplates = await this.visuService.getVisuTemplates();


    for (const v of this.visuTemplates) {
      this.visuTemplatesMap.set(v.ObjId, v);
    }
    this.version = await this.configService.getVersion();
    try {


      if (!this.page) {
        if (this.route.snapshot.data.loadHomepage) {
          this.pageGroupType = VisuPageGroupType.Favorites;
          const data = await this.visuService.getFavorites();
          this.initPage(data);

        } else {
          super.registerObservable(this.route.params, async (params) => {

            this.appService.isLoading = true;
            if (!params.id) {
              return;
            }

            const id = params["id"];
            const type = params["type"];

            if (!type) {
              return;
            }

            if (type === "area") {
              this.pageGroupType = VisuPageGroupType.Area;
            } else {
              this.pageGroupType = VisuPageGroupType.Category;
            }

            const data = await this.visuService.getVisuPage(id);
            this.initPage(data);


            this.appService.isLoading = false;
          });
        }

      }
    } catch (error) {
      this.notifyService.notifyError(error);
      console.error(error);
    }

    this.appService.isLoading = false;
  }

  private initPage(data) {
    if (data instanceof VisualizationDataFacade) {
      this.page = new VisuPage();
      this.page.Height = 3;
      this.page.Width = 5;

      const visuObjectInstances = [];

      for (const x of data.NodeInstances) {
        if (x.NodeTemplate.This2DefaultMobileVisuTemplate && this.visuTemplatesMap.has(x.NodeTemplate.This2DefaultMobileVisuTemplate)) {
          const instance = VisuObjectMobileInstance.CreateFromTemplate(this.visuTemplatesMap.get(x.NodeTemplate.This2DefaultMobileVisuTemplate), x, VisuObjectSourceType.NodeInstance);

          instance.ObjId = x.ObjId;
          this.getAndSetProperty(instance, "nodeInstance", x.ObjId);
          this.getAndSetProperty(instance, "text", x.Name);
          this.getAndSetProperty(instance, "readonly", !x.IsWriteable);
          this.getAndSetProperty(instance, "min", 0);
          this.getAndSetProperty(instance, "max", 100);


          if (x.NodeTemplate.This2NodeDataType === NodeDataTypeEnum.Boolean) {
            instance.StateTextValueTrue = x.StateTextValueTrue;
            instance.StateTextValueFalse = x.StateTextValueFalse;
            instance.StateColorValueTrue = x.StateColorValueTrue;
            instance.StateColorValueFalse = x.StateColorValueFalse;
          }
          instance.VisuName = x.VisuName;

          visuObjectInstances.push(instance);
        }
      }

      for (const x of data.RuleInstances) {
        const instance = VisuObjectMobileInstance.CreateFromTemplate(this.visuTemplatesMap.get(x.RuleTemplate.This2DefaultMobileVisuTemplate), x, VisuObjectSourceType.RuleInstance);
        visuObjectInstances.push(instance);
      }

      this.page.VisuObjectInstances = visuObjectInstances;
    } else if (data instanceof VisuPage) {
      this.page = data;
    }
  }

  private getAndSetProperty(instance: VisuObjectInstance, property: string, value: any) {
    const prop = instance.getProperty(property);
    if (prop) {
      prop.Value = value;
    }
  }
  sidebarToggle($event) {
    $event.preventDefault();
    document.querySelector("body").classList.toggle("sidebar-hidden");
  }

  ngOnDestroy() {
    super.baseOnDestroy();
  }

}
