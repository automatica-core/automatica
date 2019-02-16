import { Component, OnInit, EventEmitter, Output } from "@angular/core";
import DataSource from "devextreme/data/data_source";
import ArrayStore from "devextreme/data/array_store";
import { VisuService } from "src/app/services/visu.service";
import { VisuPage } from "src/app/base/model/visu-page";

@Component({
  selector: "visu-page-list",
  templateUrl: "./visu-page-list.component.html",
  styleUrls: ["./visu-page-list.component.scss"]
})
export class VisuPageListComponent implements OnInit {

  pages: VisuPage[] = [];

  private _selectedItems: VisuPage[] = [];
  public get selectedItems(): VisuPage[] {
    return this._selectedItems;
  }
  public set selectedItems(v: VisuPage[]) {
    this._selectedItems = v;
    if (this._selectedItems.length > 0) {
      this.onItemSelected.emit(this._selectedItems[0]);
    }
  }


  private _selectedItemKeys: any;
  public get selectedItemKeys(): any {
    return this._selectedItemKeys;
  }
  public set selectedItemKeys(v: any) {
    this._selectedItemKeys = v;
  }



  items: DataSource;

  @Output()
  onItemSelected = new EventEmitter<VisuPage>();

  constructor(private visuService: VisuService) {

  }

  async ngOnInit() {
    this.pages = await this.visuService.getVisuPages();

    this.items = new DataSource({
      store: new ArrayStore({
        key: "id",
        data: this.pages
      })
    });
  }

  onSelectionChanged($event) {
  }

}
