import { Component, OnInit, ViewChild, ElementRef } from "@angular/core";
import { PropertyInstance } from "../../../model/property-instance";
import { DataHubService } from "../../../communication/hubs/data-hub.service";
import { TranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { BaseMobileComponent } from "../../base-mobile-component";
import { ConfigService } from "src/app/services/config.service";

@Component({
  selector: "visu-clock",
  templateUrl: "./clock.component.html",
  styleUrls: ["./clock.component.scss"]
})
export class ClockComponent extends BaseMobileComponent implements OnInit {

  @ViewChild("second", { static: true }) second: ElementRef;
  @ViewChild("minute", { static: true }) minute: ElementRef;
  @ViewChild("hour", { static: true }) hour: ElementRef;

  constructor(dataHub: DataHubService, notify: NotifyService, translate: TranslationService, configService: ConfigService) {
    super(dataHub, notify, translate, configService);
  }

  public onItemResized() {

  }

  ngOnInit() {

    console.log(this.second.nativeElement.css);

    const currentSec = this.getSecondsToday();

    const seconds = (currentSec / 60) % 1;
    const minutes = (currentSec / 3600) % 1;
    const hours = (currentSec / 43200) % 1;

    this.setTime(60 * seconds, this.second);
    this.setTime(3600 * minutes, this.minute);
    this.setTime(43200 * hours, this.hour);
  }

  // Javascript is used to set the clock to your computer time.


  setTime(left, hand: ElementRef) {
    hand.nativeElement.style["animation-delay"] = left * -1 + "s";
    // $(".clock__" + hand).css("animation-delay", "" + left * -1 + "s");
  }

  getSecondsToday() {
    const now = new Date();
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());

    const diff = <any>now - <any>today;
    const round = Math.round(diff / 1000);
    console.log(round)
    return round;
  }


}
