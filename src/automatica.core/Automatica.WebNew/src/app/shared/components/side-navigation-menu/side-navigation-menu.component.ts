import { Component, NgModule, Output, Input, EventEmitter, ViewChild, OnInit, ChangeDetectionStrategy, OnDestroy, AfterViewInit, ElementRef } from "@angular/core";
import { DxTreeViewModule, DxTreeViewComponent } from "devextreme-angular/ui/tree-view";
import { L10nTranslationModule } from "angular-l10n";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";
import { CommonModule } from "@angular/common";
import { Router } from "@angular/router";
import { DxScrollViewModule } from "devextreme-angular";

@Component({
    selector: "app-side-navigation-menu",
    templateUrl: "./side-navigation-menu.component.html",
    styleUrls: ["./side-navigation-menu.component.scss"],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class SideNavigationMenuComponent implements OnInit, AfterViewInit, OnDestroy {

    private _menu: DxTreeViewComponent;

    @ViewChild(DxTreeViewComponent, { static: true })
    public get menu(): DxTreeViewComponent {
        return this._menu;
    }
    public set menu(v: DxTreeViewComponent) {
        this._menu = v;
    }

    @Output()
    selectedItemChanged = new EventEmitter<string>();

    @Input()
    menuOpened: boolean = true;

    private _items: any[];
    @Input()
    public get items(): any[] {
        return this._items;
    }
    public set items(v: any[]) {
        this._items = v;
    }

    _selectedItem: string;

    @Input()
    set selectedItem(value: string) {
        if (this.menu && this.menu.instance) {
            this._selectedItem = value;
            this.menu.instance.selectItem(value);
        }
    }
    get selectedItem(): string {
        return this._selectedItem;
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

    setInitialItem() {
        if (!this.items) {
            return;
        }

        const uriSplit = this.router.url.split("/");
        const curItem = uriSplit[uriSplit.length - 1];
        this.selectedItem = curItem;
    }


    updateSelection(event) {
        const nodeClass = "dx-treeview-node";
        const selectedClass = "dx-state-focused";
        const leafNodeClass = "dx-treeview-node-is-leaf";
        const element: HTMLElement = event.element;

        const rootNodes = element.querySelectorAll(`.${nodeClass}:not(.${leafNodeClass})`);
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
    }

    onItemClick(event) {
        this.selectedItemChanged.emit(event);
        this._selectedItem = event.itemData.path;
    }

    onMenuInitialized(event) {
        event.component.option("deferRendering", false);

        setTimeout(() => {
            this.menu.instance.collapseAll();
        }, 100);
    }


    ngAfterViewInit() {
        this.setInitialItem();

    }

    ngOnDestroy() {

    }
}

@NgModule({
    imports: [CommonModule, DxTreeViewModule, L10nTranslationModule, FontAwesomeModule, DxScrollViewModule],
    declarations: [SideNavigationMenuComponent],
    exports: [SideNavigationMenuComponent]
})
export class SideNavigationMenuModule { }
