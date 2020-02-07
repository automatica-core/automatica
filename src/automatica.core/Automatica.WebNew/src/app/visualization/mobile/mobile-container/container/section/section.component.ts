import { Component, OnInit, Input } from "@angular/core";
import { VisuObjectMobileInstance } from "src/app/base/model/visu";
import { VisuPageGroupType } from "src/app/base/model/visu-page";

@Component({
  selector: "app-visualization-section",
  templateUrl: "./section.component.html",
  styleUrls: ["./section.component.scss"]
})
export class SectionComponent implements OnInit {

  @Input()
  title: string;

  @Input()
  items: VisuObjectMobileInstance[] = [];

  @Input()
  pageGroupType: VisuPageGroupType = VisuPageGroupType.Favorites;

  constructor() { }

  ngOnInit() {
  }

}
