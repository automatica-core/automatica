import { Component, OnInit, Output, EventEmitter, Input, ViewChild, OnDestroy } from "@angular/core";
import { GridsterConfig, GridsterItem, GridsterComponent } from "angular-gridster2";
import { ActivatedRoute, Router } from "@angular/router";
import { TranslationService } from "angular-l10n";
import { ConfigService } from "../../../services/config.service";
import { LoginService } from "../../../services/login.service";
import { BaseService } from "src/app/services/base-service";
import { VisuObjectMobileInstance } from "src/app/base/model/visu";
import { NotifyService } from "src/app/services/notify.service";
import { VisuService } from "src/app/services/visu.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { VisuPage, VisuPageType } from "src/app/base/model/visu-page";
import { VisuObjectTemplate } from "src/app/base/model/visu-object-template";
import { BaseModel } from "src/app/base/model/base-model";
import { NodeInstance } from "src/app/base/model/node-instance";
import { VisuObjectInstance } from "src/app/base/model/visu-object-instance";
import { RuleInstance } from "src/app/base/model/rule-instance";
import { AutomaticVisualizationData } from "src/app/base/model/automatic-visualization-data";
import { NodeDataType, NodeDataTypeEnum } from "src/app/base/model/node-data-type";

@Component({
  selector: "mobile-container",
  templateUrl: "./mobile-container.component.html",
  styleUrls: ["./mobile-container.component.scss"]
})
export class MobileContainerComponent extends BaseComponent implements OnInit, OnDestroy {

  options: GridsterConfig;
  private _selectedItem: VisuObjectMobileInstance;

  @Input()
  public get selectedItem(): VisuObjectMobileInstance {
    return this._selectedItem;
  }
  public set selectedItem(v: VisuObjectMobileInstance) {
    this._selectedItem = v;
    this.selectedItemChange.emit(v);
  }

  @Input()
  public page: VisuPage;

  public selectionEnabled: boolean = true;

  @Output()
  selectedItemChange = new EventEmitter<VisuObjectMobileInstance>();

  @ViewChild("gridster")
  gridster: GridsterComponent;

  version: any;

  private visuTemplates: VisuObjectTemplate[] = [];
  private visuTemplatesMap = new Map<string, VisuObjectTemplate>();

  constructor(private visuService: VisuService,
    private route: ActivatedRoute,
    notify: NotifyService,
    translate: TranslationService,
    private configService: ConfigService,
    private router: Router,
    private login: LoginService,
    private appService: AppService) {

    super(notify, translate);
  }

  itemChange(item, itemComponent) {
    // console.log("itemChanged", item, itemComponent);
  }

  itemResize(item: VisuObjectMobileInstance, itemComponent) {
    // console.log("itemResized", item, itemComponent);
    if (item.itemResized) {
      item.itemResized.emit();
    }
  }

  async ngOnInit() {

    this.appService.isLoading = true;
    this.visuTemplates = await this.visuService.getVisuTemplates();
    for (const v of this.visuTemplates) {
      this.visuTemplatesMap.set(v.ObjId, v);
    }
    this.version = await this.configService.getVersion();

    this.appService.isLoading = true;
    try {


      if (!this.page) {
        if (this.route.snapshot.data.loadHomepage) {
          this.page = await this.visuService.getDefaultVisuPage(VisuPageType.Mobile);

          this.initGridster();
        } else {
          super.registerObservable(this.route.params, async (params) => {
            this.appService.isLoading = true;
            const id = params["id"];
            if (!params.id) {
              return;
            }

            const data = await this.visuService.getVisuPage(id);

            if (data instanceof AutomaticVisualizationData) {
              this.page = new VisuPage();
              this.page.Height = 3;
              this.page.Width = 5;

              const visuObjectInstances = [];

              for (const x of data.NodeInstances) {
                if (x.NodeTemplate.This2DefaultMobileVisuTemplate && this.visuTemplatesMap.has(x.NodeTemplate.This2DefaultMobileVisuTemplate)) {
                  const instance = VisuObjectInstance.CreateFromTemplate(this.visuTemplatesMap.get(x.NodeTemplate.This2DefaultMobileVisuTemplate));

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
                const instance = VisuObjectInstance.CreateFromTemplate(this.visuTemplatesMap.get(x.RuleTemplate.This2DefaultMobileVisuTemplate));

                instance.RuleInstance = x;
                visuObjectInstances.push(instance);

              }

              this.page.VisuObjectInstances = visuObjectInstances;
            } else if (data instanceof VisuPage) {
              this.page = data;
            }

            this.initGridster();
            this.appService.isLoading = false;
          });
        }

      }


      if (this.page) {
        super.registerEvent(this.page.heightWidthChange, () => {
          this.initGridster();
        });
        this.initGridster();
      }
    } catch (error) {
      this.notifyService.notifyError(error);
      console.error(error);
    }

    this.appService.isLoading = false;
  }

  private getAndSetProperty(instance: VisuObjectInstance, property: string, value: any) {
    const prop = instance.getProperty(property);
    if (prop) {
      prop.Value = value;
    }
  }

  initGridster() {

    if (!this.page) {
      console.error("cannot init grid because no page has been loaded so far");
      return;
    }
    this.options = {
      gridType: "fit",
      mobileBreakpoint: 0,
      itemChangeCallback: this.itemChange,
      itemResizeCallback: this.itemResize,
      maxRows: this.page.Height,
      maxCols: this.page.Width,
      minRows: this.page.Height,
      minCols: this.page.Width,
      enableEmptyCellDrop: true,
      resizable: {
        enabled: true
      },
      displayGrid: "always",
      draggable: {
        enabled: true
      },
      swap: true,
      pushResizeItems: true
    };

    if (!this.route.snapshot.data.editable) {
      this.options.resizable.enabled = false;
      this.options.draggable.enabled = false;
      this.selectionEnabled = false;
      this.options.displayGrid = "none";
    }
    if (this.gridster) {
      this.gridster.resize();
    }
  }

  changedOptions() {
    this.options.api.optionsChanged();
  }

  removeItem(item) {
    this.page.VisuObjectInstances.splice(this.page.VisuObjectInstances.indexOf(item), 1);
  }

  elementDroped($event) {
    const instance = VisuObjectMobileInstance.CreateFromTemplate($event.dragData);
    const pos = this.gridster.getFirstPossiblePosition(instance);

    instance.x = pos.x;
    instance.y = pos.y;
    instance.Height = instance.VisuObjectTemplate.Height;
    instance.Width = instance.VisuObjectTemplate.Width;

    this.page.VisuObjectInstances.push(instance);

    this.onMouseDown(void 0, instance);
  }


  onMouseDown($event, item) {
    if (!this.selectionEnabled) {
      return;
    }
    if (this.selectedItem) {
      this.selectedItem.isSelected = false;
    }
    this.selectedItem = item;
    this.selectedItem.isSelected = true;
  }

  async logoutClicked($event) {
    await this.login.logout();
    this.router.navigateByUrl("/login");
  }

  sidebarToggle($event) {
    $event.preventDefault();
    document.querySelector("body").classList.toggle("sidebar-hidden");
  }

  ngOnDestroy() {
    super.baseOnDestroy();
  }
}
