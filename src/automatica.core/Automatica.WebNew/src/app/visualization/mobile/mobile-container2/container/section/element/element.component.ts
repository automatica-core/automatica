import { Component, OnInit, Input } from "@angular/core";
import { VisuObjectMobileInstance } from "src/app/base/model/visu";

@Component({
  selector: "app-visualization-element",
  templateUrl: "./element.component.html",
  styleUrls: ["./element.component.scss"]
})
export class ElementComponent implements OnInit {

  @Input()
  item: VisuObjectMobileInstance;

  constructor() { }

  ngOnInit() {
  }

}
