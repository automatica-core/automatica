import { Component, OnInit } from "@angular/core";
import { HubConnectionService } from "../base/communication/hubs/hub-connection.service";
import { AreaService } from "../services/areas.service";
import { VisuService } from "../services/visu.service";
import { CategoryService } from "../services/categories.service";
import { AppService } from "../services/app.service";
import { ServerStateService } from "../services/server-state.service";
import { AreaInstance } from "../base/model/areas";
import { VisuPage } from "../base/model/visu-page";
import { CategoryInstance } from "../base/model/categories";
import { DataService } from "../services/data.service";

@Component({
  selector: "app-visualization",
  templateUrl: "./visualization.component.html",
  styleUrls: ["./visualization.component.scss"]
})
export class VisualizationComponent implements OnInit {

  menuItems = [];
  areas: AreaInstance[] = [];
  categories: CategoryInstance[] = [];
  values: any;

  constructor(private hubService: HubConnectionService,
    private areaService: AreaService,
    private catService: CategoryService,
    appService: AppService,
    private serverStateService: ServerStateService) {
    appService.setAppTitle("Automatica.Core");
  }

  async ngOnInit() {

    if (!await this.serverStateService.isStarted()) {
      this.serverStateService.init(false);
    }

    this.serverStateService.start();
    await this.hubService.init();

    const [areas, categories] = await Promise.all(
      [
        this.areaService.getAreaInstances(),
        this.catService.getCategoryInstances()
      ]);

    this.areas = areas;
    this.categories = categories;


    const menu = [];
    menu.push({
      text: "COMMON.HOME",
      icon: "home",
      path: "/visualization/page/home",
    });

    const categoriesItem = {
      text: "CATEGORIES.NAME",
      icon: "th",
      seperator: false,
      items: []
    };

    for (const x of this.categories) {
      categoriesItem.items.push({
        text: x.DisplayName,
        path: "/visualization/page/category/" + x.ObjId,
        icon: x.Icon.replace("fas", "").trim()
      });
    }
    menu.push(categoriesItem);

    const areasItem = {
      text: "AREAS.NAME",
      icon: "layer-group",
      items: []
    };

    this.addAreaRecursive(areasItem, this.areas[0].InverseThis2ParentNavigation);

    menu.push(areasItem);

    this.menuItems = menu;
  }


  private addAreaRecursive(parent, areas: AreaInstance[]) {
    for (const x of areas) {
      const item = {
        text: x.DisplayName,
        path: "/visualization/page/area/" + x.ObjId,
        icon: x.Icon,
        expanded: true,
        items: []
      };
      parent.items.push(item);

      if (x.InverseThis2ParentNavigation) {
        this.addAreaRecursive(item, x.InverseThis2ParentNavigation);
      }
    }
  }

}
