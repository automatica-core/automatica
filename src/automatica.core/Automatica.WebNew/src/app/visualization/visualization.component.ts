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
    private dataService: DataService,
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


    var map = new Map<string, AreaInstance>();
    var menuMap = new Map<string, any>();
    var root = this.areas.filter(a => a.This2Parent == null)[0];

    this.areas.forEach(x => {
      map.set(x.ObjId, x);
      const item = {
        text: x.DisplayName,
        path: "/visualization/page/area/" + x.ObjId,
        icon: x.Icon,
        expanded: true,
        items: []
      };
      menuMap.set(x.ObjId, item);

      if(x.This2Parent == root.ObjId) {
        areasItem.items.push(item);
      }
    });

    for (const x of areas) {
      if (!x.This2Parent) {
        continue;
      }
      const parent = menuMap.get(x.This2Parent);
      parent.items.push(menuMap.get(x.ObjId));
    }
    
    menu.push(areasItem);

    this.menuItems = menu;
  }

}
