import { Component, OnInit, Input } from "@angular/core";
import { VisuObjectMobileInstance } from "src/app/base/model/visu";

interface Section {
  title: string;

  items: VisuObjectMobileInstance[];
}

@Component({
  selector: "app-visualization-container",
  templateUrl: "./container.component.html",
  styleUrls: ["./container.component.scss"]
})
export class ContainerComponent implements OnInit {

  private _items: VisuObjectMobileInstance[];

  @Input()
  public get items(): VisuObjectMobileInstance[] {
    return this._items;
  }
  public set items(v: VisuObjectMobileInstance[]) {
    this._items = v;

    this.sectionsMap = new Map<string, Section>();
    this.sections = [];

    for (const item of v) {
      let sectionName = "unknown";
      sectionName = item.objectType.This2CategoryInstanceNavigation.Name;

      if (!this.sectionsMap.has(sectionName)) {
        const section: Section = { title: sectionName, items: [] };

        this.sectionsMap.set(sectionName, section);
        this.sections.push(section);
      }

      const existingSection = this.sectionsMap.get(sectionName);
      existingSection.items.push(item);
    }

  }

  private sectionsMap: Map<string, Section>;

  sections: Section[] = [];

  constructor() { }

  ngOnInit() {

    console.log(this.sections);

  }

}
