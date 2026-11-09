import { EventEmitter } from "@angular/core";
import { PropertyEditorComponent } from "./propertyeditor.component";
import { NodeInstance } from "src/app/base/model/node-instance";
import { Setting } from "src/app/base/model/setting";
import { VirtualSettingsPropertyInstance } from "src/app/base/model/virtual-props/settings/settings-property-instance";
import { PropertyInstance } from "src/app/base/model/property-instance";

/**
 * valueChanged without the component's template (the full PropertyEditorComponent spec is disabled):
 * the method only needs the services it calls, so it runs on a bare instance with fakes.
 */
describe("PropertyEditorComponent.valueChanged", () => {
  let editor: any;
  let config: jasmine.SpyObj<any>;
  let instances: jasmine.SpyObj<any>;
  let notify: jasmine.SpyObj<any>;

  function setting(key: string, restart = false) {
    const s = new Setting();
    s.ObjId = key;
    s.ValueKey = key;
    s.Group = "SERVER.MCP";
    s.Type = 0;
    s.NeedsReloadOnChange = restart;
    s.ReloadContext = restart ? 0 : 1;
    return s;
  }

  function node(parent?: string) {
    const n = new NodeInstance(void 0);
    n.ObjId = "node";
    n.This2ParentNodeInstance = parent;
    return n;
  }

  beforeEach(() => {
    config = jasmine.createSpyObj("ConfigService", ["update"]);
    config.update.and.resolveTo(undefined);
    instances = jasmine.createSpyObj("LogicEditorInstanceService", ["saveSetting"]);
    instances.saveSetting.and.resolveTo(undefined);
    notify = jasmine.createSpyObj("NotifyService", ["notifyInfo", "notifyError"]);

    editor = Object.create(PropertyEditorComponent.prototype);
    Object.assign(editor, {
      config,
      nodeInstanceService: instances,
      notifyService: notify,
      translate: { translate: (k: string) => k },
      appService: { beginLoading: () => () => void 0 },
      changeDetection: { detectChanges: () => void 0 },
      validate: new EventEmitter(),
      onChanged: new EventEmitter(),
      handleError: jasmine.createSpy("handleError")
    });
  });

  it("serverSettingOnTheRoot_SavesOnlyTheSettingNotTheWholeTree", async () => {
    editor._item = node();
    Object.defineProperty(editor, "item", { get: () => editor._item });
    const prop = new VirtualSettingsPropertyInstance(setting("mcpEnabled"));

    await editor.valueChanged({}, { data: prop });

    expect(instances.saveSetting).toHaveBeenCalledOnceWith(prop);
    expect(config.update).not.toHaveBeenCalled();
    expect(notify.notifyInfo).not.toHaveBeenCalled();
    expect(editor.handleError).not.toHaveBeenCalled();
  });

  it("serverSettingThatRestartsTheServer_TellsTheUser", async () => {
    Object.defineProperty(editor, "item", { get: () => node() });
    const prop = new VirtualSettingsPropertyInstance(setting("language", true));

    await editor.valueChanged({}, { data: prop });

    expect(instances.saveSetting).toHaveBeenCalledOnceWith(prop);
    expect(notify.notifyInfo).toHaveBeenCalledOnceWith("COMMON.SERVER_SETTINGS_UPDATE_REQUIRE_REBOOT");
  });

  it("serverSettingSaveFails_ReportsTheError", async () => {
    Object.defineProperty(editor, "item", { get: () => node() });
    const error = new Error("nope");
    instances.saveSetting.and.rejectWith(error);

    await editor.valueChanged({}, { data: new VirtualSettingsPropertyInstance(setting("mcpEnabled")) });

    expect(editor.handleError).toHaveBeenCalledOnceWith(error);
  });

  it("normalPropertyOnANode_StillSavesTheNode", async () => {
    const item = node("parent");
    Object.defineProperty(editor, "item", { get: () => item });
    const prop = Object.defineProperty(Object.create(PropertyInstance.prototype), "updateScope", { get: () => 1 });

    await editor.valueChanged({}, { data: prop });

    expect(config.update).toHaveBeenCalledOnceWith(item, 1);
    expect(instances.saveSetting).not.toHaveBeenCalled();
  });

  it("normalPropertyOnTheRoot_StillSavesTheNode", async () => {
    const item = node();
    Object.defineProperty(editor, "item", { get: () => item });
    const prop = Object.defineProperty(Object.create(PropertyInstance.prototype), "updateScope", { get: () => 1 });

    await editor.valueChanged({}, { data: prop });

    expect(config.update).toHaveBeenCalledOnceWith(item, 1);
    expect(instances.saveSetting).not.toHaveBeenCalled();
  });
});
