import { Component, OnInit, OnDestroy, ViewChild } from "@angular/core";
import { TranslationService } from "angular-l10n";
import { Router, ActivatedRoute } from "@angular/router";
import { DxTreeListComponent } from "devextreme-angular";
import { NotifyService } from "src/app/services/notify.service";
import { AreaService } from "src/app/services/areas.service";
import { BaseComponent } from "src/app/base/base-component";
import { AreaInstance } from "src/app/base/model/areas";
import { BaseModel } from "src/app/base/model/base-model";

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


  private mapList: Map<any, AreaInstance> = new Map<any, AreaInstance>();


  private _selectedRowKeys: string[];
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
    return { "Authorization": "Bearer " + localStorage.getItem("jwt") };
  }

  constructor(
    private translationService: TranslationService,
    private router: Router,
    private notify: NotifyService,
    private activatedRoute: ActivatedRoute,
    private areaService: AreaService) {
    super(notify, translationService);
  }

  ngOnInit() {

    super.registerObservable(this.activatedRoute.params, (params) => {
      this.parentObjId = params.id;
    });

  }

  onHiding($event) {
    this.router.navigate(["."], { relativeTo: this.activatedRoute.parent });
  }

  onFileUploaded($event) {

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

      const tmpConfig = [];
      for (const node of list) {
        this.addInstancesRec(node, tmpConfig);

        tmpConfig.push(node);
      }
      tmpConfig.sort((a, b) => (a.Name.localeCompare(b.Name) - (b.Name.localeCompare(a.Name))));

      this.instances = tmpConfig;
    }
  }


  addInstancesRec(instance: AreaInstance, tmpConfig: AreaInstance[]): any {
    for (const x of instance.InverseThis2ParentNavigation) {
      tmpConfig.push(x);
      this.mapList.set(x.ObjId, x);

      this.addInstancesRec(x, tmpConfig);
    }
  }

  addParents(instance: AreaInstance, array: AreaInstance[]) {
    if (!instance || instance.ObjId === this.parentObjId || instance.This2ParentNavigation.ObjId === this.parentObjId) {
      return;
    }

    if (!array.find(a => a.ObjId === instance.This2ParentNavigation.ObjId)) {
      array.push(instance.This2ParentNavigation);
    }
    this.addParents(instance.This2ParentNavigation, array);
  }

  selectRecurisve(childs: AreaInstance[], selected: string[]) {
    for (const x of childs) {

      if (!selected.find(a => a === x.ObjId)) {
        selected.push(x.ObjId);
      }

      this.selectRecurisve(x.InverseThis2ParentNavigation, selected);
    }
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
    const areaInstances: AreaInstance[] = this.instances;
    const toSave: AreaInstance[] = [];
    const selected = [...this.selectedRowKeys];

    for (const x of this.selectedRowKeys) {
      if (x === this.parentObjId) {
        const childs = this.instances.filter(a => a.This2Parent === this.parentObjId);
        this.selectRecurisve(childs, selected);
      } else {
        const childs = this.mapList.get(x).InverseThis2ParentNavigation;
        this.selectRecurisve(childs, selected);
      }
    }

    for (const x of areaInstances) {
      if (selected.find(a => x.ObjId === a)) {

        if (!toSave.find(a => a.ObjId === x.ObjId)) {
          toSave.push(x);
        }
        this.addParents(x, toSave);
      }
    }

    const fixed = this.removeDuplicates(toSave).filter(a => a.ObjId !== this.parentObjId);

    try {
      const data = await this.areaService.addAreaInstances(fixed);

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
