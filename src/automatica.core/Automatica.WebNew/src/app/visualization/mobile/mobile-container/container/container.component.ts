import { Component, OnInit, Input } from "@angular/core";
import { VisuObjectMobileInstance } from "src/app/base/model/visu";
import { VisuPageGroupType } from "src/app/base/model/visu-page";
import { TranslationService } from "angular-l10n";

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
  public pageGroupType: VisuPageGroupType = VisuPageGroupType.Favorites;

  @Input()
  public get items(): VisuObjectMobileInstance[] {
    return this._items;
  }
  public set items(v: VisuObjectMobileInstance[]) {
    this._items = v;

    this.sectionsMap = new Map<string, Section>();
    this.sections = [];

    if (!v) {
      return;
    }

    for (const item of v) {
      let sectionName = "unknown";
      if (this.pageGroupType === VisuPageGroupType.Favorites) {
        sectionName = this.translationService.translate("COMMON.FAVORITES");
      } else if (this.pageGroupType === VisuPageGroupType.Category) {
        sectionName = item.objectType.This2CategoryInstanceNavigation.Name;
      } else if (this.pageGroupType === VisuPageGroupType.Area) {
        sectionName = item.objectType.This2AreaInstanceNavigation.Name;
      }

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

  constructor(private translationService: TranslationService) { }

  ngOnInit() {

    console.log(this.sections);

  }

}
