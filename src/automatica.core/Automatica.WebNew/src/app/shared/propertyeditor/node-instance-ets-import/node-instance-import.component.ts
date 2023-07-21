import { Component, OnInit, OnDestroy, ViewChild, Input, Output, EventEmitter } from "@angular/core";
import { L10nTranslationService } from "angular-l10n";
import { DxTreeListComponent } from "devextreme-angular";
import { NotifyService } from "src/app/services/notify.service";
import { BaseComponent } from "src/app/base/base-component";
import { AreaInstance } from "src/app/base/model/areas";
import { AppService } from "src/app/services/app.service";
import { PropertyInstance } from "src/app/base/model/property-instance";
import { NodeInstance } from "src/app/base/model/node-instance";
import { IPropertyModel } from "src/app/base/model/interfaces";

@Component({
  selector: "node-instance-import",
  templateUrl: "./node-instance-import.component.html",
  styleUrls: ["./node-instance-import.component.scss"]
})
export class NodeInstanceImportComponent extends BaseComponent implements OnDestroy {

  parentObjId: string;

  instances: AreaInstance[] = [];

  @ViewChild("tree")
  dxTree: DxTreeListComponent;

  password: string;
  public isVisible: boolean = false;

  propertyInstance: PropertyInstance;
  nodeInstance: IPropertyModel;

  @Output()
  public fileUploaded = new EventEmitter<any>();

  get uploadHeader() {
    return { "Authorization": "Bearer " + localStorage.getItem("jwt"), password: this.password };
  }

  constructor(
    translationService: L10nTranslationService,
    private notify: NotifyService,
    appService: AppService) {
    super(notify, translationService, appService);
  }

  onHiding($event) {
    this.isVisible = false;
  }

  onUploadStarted($event) {
    this.isLoading = true;
  }

  async onFileUploaded($event) {

    if (this.propertyInstance.PropertyTemplate.Meta === "OBJECT_SAVED" && this.nodeInstance.isNewObject) {
      this.notify.notifyWarning("COMMON.SAVE_BEFORE_CONTINUE", 5000);
      return;
    }
    if (this.nodeInstance instanceof NodeInstance) {
      await this.fileUploaded.emit({ item: this.nodeInstance, file: $event.file, password: this.password });
      this.isVisible = false;
    }
  }


  ngOnDestroy() {
    super.baseOnDestroy();
  }

}
