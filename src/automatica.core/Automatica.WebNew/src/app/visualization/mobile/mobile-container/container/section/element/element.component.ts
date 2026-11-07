import { Component, OnInit, Input, ChangeDetectionStrategy, HostBinding } from "@angular/core";
import { VisuObjectMobileInstance } from "src/app/base/model/visu";
import { VisuPageGroupType } from "src/app/base/model/visu-page";

@Component({
    selector: "app-visualization-element",
    templateUrl: "./element.component.html",
    styleUrls: ["./element.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class ElementComponent implements OnInit {

  @Input()
  item: VisuObjectMobileInstance;

  @Input()
  pageGroupType: VisuPageGroupType = VisuPageGroupType.Favorites;

  /** camera pictures need the room of two tiles */
  @HostBinding("class.is-wide")
  get isWide(): boolean {
    return this.item?.VisuObjectTemplate?.Key === "camera";
  }

  constructor() { }

  ngOnInit() {
  }

}
