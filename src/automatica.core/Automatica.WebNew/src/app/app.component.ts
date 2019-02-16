import { Component, OnInit, ChangeDetectorRef } from "@angular/core";
import { library } from "@fortawesome/fontawesome-svg-core";
// import { fas } from "@fortawesome/free-solid-svg-icons";
import { fas } from "@fortawesome/pro-solid-svg-icons";
import { far } from "@fortawesome/free-regular-svg-icons";
import { AppService } from "./services/app.service";
import { NotifyService } from "./services/notify.service";
import { TranslationService } from "angular-l10n";
import { BaseComponent } from "./base/base-component";


@Component({
  selector: "app-root",
  templateUrl: "./app.component.html",
  styleUrls: ["./app.component.scss"]
})
export class AppComponent extends BaseComponent implements OnInit {
  title = "automatica-web";
  private _isLoading: string;

  constructor(private appService: AppService, notify: NotifyService, translate: TranslationService, private changeDet: ChangeDetectorRef) {
    super(notify, translate);
    library.add(fas, far);
  }

  ngOnInit() {
    super.registerEvent(this.appService.isLoadingChanged, (v) => {
      this.isLoading = v;
      this.changeDet.detectChanges();
    });
  }


  public get isLoading(): string {
    return this._isLoading;
  }
  public set isLoading(v: string) {
    this._isLoading = v;
  }

}
