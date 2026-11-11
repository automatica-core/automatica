import { EventEmitter } from "@angular/core";
import { LearnedDevice, LearnModeDialogComponent } from "./learn-mode-dialog.component";

/**
 * The learn mode dialog on a bare instance with fakes: what a driver notification turns into and what "Add" sends.
 */
describe("LearnModeDialogComponent", () => {
  let dialog: any;
  let config: jasmine.SpyObj<any>;
  let notify: jasmine.SpyObj<any>;

  function prop(key: string, value: string) {
    return { This2PropertyTemplateNavigation: { Key: key }, Value: value };
  }

  function teachIn(serial: string, extra: { [key: string]: string } = {}) {
    const info = { status: "new", profile: "A5-20-01", telegram: "4BS EEP", rssi: "-62", paired: "false", ...extra };
    return [serial, `${info.profile} · ${info.telegram}`, [], [
      prop("enocean-serialnumber", serial),
      ...Object.keys(info).map(k => prop(`learn-info-${k}`, info[k]))
    ]];
  }

  beforeEach(() => {
    config = jasmine.createSpyObj("ConfigService", ["addLearnedNode"]);
    notify = jasmine.createSpyObj("NotifyService", ["notifySuccess", "notifyError"]);

    dialog = Object.create(LearnModeDialogComponent.prototype);
    Object.assign(dialog, {
      config,
      notifyService: notify,
      translate: { translate: (k: string, p?: any) => p?.name !== undefined ? `${k}:${p.name}` : k },
      changeDetection: { detectChanges: () => void 0 },
      dataHub: { enableLearnMode: jasmine.createSpy("enableLearnMode").and.resolveTo(undefined) },
      added: new EventEmitter(),
      devices: [],
      relearnTimers: [],
      visible: true,
      node: { ObjId: "learned" }
    });
  });

  it("teachIn_AddsTheSenderWithItsAddressButNotTheInfoValues", () => {
    dialog.onLearnNotification(teachIn("0187A1B2", { paired: "true" }));

    expect(dialog.devices.length).toBe(1);
    const device: LearnedDevice = dialog.devices[0];
    expect(device.key).toBe("0187A1B2");
    expect(device.status).toBe("new");
    expect(device.profile).toBe("A5-20-01");
    expect(device.telegram).toBe("4BS EEP");
    expect(device.rssi).toBe(-62);
    expect(device.replySent).toBeTrue();
    expect(device.properties).toEqual({ "enocean-serialnumber": "0187A1B2" });
    expect(device.uniqueProperty).toBe("enocean-serialnumber");
  });

  it("sameSenderTwice_IsOneCard", () => {
    dialog.onLearnNotification(teachIn("0187A1B2"));
    dialog.onLearnNotification(teachIn("0187A1B2", { rssi: "-80" }));

    expect(dialog.devices.length).toBe(1);
    expect(dialog.devices[0].rssi).toBe(-80);
  });

  it("knownSender_ShowsTheExistingNode", () => {
    dialog.onLearnNotification(teachIn("0187A1B2", { status: "known", known: "Valve office" }));

    expect(dialog.devices[0].status).toBe("known");
    expect(dialog.devices[0].knownName).toBe("Valve office");
    expect(dialog.devices[0].canAdd).toBeFalse();
  });

  it("dataTelegram_OfASenderNotInThisSession_IsIgnored", () => {
    dialog.onLearnNotification(teachIn("0187A1B2", { status: "receiving" }));

    expect(dialog.devices.length).toBe(0);
  });

  it("dataTelegram_AfterAdding_MarksTheDevicePaired", async () => {
    dialog.onLearnNotification(teachIn("0187A1B2"));
    const device: LearnedDevice = dialog.devices[0];
    device.template = "valve-template";
    device.name = "Valve kitchen";
    config.addLearnedNode.and.resolveTo({ Name: "Valve kitchen" });

    await dialog.add(device);
    dialog.clearRelearn();
    expect(device.status).toBe("added");

    dialog.onLearnNotification(teachIn("0187A1B2", { status: "receiving" }));
    expect(device.status).toBe("paired");
  });

  it("add_SendsNameAreaAndAddress", async () => {
    dialog.onLearnNotification(teachIn("0187A1B2"));
    const device: LearnedDevice = dialog.devices[0];
    device.template = "valve-template";
    device.name = "  Valve kitchen ";
    device.area = "kitchen";
    config.addLearnedNode.and.resolveTo({ Name: "Valve kitchen" });
    const added = spyOn(dialog.added, "emit");

    await dialog.add(device);
    dialog.clearRelearn();

    expect(config.addLearnedNode).toHaveBeenCalledWith({
      Parent: "learned",
      NodeTemplate: "valve-template",
      Name: "Valve kitchen",
      Area: "kitchen",
      Properties: { "enocean-serialnumber": "0187A1B2" },
      UniqueProperty: "enocean-serialnumber",
      Replace: undefined
    });
    expect(added).toHaveBeenCalled();
    expect(notify.notifySuccess).toHaveBeenCalledWith("LEARN_MODE.ADDED:Valve kitchen");
  });

  it("add_DuplicateSerial_ShowsTheNodeThatHasIt", async () => {
    dialog.onLearnNotification(teachIn("0187A1B2"));
    const device: LearnedDevice = dialog.devices[0];
    device.template = "valve-template";
    device.name = "Valve kitchen";
    config.addLearnedNode.and.rejectWith({ status: 409, error: { NodeId: "n1", Name: "Valve office" } });

    await dialog.add(device);

    expect(device.status).toBe("known");
    expect(device.error).toBe("LEARN_MODE.DUPLICATE:Valve office");
  });

  it("replace_NeedsANodeButNoTemplate", () => {
    const device = new LearnedDevice();
    device.mode = "replace";
    expect(device.canAdd).toBeFalse();
    device.replace = "n1";
    expect(device.canAdd).toBeTrue();
  });

  it("bars_FollowTheSignal", () => {
    const device = new LearnedDevice();
    expect(device.bars).toBe(0);
    device.rssi = -95;
    expect(device.bars).toBe(0);
    device.rssi = -70;
    expect(device.bars).toBe(2);
    device.rssi = -45;
    expect(device.bars).toBe(4);
  });
});
