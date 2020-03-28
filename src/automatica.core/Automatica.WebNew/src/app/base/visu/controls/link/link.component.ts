import { Component, OnInit, OnDestroy } from "@angular/core";
import { Router, ActivatedRoute } from "@angular/router";
import { PropertyInstance } from "../../../model/property-instance";
import { DataHubService } from "../../../communication/hubs/data-hub.service";
import { L10nTranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { BaseMobileComponent } from "../../base-mobile-component";
import { VisuService } from "src/app/services/visu.service";
import { VisuPage } from "src/app/base/model/visu-page";
import { ConfigService } from "src/app/services/config.service";
import { AppService } from "src/app/services/app.service";

@Component({
  // tslint:disable-next-line:component-selector
  selector: "visu-link",
  templateUrl: "./link.component.html",
  styleUrls: ["./link.component.scss"]
})
export class LinkComponent extends BaseMobileComponent implements OnInit, OnDestroy {

  value: string;

  private _linkProperty: PropertyInstance;
  private _areaLinkProperty: PropertyInstance;
  private _textProperty: PropertyInstance;

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    dataHub: DataHubService,
    notify: NotifyService,
    translate: L10nTranslationService,
    private visuService: VisuService,
    configService: ConfigService,
    appService: AppService) {
    super(dataHub, notify, translate, configService, appService);
  }

  public onItemResized() {

  }

  async ngOnInit() {
    super.baseOnInit();
    this.isLoading = true;
    const prop = this.getProperty("link");
    const text = super.getProperty("text");

    this._linkProperty = prop;
    this._textProperty = text;

    this._areaLinkProperty = this.getProperty("area_link");

    super.registerEvent(prop.propertyChanged, (value) => {
      this.setTextValue();
    });

    super.registerEvent(this._areaLinkProperty.propertyChanged, (value) => {
      this.setTextValue();
    });

    super.registerEvent(text.propertyChanged, (value) => {
      this.setTextValue();
    });


    if (text && text.Value) {
      this.value = text.Value;
    } else if (prop.ValueVisuPage) {
      try {
        const visuPage = <VisuPage>await this.visuService.getVisuPage(prop.ValueVisuPage);
        this.value = visuPage.Name;
      } catch (error) {
        super.handleError(error);
      }
    } else if (this._areaLinkProperty.ValueAreaInstance) {
      this.value = this._areaLinkProperty.AreaInstance.DisplayName;
    } else {
      this.value = "NO LINK";
    }
    this.isLoading = false;
  }

  private setTextValue() {
    if (this._linkProperty.VisuPage) {
      if (!this._textProperty.Value) {
        this.value = this._linkProperty.VisuPage.Name;
      } else {
        this.value = this._textProperty.Value;
      }
    } else if (this._areaLinkProperty.ValueAreaInstance) {
      this.value = this._areaLinkProperty.AreaInstance.DisplayName;
    } else {
      this.value = this._textProperty.Value;
    }
  }

  ngOnDestroy() {
    super.baseOnDestroy();
  }

  linkClicked($event) {
    if (this.editMode) {
      return;
    }

    const prop = this.getProperty("link");
    const areaLink = this.getProperty("area_link");

    if (prop.ValueVisuPage) {
      this.router.navigate([prop.Value], { relativeTo: this.route.parent });
    } else if (areaLink.ValueAreaInstance) {
      this.router.navigate([areaLink.Value], { relativeTo: this.route.parent });
    }
  }

}
