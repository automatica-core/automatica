import { Component, OnInit, OnDestroy } from "@angular/core";
import { PropertyInstance } from "../../../model/property-instance";
import { DataHubService } from "../../../communication/hubs/data-hub.service";
import { TranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { BaseMobileComponent } from "../../base-mobile-component";
import { ConfigService } from "src/app/services/config.service";
import { NodeDataTypeEnum } from "src/app/base/model/node-data-type";
import * as moment from "moment";

@Component({
  selector: "visu-label",
  templateUrl: "./label.component.html",
  styleUrls: ["./label.component.scss"]
})
export class LabelComponent extends BaseMobileComponent implements OnInit, OnDestroy {


  public get displayValue() {
    if (this.value === void 0) {
      return void 0;
    }

    if (this.nodeInstanceModel && this.nodeInstanceModel.NodeTemplate && this.nodeInstanceModel.NodeTemplate.NodeType) {
      switch (this.nodeInstanceModel.NodeTemplate.NodeType.Type) {
        case NodeDataTypeEnum.Date:
          return moment(this.value).format(this.translate.translate("COMMON.DATETIMEFORMAT.DATE"));
        case NodeDataTypeEnum.DateTime:
          return moment(this.value).format(this.translate.translate("COMMON.DATETIMEFORMAT.DATETIME"));
        case NodeDataTypeEnum.Time:
          return moment(this.value, ["HH:mm:ss.SSS"]).format(this.translate.translate("COMMON.DATETIMEFORMAT.TIME"));

        default:
          return this.value;
      }
    }
    return this.value;
  }

  constructor(dataHub: DataHubService, notify: NotifyService, translate: TranslationService, configService: ConfigService) {
    super(dataHub, notify, translate, configService);
  }

  async ngOnInit() {
    super.baseOnInit();

    super.propertyChanged();

  }
  public onItemResized() {

  }


  async ngOnDestroy() {
    super.baseOnDestroy();
  }

}
