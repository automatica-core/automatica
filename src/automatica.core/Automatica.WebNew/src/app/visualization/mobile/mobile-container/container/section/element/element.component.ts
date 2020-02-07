import { Component, OnInit, Input } from "@angular/core";
import { VisuObjectMobileInstance } from "src/app/base/model/visu";
import { VisuPageGroupType } from "src/app/base/model/visu-page";

@Component({
  selector: "app-visualization-element",
  templateUrl: "./element.component.html",
  styleUrls: ["./element.component.scss"]
})
export class ElementComponent implements OnInit {

  @Input()
  item: VisuObjectMobileInstance;

  @Input()
  pageGroupType: VisuPageGroupType = VisuPageGroupType.Favorites;

  constructor() { }

  ngOnInit() {
  }

}
