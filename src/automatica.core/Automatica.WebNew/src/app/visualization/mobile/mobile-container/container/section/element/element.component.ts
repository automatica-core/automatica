import { Component, OnInit, Input, ChangeDetectionStrategy, HostBinding } from "@angular/core";
import { VisuObjectMobileInstance } from "src/app/base/model/visu";
import { VisuPageGroupType } from "src/app/base/model/visu-page";

const WIDE_TILES = new Set(["camera", "energy-flow"]);

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

  /** camera pictures and the energy flow need the room of two tiles */
  @HostBinding("class.is-wide")
  get isWide(): boolean {
    return WIDE_TILES.has(this.item?.VisuObjectTemplate?.Key);
  }

  constructor() { }

  ngOnInit() {
  }

}
