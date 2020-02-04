import { Component, OnInit, Input, Type, ViewEncapsulation, HostBinding, OnDestroy, ElementRef } from "@angular/core";
import { LabelComponent } from "./label/label.component";
import { DefaultComponent } from "./default/default.component";
import { LinkComponent } from "./link/link.component";
import { ToggleComponent } from "./buttons/toggle/toggle.component";
import { TranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { BaseComponent } from "../../base-component";
import { VisuObjectMobileInstance } from "../../model/visu/visu-object-mobile-instance";
import { BaseMobileComponent } from "../base-mobile-component";
import { AppService } from "src/app/services/app.service";
import { VisuPageGroupType } from "../../model/visu-page";

@Component({
  selector: "visu-component",
  templateUrl: "./control.component.html",
  styleUrls: ["./control.component.scss"],
  encapsulation: ViewEncapsulation.None
})
export class ControlComponent extends BaseComponent implements OnInit, OnDestroy {



  @HostBinding("class.mobile-control") true;

  @Input()
  item: VisuObjectMobileInstance;

  @Input()
  editMode: boolean = false;

  @Input()
  pageGroupType: VisuPageGroupType = VisuPageGroupType.Favorites;

  inputs: any;

  outputs: any;

  component: Type<BaseMobileComponent>;

  constructor(
    private elementRef: ElementRef,
    notify: NotifyService,
    translate: TranslationService,
    appService: AppService) {
    super(notify, translate, appService);
  }

  ngOnInit() {
    super.baseOnInit();

    this.inputs = {
      item: this.item,
      parent: this.elementRef,
      editMode: this.editMode,
      pageGroupType: this.pageGroupType
    };

    switch (this.item.VisuObjectTemplate.Key) {
      case "label": {
        this.component = LabelComponent;
        break;
      }
      case "link": {
        this.component = LinkComponent;
        break;
      }
      case "toggle-button": {
        this.component = ToggleComponent;
        break;
      }
      default: {
        this.component = DefaultComponent;
        break;
      }
    }
  }

  ngOnDestroy(): void {
    super.baseOnDestroy();
  }

}
