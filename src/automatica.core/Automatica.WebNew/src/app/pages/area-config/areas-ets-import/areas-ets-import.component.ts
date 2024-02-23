import { Component, OnInit, OnDestroy, ViewChild } from "@angular/core";
import { L10nTranslationService } from "angular-l10n";
import { Router, ActivatedRoute } from "@angular/router";
import { DxTreeListComponent } from "devextreme-angular";
import { NotifyService } from "src/app/services/notify.service";
import { AreaService } from "src/app/services/areas.service";
import { BaseComponent } from "src/app/base/base-component";
import { AreaInstance } from "src/app/base/model/areas";
import { BaseModel } from "src/app/base/model/base-model";
import { AppService } from "src/app/services/app.service";

@Component({
  selector: "app-areas-ets-import",
  templateUrl: "./areas-ets-import.component.html",
  styleUrls: ["./areas-ets-import.component.scss"]
})
export class AreasEtsImportComponent extends BaseComponent implements OnInit, OnDestroy {

  parentObjId: string;

  instances: AreaInstance[] = [];

  @ViewChild("tree")
  dxTree: DxTreeListComponent;

  password: string;


  private mapList: Map<any, AreaInstance> = new Map<any, AreaInstance>();


  private _selectedRowKeys: string[] = [];
  public get selectedRowKeys(): string[] {
    return this._selectedRowKeys;
  }
  public set selectedRowKeys(v: string[]) {
    this._selectedRowKeys = v;
  }


  get uploadUrl(): string {
    return "/webapi/areas/" + this.parentObjId + "/etsImport";
  }

  get uploadHeader() {
    return { "Authorization": "Bearer " + localStorage.getItem("jwt"), password: this.password };
  }

  constructor(
    private translationService: L10nTranslationService,
    private router: Router,
    private notify: NotifyService,
    private activatedRoute: ActivatedRoute,
    private areaService: AreaService,
    appService: AppService) {
    super(notify, translationService, appService);
  }

  ngOnInit() {

    super.registerObservable(this.activatedRoute.params, (params) => {
      this.parentObjId = params.id;
    });

  }

  onHiding($event) {
    this.router.navigate(["."], { relativeTo: this.activatedRoute.parent });
  }

  onUploadStarted($event) {
    this.isLoading = true;
  }

  onFileUploaded($event) {

    this.isLoading = false;
    if ($event.request.status === 200) {
      const json = JSON.parse($event.request.responseText);

      const list = [];

      for (const node of json) {
        const model = BaseModel.getBaseModelFromJson<AreaInstance>(node, void 0, this.translationService);
        if (model) {
          list.push(model);
          this.mapList.set(node.ObjId, node);
        }
      }

      const tmpConfig = [...list];
      tmpConfig.sort((a, b) => (a.Name.localeCompare(b.Name) - (b.Name.localeCompare(a.Name))));

      this.instances = tmpConfig;
      this.dxTree.instance.refresh();
    }
  }


  selectRecurisve(childs: AreaInstance[]): AreaInstance[] {
    let ret = [];
    for (const child of childs) {
      var retChild = this.selectRecurisve(this.instances.filter(a => a.This2Parent === child.ObjId));

      ret.push(child);
      ret.push(...retChild)
    }
    return ret;
  }

  removeDuplicates(arr) {
    const unique_array = []
    for (let i = 0; i < arr.length; i++) {
      if (!unique_array.find(a => a.ObjId === arr[i].ObjId)) {
        unique_array.push(arr[i])
      }
    }
    return unique_array
  }

  async saveClick($event) {

    let selectedItems = this.selectRecurisve(this.instances.filter(a => this.selectedRowKeys.includes(a.ObjId)));
    selectedItems = this.removeDuplicates(selectedItems);

    let root = selectedItems.filter(a => a.This2Parent === null);

    root[0].This2Parent = this.parentObjId;

    try {
      const data = await this.areaService.addAreaInstances(selectedItems);

      this.areaService.etsImported.emit(data);
      this.onHiding($event);

    } catch (error) {
      this.notify.notifyError(error);
    }

  }

  ngOnDestroy() {
    super.baseOnDestroy();
  }

}
