import { Component, OnInit, Input } from "@angular/core";

@Component({
  selector: "app-visualization-section",
  templateUrl: "./section.component.html",
  styleUrls: ["./section.component.scss"]
})
export class SectionComponent implements OnInit {

  @Input()
  title: string;

  @Input()
  items: any[] = [];

  constructor() { }

  ngOnInit() {
  }

}
