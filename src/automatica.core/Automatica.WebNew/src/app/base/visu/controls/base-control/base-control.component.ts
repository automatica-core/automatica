import { Component, OnInit, Input, Output, EventEmitter } from "@angular/core";
import { AggregatedValueRecord } from "src/app/base/model/aggregated-record-value";

@Component({
  selector: "app-visualization-control",
  templateUrl: "./base-control.component.html",
  styleUrls: ["./base-control.component.scss"]
})
export class BaseControlComponent implements OnInit {

  @Input()
  width: number = 1;

  @Input()
  section_cell_class: string = "section_cell";

  @Input()
  icon: string = "question";

  @Input()
  iconColor: string = "white";

  @Input()
  location: string;

  @Input()
  hint: string;

  @Input()
  value: string;

  @Input()
  subValue: string;

  @Input()
  hasPopup: boolean = false;

  @Input()
  textColor: string;

  @Input()
  showIcon: boolean = true;

  @Input()
  useFullContainer: boolean = false;

  @Input()
  aggregatedValues: AggregatedValueRecord[] = undefined;

  @Input()
  showIconInPopup: boolean = true;

  @Input()
  showLocationInPopup: boolean = true;

  @Output()
  onPopupShowing: EventEmitter<any> = new EventEmitter<any>();

  @Output()
  onPopupHiding: EventEmitter<any> = new EventEmitter<any>();

  popupVisible = false;

  public get valueHidden(): boolean {
    return this.value === void 0 || this.value === null;
  }


  constructor() { }

  ngOnInit() {
  }

  preventDefault($event) {
    $event.stopPropagation()
  }

  onCellClick($event) {
    this.popupVisible = true;
  }

  async onPopupHidingCall($event) {
    await this.onPopupHiding.emit($event);
    this.popupVisible = false;
  }
  async onPopupShowingCall($event) {
    await this.onPopupShowing.emit($event);
  }

}
