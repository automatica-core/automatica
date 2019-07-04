/// <reference path="../../../../node_modules/@types/jszip/index.d.ts" />

import { Component, OnInit, Input, Output, EventEmitter, ViewChild, ChangeDetectorRef } from "@angular/core";
import { TranslationService } from "angular-l10n";
import { DxMenuComponent } from "devextreme-angular";
import * as JSZip from "jszip";
import { saveAs } from "file-saver";
import { NotifyService } from "src/app/services/notify.service";
import { RulePage } from "src/app/base/model/rule-page";
import { NodeInstance2RulePage } from "src/app/base/model/node-instance-2-rule-page";
import { NodeInstance } from "src/app/base/model/node-instance";
import { RuleInstance } from "src/app/base/model/rule-instance";
import { NodeTemplate } from "src/app/base/model/node-template";
import { RuleTemplate } from "src/app/base/model/rule-template";
import { CustomMenuItem } from "src/app/base/model/custom-menu-item";

@Component({
  selector: "p3-config-menu",
  templateUrl: "./config-menu.component.html",
  styleUrls: ["./config-menu.component.scss"]
})

export class ConfigMenuComponent implements OnInit {

  @ViewChild("menu", {static: false})
  menu: DxMenuComponent;


  private _selectedItem: NodeInstance | NodeInstance2RulePage | RulePage | RuleInstance;

  @Input()
  public get selectedItem(): NodeInstance | NodeInstance2RulePage | RulePage | RuleInstance {
    return this._selectedItem;
  }
  public set selectedItem(v: NodeInstance | NodeInstance2RulePage | RulePage | RuleInstance) {
    this.selectedItemChanged(v);
    this._selectedItem = v;
  }


  @Input() nodeTemplates: NodeTemplate[];

  @Output() onAddItem = new EventEmitter<NodeTemplate>();
  @Output() onSave = new EventEmitter<any>();
  @Output() onDelete = new EventEmitter<any>();
  @Output() onAddRule = new EventEmitter<RuleTemplate>();
  @Output() onAddRulePage = new EventEmitter<void>();
  @Output() onReinitServer = new EventEmitter<void>();
  @Output() onImportData = new EventEmitter<NodeInstance>();

  @Input()
  showRuleMenu: boolean = false;

  @Input()
  ruleTemplates: RuleTemplate[];
  menuItems: CustomMenuItem[] = [];

  addItemsMenu: CustomMenuItem[] = void 0;

  menuItemNew: CustomMenuItem = {
    id: "new",
    label: "New",
    icon: "fa-plus",
    disabled: true,
    items: this.addItemsMenu
  }
  menuSave: CustomMenuItem = {
    id: "save",
    label: "Save",
    icon: "fa-save",
    items: undefined,
    command: (event) => { this.save(); }
  }

  menuDelete: CustomMenuItem = {
    id: "delete",
    label: "Delete",
    icon: "fa-minus",
    disabled: true,
    items: undefined,
    command: (event) => { this.delete(); }
  }

  menuRules: CustomMenuItem = {
    id: "rules",
    label: "Rules",
    icon: "fa-add",
    disabled: false,
    items: []
  };

  menuRulePages: CustomMenuItem = {
    id: "rulePages",
    label: "RulePage",
    icon: "fa-add",
    disabled: false,
    items: [{
      id: "addRulePage",
      label: "Add",
      icon: "fa-add",
      disabled: false,
      command: (event) => { this.onAddRulePage.emit(); }
    }]
  };

  reinit: CustomMenuItem = {
    id: "reInit",
    label: "ReInit",
    icon: "fa-reload",
    disabled: false,
    command: (event) => { this.reinitServer(); }
  };

  export: CustomMenuItem = {
    id: "export",
    label: "Export",
    icon: "fa-file-export",
    disabled: true,
    command: (event) => { this.exportNode(); }
  };

  import: CustomMenuItem = {
    id: "import",
    label: "Import",
    icon: "fa-file-import",
    disabled: true,
    command: (event) => { this.importNode(); }
  };

  constructor(private translate: TranslationService, private changeRef: ChangeDetectorRef, private notify: NotifyService) {
    this.menuItems = [];


    this.menuItems.push(this.menuItemNew);
    this.menuItems.push(this.menuDelete);
    this.menuItems.push(this.menuSave);
    this.menuItems.push(this.reinit);

    this.menuItemNew.label = translate.translate("COMMON.NEW");
    this.reinit.label = translate.translate("COMMON.REINIT");
    this.export.label = translate.translate("COMMON.EXPORT");
    this.import.label = translate.translate("COMMON.IMPORT");
    this.menuSave.label = translate.translate("COMMON.SAVE");
    this.menuDelete.label = translate.translate("COMMON.DELETE");

    this.menuRulePages.label = translate.translate("COMMON.LOGIC_PAGE");
    this.menuRulePages.items[0].label = translate.translate("COMMON.ADD");


    this.menuRules.label = translate.translate("COMMON.RULES");
  }

  save() {
    this.onSave.emit();
  }

  reinitServer() {
    this.onReinitServer.emit();
  }

  delete() {
    this.onDelete.emit(this.selectedItem);
  }

  itemClick($event) {
    const item: CustomMenuItem = $event.itemData;

    if (item && item.command) {
      item.command($event);
    }
  }

  async onImportFileChanged($event, that: ConfigMenuComponent) {
    if (that.selectedItem instanceof NodeInstance) {
      const files = $event.target.files;

      if (files.length > 0) {
        const file = files[0];

        const zip = await JSZip.loadAsync(file);
        const manifestoFile = zip.files["manifest.json"];
        const nodesFile = zip.files["nodes.json"];

        if (!manifestoFile || !nodesFile) {
          that.notify.notifyError(this.translate.translate("COMMON.NODE_IMPORT.INVALID_ZIP"));
          return;
        }

        const manifesto = JSON.parse(await manifestoFile.async("text"));
        const node = new NodeInstance(void 0);
        const jsonData = await nodesFile.async("text");
        NodeInstance.fromJson(JSON.parse(jsonData), node, that.translate);


        if (!manifesto || manifesto.crypto !== "cDNyb290IGlzIHRoZSBhd2Vzb21lbmVzcyBpbiBwZXJzb24=") {
          that.notify.notifyError(this.translate.translate("COMMON.NODE_IMPORT.INVALID_ZIP"));
          return;
        }
        if (!manifesto || manifesto.version !== 1.0) {
          that.notify.notifyError(this.translate.translate("COMMON.NODE_IMPORT.INVALID_VERSION"));
          return;
        }

        const targetNodeTemplate = that.getNodeTemplate(that.selectedItem.This2NodeTemplate);
        const sourceNodeTemplate = that.getNodeTemplate(node.This2NodeTemplate);

        if (targetNodeTemplate.ProvidesInterface2InterfaceType !== sourceNodeTemplate.NeedsInterface2InterfacesType) {
          that.notify.notifyError(this.translate.translate("COMMON.NODE_IMPORT.UNSUPPORTED_NODE"));
          return;
        }

        if (targetNodeTemplate.ProvidesInterface.MaxChilds === that.selectedItem.Children.length) {
          that.notify.notifyError(this.translate.translate("COMMON.NODE_IMPORT.MAX_CHILDS_REACHED"));
          return;
        }

        const newNode = that.importRecursive(node, that.selectedItem);
        that.onImportData.emit(newNode);
      }
    }
  }

  private importRecursive(nodeInstance: NodeInstance, parent: NodeInstance): NodeInstance {
    const newNodeInstance = NodeInstance.createForNodeInstanceFromTemplate(this.getNodeTemplate(nodeInstance.This2NodeTemplate), parent);
    newNodeInstance.Name = nodeInstance.Name;
    newNodeInstance.Description = nodeInstance.Description;

    newNodeInstance.This2ParentNodeInstance = parent.ObjId;

    for (const p of nodeInstance.Properties) {
      newNodeInstance.setPropertyValueIfPresent(p.PropertyTemplate.Key, p.Value);
    }

    for (const x of nodeInstance.Children) {
      const child = this.importRecursive(x, newNodeInstance);
      newNodeInstance.Children.push(child);
    }
    return newNodeInstance;
  }

  private getNodeTemplate(id: string): NodeTemplate {
    return this.nodeTemplates.find(a => a.ObjId === id);
  }

  importNode() {
    const fileSelector = document.createElement("input");
    fileSelector.setAttribute("type", "file");

    const that = this;
    fileSelector.onchange = function (data) {
      that.onImportFileChanged(data, that);
    };
    fileSelector.click();
  }

  async exportNode() {
    const data = this.selectedItem.toJson();
    const manifesto = {
      version: 1.0,
      date: new Date(),
      size: data.length,
      crypto: "cDNyb290IGlzIHRoZSBhd2Vzb21lbmVzcyBpbiBwZXJzb24="
    };


    const zipFile: JSZip = new JSZip();
    zipFile.file("nodes.json", JSON.stringify(data));
    zipFile.file("manifest.json", JSON.stringify(manifesto));

    const blob = await zipFile.generateAsync({ type: "blob" });

    saveAs(blob, "export.zip");
  }


  ngOnInit() {


    if (!this.showRuleMenu) {
      this.menuItems.push(this.export);
      this.menuItems.push(this.import);
    }

    if (this.showRuleMenu) {

      this.menuItems.push(this.menuRulePages);

      const map = new Map<string, RuleTemplate[]>();
      for (const x of this.ruleTemplates) {
        if (!map.has(x.Group)) {
          map.set(x.Group, []);
        }
        map.get(x.Group).push(x);
      }

      map.forEach((value: RuleTemplate[], key: string) => {

        const gItem: CustomMenuItem = {
          id: "ruleGroup",
          label: this.translate.translate(key),
          icon: "fa-plus",
          disabled: false,
          items: []
        };
        for (const x of value) {
          const ruleItem: CustomMenuItem = {
            id: "rule",
            label: this.translate.translate(x.Name),
            icon: "fa-plus",
            disabled: false,
            command: (event) => this.onAddRule.emit(x)
          };

          gItem.items.push(ruleItem);
        }

        this.menuRules.items.push(gItem);

      });
      this.menuItems.push(this.menuRules);
    }
  }

  private selectedItemChanged(data: NodeInstance | NodeInstance2RulePage | RulePage | RuleInstance) {

    this.addItemsMenu = [];
    this.menuItemNew.disabled = true;
    this.menuDelete.disabled = true;
    this.export.disabled = true;

    if (!data) {
      return;
    }
    if (this.selectedItem && this.selectedItem.ObjId === data.ObjId) {
      return;
    }

    if (data instanceof RulePage) {
      this.menuDelete.disabled = false;
    } else if (data instanceof NodeInstance2RulePage) {
      this.menuDelete.disabled = false;
    } else if (data instanceof RuleInstance) {
      this.menuDelete.disabled = false;
    } else if (data instanceof NodeInstance) {
      const ni: NodeInstance = data as NodeInstance;

      if (ni.NodeTemplate.IsDeleteable) {
        this.menuDelete.disabled = false;
        this.export.disabled = false;
      } else {
        this.menuDelete.disabled = true;
        this.export.disabled = true;
      }
      this.menuDelete.source = data;

      this.import.disabled = false;


      const nodeTemplates: NodeTemplate[] = NodeInstance.getSupportedTypes(data, this.nodeTemplates);
      this.addSupportedItems(nodeTemplates);

      if (this.addItemsMenu.length === 0) {
        this.menuItemNew.disabled = true;
      } else {
        this.menuItemNew.disabled = false;
      }
      this.menuItemNew.items = this.addItemsMenu;

      const menuItems = this.menuItems;
      this.menuItems = [];
      this.changeRef.detectChanges();
      this.menuItems = menuItems;
    }
  }

  addSupportedItems(items: NodeTemplate[]) {
    if (!items) {
      return;
    }
    for (const a of items) {
      const mItem: CustomMenuItem = {
        id: "addNode",
        label: this.translate.translate(a.Name),
        icon: "fa-plus",
        nodeTemplate: a,
        disabled: false,
        command: (event) => { this.onAddItem.emit(a); }
      };
      this.addItemsMenu.push(mItem);
    }
  }

  addClicked(event) {
    const intf: NodeTemplate = event.item.nodeTemplate;
    this.onAddItem.emit(intf);
  }
}
