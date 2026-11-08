import { BaseModel } from "./base-model";
import { Setting, SettingReloadContext } from "./setting";

function setting(extra: any): Setting {
  // as the web api sends it (camel case)
  return BaseModel.getBaseModelFromJson<Setting>({
    typeInfo: "Setting", objId: 1, valueKey: "Latitude", valueDouble: 48.1, type: 7, group: "SERVER.SETTINGS",
    isVisible: true, isReadonly: false, ...extra
  });
}

describe("Setting", () => {

  it("reads the reload flags of the web api", () => {
    const s = setting({ needsReloadOnChange: true, reloadContext: SettingReloadContext.Recorders });

    expect(s.NeedsReloadOnChange).toBeTrue();
    expect(s.ReloadContext).toBe(SettingReloadContext.Recorders);
  });

  it("only a server reload context restarts the server", () => {
    expect(setting({ needsReloadOnChange: true, reloadContext: SettingReloadContext.Server }).restartsServerOnChange).toBeTrue();

    expect(setting({ needsReloadOnChange: true, reloadContext: SettingReloadContext.Recorders }).restartsServerOnChange).toBeFalse();
    expect(setting({ needsReloadOnChange: true, reloadContext: SettingReloadContext.RemoteConnect }).restartsServerOnChange).toBeFalse();
    // the context defaults to Server (0) for settings that never reload
    expect(setting({ needsReloadOnChange: false, reloadContext: SettingReloadContext.Server }).restartsServerOnChange).toBeFalse();
    expect(setting({ needsReloadOnChange: false, reloadContext: SettingReloadContext.None }).restartsServerOnChange).toBeFalse();
  });

  it("older servers without the flags never announce a restart", () => {
    expect(setting({}).restartsServerOnChange).toBeFalse();
  });
});
