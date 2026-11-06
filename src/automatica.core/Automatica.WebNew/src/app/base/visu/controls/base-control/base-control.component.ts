import { Component, OnInit, OnDestroy, Input, Output, EventEmitter, ChangeDetectionStrategy, ElementRef, NgZone } from "@angular/core";
import { AggregatedValueRecord } from "src/app/base/model/aggregated-record-value";

@Component({
    selector: "app-visualization-control",
    templateUrl: "./base-control.component.html",
    styleUrls: ["./base-control.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class BaseControlComponent implements OnInit, OnDestroy {

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

  chartWidth = 180;
  chartHeight = 30;

  public get valueHidden(): boolean {
    return this.value === void 0 || this.value === null;
  }


  private resizeObserver: ResizeObserver;

  constructor(private elementRef: ElementRef<HTMLElement>, private zone: NgZone) { }

  ngOnInit() {
    let screenWidth = window.innerWidth;

    if(screenWidth < 400) {
      this.chartHeight = 20;
    }

    // the tile width follows the section grid column, the sparkline has to follow it
    this.resizeObserver = new ResizeObserver(entries => {
      const width = Math.floor(entries[0].contentRect.width);
      if (width > 0 && width !== this.chartWidth) {
        this.zone.run(() => this.chartWidth = width);
      }
    });
    this.resizeObserver.observe(this.elementRef.nativeElement);
  }

  ngOnDestroy() {
    this.resizeObserver?.disconnect();
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
