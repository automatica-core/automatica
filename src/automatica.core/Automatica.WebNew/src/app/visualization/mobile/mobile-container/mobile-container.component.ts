import { Component, OnInit, Output, EventEmitter, Input, ViewChild, OnDestroy } from "@angular/core";
import { GridsterConfig, GridsterComponent } from "ngx-gridster";
import { ActivatedRoute, Router } from "@angular/router";
import { TranslationService } from "angular-l10n";
import { ConfigService } from "../../../services/config.service";
import { LoginService } from "../../../services/login.service";
import { VisuObjectMobileInstance } from "src/app/base/model/visu";
import { NotifyService } from "src/app/services/notify.service";
import { VisuService } from "src/app/services/visu.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { VisuPage, VisuPageType } from "src/app/base/model/visu-page";
import { VisuObjectTemplate } from "src/app/base/model/visu-object-template";
import { VisuObjectInstance } from "src/app/base/model/visu-object-instance";
import { NodeDataTypeEnum } from "src/app/base/model/node-data-type";
import { VisualizationDataFacade } from "src/app/base/model/visualization-data-facade";
import { DeviceService, Orientation } from "src/app/services/device/device.service";
import { gridTypes } from "ngx-gridster/lib/gridsterConfig.interface";

@Component({
  selector: "mobile-container",
  templateUrl: "./mobile-container.component.html",
  styleUrls: ["./mobile-container.component.scss"]
})
export class MobileContainerComponent extends BaseComponent implements OnInit, OnDestroy {

  options: GridsterConfig;
  private _selectedItem: VisuObjectMobileInstance;
  orientationSub: any;

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

  @ViewChild("gridster", { static: false })
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
    appService: AppService,
    private deviceService: DeviceService) {

    super(notify, translate, appService);
  }

  itemChange() {
    // console.log("itemChanged", item, itemComponent);
  }

  itemResize(item: VisuObjectMobileInstance) {
    // console.log("itemResized", item, itemComponent);
    if (item.itemResized) {
      item.itemResized.emit();
    }
  }

  async ngOnInit() {

    this.registerEvent(this.deviceService.orientationChange, () => {
      this.initGridster();
    });


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

            if (data instanceof VisualizationDataFacade) {
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

    let maxRows = this.page.Height;
    let maxColumns = this.page.Width;
    let minRows = this.page.Height;
    let minColumns = this.page.Width;
    let keepFixedHeightInMobile = false;
    let keepFixedWidthInMobile = false;
    let fixedSize = (window.innerWidth / 2) - 35;

    const gridType: gridTypes = "fitToGridOptions";

    if (this.deviceService.isMobile()) {
      keepFixedHeightInMobile = true;
      keepFixedWidthInMobile = true;

      if (this.deviceService.orientation === Orientation.Portrait) {
        maxRows = this.page.Width;
        minRows = this.page.Width;

        maxColumns = this.page.Height;
        minColumns = this.page.Width;
      } else {
        maxRows = this.page.Height;
        minRows = this.page.Height;

        maxColumns = this.page.Width;
        minColumns = this.page.Width;

        fixedSize = (window.innerHeight / 2) - 65;
      }
    }

    console.log("height:", maxRows, "width", maxColumns);

    if (!this.page) {
      console.error("cannot init grid because no page has been loaded so far");
      return;
    }

    this.options = {
      gridType: gridType,
      mobileBreakpoint: 0,
      itemChangeCallback: this.itemChange,
      itemResizeCallback: this.itemResize,
      keepFixedHeightInMobile: keepFixedHeightInMobile,
      keepFixedWidthInMobile: keepFixedWidthInMobile,
      fixedColWidth: fixedSize,
      fixedRowHeight: fixedSize,
      maxRows: maxRows,
      maxCols: maxColumns,
      minRows: minRows,
      minCols: minColumns,
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
      this.options.api.optionsChanged();
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

  async logoutClicked() {
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
