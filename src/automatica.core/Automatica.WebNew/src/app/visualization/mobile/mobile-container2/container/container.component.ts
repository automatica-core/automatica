import { Component, OnInit, Input } from "@angular/core";

interface Section {
  title: string;

  items: any[];
}

@Component({
  selector: "app-visualization-container",
  templateUrl: "./container.component.html",
  styleUrls: ["./container.component.scss"]
})
export class ContainerComponent implements OnInit {

  private _items: any[];

  @Input()
  public get items(): any[] {
    return this._items;
  }
  public set items(v: any[]) {
    this._items = v;

    this.sectionsMap = new Map<string, Section>();
    this.sections = [];

    for (const item of v) {
      const sectionName = item[this.sectionKey];
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

  @Input()
  sectionKey: string = "section";

  constructor() { }

  ngOnInit() {

    console.log(this.sections);

  }

}
