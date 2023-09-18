import { Component, OnInit, Input } from "@angular/core";
import { L10nTranslationService } from "angular-l10n";
import { BaseComponent } from "src/app/base/base-component";
import { VisuObjectMobileInstance } from "src/app/base/model/visu";
import { VisuPage, VisuPageGroupType } from "src/app/base/model/visu-page";
import { AppService } from "src/app/services/app.service";
import { NotifyService } from "src/app/services/notify.service";
import { VisuService } from "src/app/services/visu.service";

@Component({
  selector: "app-visualization-section",
  templateUrl: "./section.component.html",
  styleUrls: ["./section.component.scss"]
})
export class SectionComponent extends BaseComponent implements OnInit {

  @Input()
  title: string;

  @Input()
  items: VisuObjectMobileInstance[] = [];

  @Input()
  page: VisuPage;

  @Input()
  pageGroupType: VisuPageGroupType = VisuPageGroupType.Favorites;

  constructor(
    private visuService: VisuService,
    notify: NotifyService,
    translationService: L10nTranslationService,
    appService: AppService) {
    super(notify, translationService, appService);
  }

  ngOnInit() {
    this.appService.setAppTitle(this.title);
  }

  async onRefresh($event) {
    if (this.pageGroupType == VisuPageGroupType.Favorites) {
      this.visuService.reloadedPage.emit(await this.visuService.getFavorites());
    }
    else {
      this.visuService.reloadPage(this.page);
    }
    
    

  }

}
