import { Component, OnInit } from "@angular/core";
import { CategoryService } from "src/app/services/categories.service";
import { L10nTranslationService } from "angular-l10n";
import { Guid } from "src/app/base/utils/Guid";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { CategoryGroup, CategoryInstance } from "src/app/base/model/categories";
import { CustomMenuItem } from "src/app/base/model/custom-menu-item";

@Component({
  selector: "app-category-config",
  templateUrl: "./category-config.component.html",
  styleUrls: ["./category-config.component.scss"]
})
export class CategoryConfigComponent extends BaseComponent implements OnInit {
  groups: CategoryGroup[] = [];
  categories: CategoryInstance[] = [];
  selectedNode: CategoryInstance = void 0;


  menuItems: CustomMenuItem[] = [];

  menuSave: CustomMenuItem = {
    id: "save",
    label: "Save",
    icon: "fa-save",
    items: undefined,
    command: (event) => { this.save(); }
  }

  constructor(
    private catService: CategoryService,
    translate: L10nTranslationService,
    private notify: NotifyService,
    appService: AppService) {
    super(notify, translate, appService);
    appService.setAppTitle("CATEGORIES.NAME");


    this.menuItems.push(this.menuSave);

    this.translate.onChange().subscribe({
      next: () => {
        this.menuSave.label = this.translate.translate("COMMON.SAVE");
      }
    });

  }

  async ngOnInit() {
    this.appService.isLoading = true;

    try {
      const [groups, categories] = await Promise.all(
        [
          this.catService.getCategoryGroups(),
          this.catService.getCategoryInstances()
        ]);
      this.groups = groups;
      this.categories = categories;

    } catch (error) {
      this.handleError(error);
    }

    this.appService.isLoading = false;
  }
  save(): any {
    this.appService.isLoading = true;

    try {
      this.catService.saveAreaInstance(this.categories);
      this.notify.notifySuccess("COMMON.SAVED");
    } catch (error) {
      super.handleError(error);
    }

    this.appService.isLoading = false;
  }

  colorBoxOnValueChanged($event, cell) {
    cell.setValue($event.value);
  }
  onRowRemoving($event) {
    const data: CategoryInstance = $event.data;

    if (!data.IsDeleteable) {
      $event.cancel = true;
    }
  }

  itemClick($event) {
    const item: CustomMenuItem = $event.itemData;

    if (item && item.command) {
      item.command($event);
    }
  }

  onRowUpdating($event) {
    const oldData: CategoryInstance = $event.oldData;

    if ($event.newData.DisplayName || $event.newData.DisplayDescription) {
      $event.newData.DisplayName = this.translate.translate(oldData.Name);
      $event.newData.DisplayDescription = ""; // oldData.Description;
    }
  }

  onInitNewRow($event) {
    const newObject = new CategoryInstance();
    newObject.ObjId = Guid.MakeNew().ToString();
    newObject.addVirtualProperties();

    $event.data = newObject;

  }

  onRowInserting($event) {
    const newObject = new CategoryInstance();
    newObject.addVirtualProperties();

    Object.assign(newObject, $event.data);
    $event.data = newObject;
  }

  onRowClicked($event) {
    this.selectedNode = $event.data;
  }

  onInitialized($event) {
    $event.component.columnOption("command:edit", "width", 150);
  }

  onCellPrepared(e) {
    if (e.rowType === 'data' && e.column.command === 'edit')
      if (e.row.data != null && !e.row.data.IsDeleteable) {
        let editLink = e.cellElement.querySelector(".dx-link-delete");
        editLink?.remove();
      }
  }
}
