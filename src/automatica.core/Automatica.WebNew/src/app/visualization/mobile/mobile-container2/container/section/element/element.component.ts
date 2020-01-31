import { Component, OnInit, Input } from "@angular/core";

@Component({
  selector: "app-visualization-element",
  templateUrl: "./element.component.html",
  styleUrls: ["./element.component.scss"]
})
export class ElementComponent implements OnInit {

  @Input()
  item: any;

  constructor() { }

  ngOnInit() {
  }

}
