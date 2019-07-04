import { Component, NgModule, Output, Input, EventEmitter, ViewChild, OnInit } from "@angular/core";
import { DxTreeViewModule, DxTreeViewComponent } from "devextreme-angular/ui/tree-view";
import { TranslationModule } from "angular-l10n";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";
import { CommonModule } from "@angular/common";
import { Router } from "@angular/router";

@Component({
    selector: "app-side-navigation-menu",
    templateUrl: "./side-navigation-menu.component.html",
    styleUrls: ["./side-navigation-menu.component.scss"]
})
export class SideNavigationMenuComponent implements OnInit {

    private _menu: DxTreeViewComponent;

    @ViewChild(DxTreeViewComponent, {static: false})
    public get menu(): DxTreeViewComponent {
        return this._menu;
    }
    public set menu(v: DxTreeViewComponent) {
        this._menu = v;
    }

    @Output()
    selectedItemChanged = new EventEmitter<string>();


    private _items: any[];
    @Input()
    public get items(): any[] {
        return this._items;
    }
    public set items(v: any[]) {
        this._items = v;
    }


    @Input()
    set selectedItem(value: String) {
        if (this.menu && this.menu.instance) {
            this.menu.instance.selectItem(value);
        }
    }

    private _compactMode = false;
    @Input()
    get compactMode() {
        return this._compactMode;
    }
    set compactMode(val) {
        this._compactMode = val;
        if (val && this.menu && this.menu.instance) {
            this.menu.instance.collapseAll();
        }
    }

    constructor(private router: Router) { }

    ngOnInit() {
        
    }

    setDefaultItem() {
        if (!this.items) {
            return;
        }
        const defaultItem = this.findDefaultItemRecursive(this.items);
        if (this.menu && this.menu.instance && defaultItem) {
            this.menu.instance.selectItem(defaultItem);
        }
    }

    findDefaultItemRecursive(items: any[]) {
        for (const x of items) {
            if (x.default) {
                return x;
            }

            if (x.items) {
                const item = this.findDefaultItemRecursive(x.items);
                if (item) {
                    return item;
                }
            }
        }
        return void 0;
    }

    updateSelection(event) {
        const nodeClass = "dx-treeview-node";
        const selectedClass = "dx-state-selected";
        const leafNodeClass = "dx-treeview-node-is-leaf";
        const element: HTMLElement = event.element;

        const rootNodes = element.querySelectorAll(`.${nodeClass}:not(.${leafNodeClass}`);
        Array.from(rootNodes).forEach(node => {
            node.classList.remove(selectedClass);
        });

        let selectedNode = element.querySelector(`.${nodeClass}.${selectedClass}`);
        while (selectedNode && selectedNode.parentElement) {
            if (selectedNode.classList.contains(nodeClass)) {
                selectedNode.classList.add(selectedClass);
            }

            selectedNode = selectedNode.parentElement;
        }

        this.setDefaultItem();
    }

    onItemClick(event) {
        this.selectedItemChanged.emit(event);
    }

    onMenuInitialized(event) {
        event.component.option("deferRendering", false);

        this.setDefaultItem();
    }
}

@NgModule({
    imports: [CommonModule, DxTreeViewModule, TranslationModule, FontAwesomeModule],
    declarations: [SideNavigationMenuComponent],
    exports: [SideNavigationMenuComponent]
})
export class SideNavigationMenuModule { }
