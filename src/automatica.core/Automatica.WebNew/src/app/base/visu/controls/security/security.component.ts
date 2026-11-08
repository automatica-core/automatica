import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, inject, ViewChild } from "@angular/core";
import { confirm } from "devextreme/ui/dialog";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
import { SecurityVisuService } from "src/app/services/security-visu.service";
import { SecurityAction, SecurityControlInfo, SecurityDetail, SecurityLogEntry, SecurityState, SecurityUser } from "src/app/base/model/security/security";
import { BaseControlComponent } from "../base-control/base-control.component";

/** how often the state is read again without a value change (alarm levels advance on the server) */
const REFRESH_MS = 15000;

/** value changes of the logic outputs come in bursts: read the state once they settled */
const CHANGE_DEBOUNCE_MS = 300;

/** the visu tile shows the main action only (the first the logic offers), the popup all of them */
const TILE_ACTIONS = 1;

interface UserForm {
  previousName?: string;
  name: string;
  groups: string;
  code: string;
  tag: string;
  clearCode: boolean;
  clearTag: boolean;
}

/**
 * Tile of the security logics (burglar alarm and central, fire/water and emergency alarm, alarm chain, access
 * controller). State and commands go through webapi/security-visu; the logic decides which actions are possible.
 * Codes typed in the user form are sent once and never kept or logged.
 */
@Component({
  selector: "visu-security",
  templateUrl: "./security.component.html",
  styleUrls: ["./security.component.scss"],
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: false,
  host: {
    "[class.sev-alarm]": "severity === 'alarm'",
    "[class.sev-warning]": "severity === 'warning'",
    "[class.sev-info]": "severity === 'info'"
  }
})
export class SecurityComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

  @ViewChild(BaseControlComponent) baseControl: BaseControlComponent;

  private readonly security = inject(SecurityVisuService);

  info: SecurityControlInfo | undefined;
  busy = false;
  loadFailed = false;
  countdownText = "";
  userForm: UserForm | undefined;

  private changeTimer: any;
  private refreshTimer: any;
  private tickTimer: any;

  get state(): SecurityState | undefined {
    return this.info?.state;
  }

  get severity(): string {
    return this.state?.severity ?? "ok";
  }

  get controlId(): string {
    return this.ruleInstance?.ObjId;
  }

  get stateIcon(): any {
    return this.state?.icon || "shield-halved";
  }

  get statusText(): string {
    return this.state ? this.translate.translate(this.state.statusText) : "";
  }

  get canCommand(): boolean {
    return !!this.info?.canCommand && !this.editMode;
  }

  get actions(): SecurityAction[] {
    return this.state?.actions ?? [];
  }

  get tileActions(): SecurityAction[] {
    return this.actions.slice(0, TILE_ACTIONS);
  }

  get details(): SecurityDetail[] {
    return this.state?.details ?? [];
  }

  get log(): SecurityLogEntry[] {
    return this.state?.log ?? [];
  }

  get users(): SecurityUser[] | undefined {
    return this.info?.isAdmin && this.info.kind === "access" ? (this.state?.users ?? []) : undefined;
  }

  get subText(): string {
    const first = this.details[0];
    return first ? `${this.translate.translate(first.label)}: ${this.detailValue(first)}` : "";
  }

  async ngOnInit() {
    this.baseOnInit();
    await this.mobileRuleInit();
    if (this.editMode) {
      return;
    }

    await this.load();
    this.refreshTimer = setInterval(() => this.load(), REFRESH_MS);
    this.tickTimer = setInterval(() => this.updateCountdown(), 1000);
  }

  ngOnDestroy() {
    clearTimeout(this.changeTimer);
    clearInterval(this.refreshTimer);
    clearInterval(this.tickTimer);
    super.baseOnDestroy();
  }

  onRuleInstanceValueChanged(interfaceId, value) {
    // an output of the logic changed: the state is built by the server
    clearTimeout(this.changeTimer);
    this.changeTimer = setTimeout(() => this.load(), CHANGE_DEBOUNCE_MS);
  }

  async load() {
    if (!this.controlId || this.destroyed) {
      return;
    }
    try {
      this.info = await this.security.getState(this.controlId);
      this.loadFailed = false;
      this.updateCountdown();
    } catch (error) {
      this.loadFailed = true;
    }
  }

  private updateCountdown() {
    const until = this.state?.countdownUntil ? new Date(this.state.countdownUntil).getTime() : NaN;
    if (isNaN(until)) {
      this.countdownText = "";
      return;
    }
    const seconds = Math.max(0, Math.ceil((until - Date.now()) / 1000));
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    this.countdownText = `${m}:${s.toString().padStart(2, "0")}`;
    if (seconds === 0) {
      // the server moved on (armed, next stage): read the new state soon
      clearTimeout(this.changeTimer);
      this.changeTimer = setTimeout(() => this.load(), 1200);
    }
  }

  detailValue(detail: SecurityDetail): string {
    return detail.translate ? this.translate.translate(detail.value) : detail.value;
  }

  eventText(entry: SecurityLogEntry): string {
    return this.translate.translate("SURVEILLANCE.EVENT." + (entry.event ?? "").toUpperCase().replace(/-/g, "_"));
  }

  timeText(time: string): string {
    const date = new Date(time);
    return isNaN(date.getTime()) ? "" : date.toLocaleString(this.translate.getLocale()?.language || undefined,
      { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit", second: "2-digit" });
  }

  actionClass(action: SecurityAction): string {
    return "sec-btn " + (action.style ?? "default");
  }

  async run(action: SecurityAction, $event?: Event) {
    $event?.stopPropagation();
    if (!this.canCommand || this.busy) {
      return;
    }
    if (action.confirm) {
      const label = this.translate.translate(action.label);
      const ok = await confirm(`${label}: ${this.displayText}?`, this.translate.translate("UI.VISU.SECURITY_CONFIRM_TITLE"));
      if (!ok) {
        return;
      }
    }
    await this.send(action.command);
  }

  /** sends a command; arguments may hold a code and are never logged */
  private async send(command: string, args?: { [key: string]: string }): Promise<boolean> {
    this.busy = true;
    try {
      const state = await this.security.command(this.controlId, command, args);
      if (state && this.info) {
        this.info = { ...this.info, state };
        this.updateCountdown();
      }
      return true;
    } catch (error) {
      const status = (<any>error)?.status;
      const key = status === 409 ? "UI.VISU.SECURITY_NOT_POSSIBLE"
        : status === 403 ? "UI.VISU.SECURITY_DENIED"
          : "UI.VISU.SECURITY_FAILED";
      this.notifyService.notifyError(this.translate.translate(key));
      await this.load();
      return false;
    } finally {
      this.busy = false;
    }
  }

  openDetails($event?: Event) {
    $event?.stopPropagation();
    this.baseControl?.openPopup();
  }

  // access controller: users (administrators only)

  newUser() {
    this.userForm = { name: "", groups: (this.state?.groups ?? [])[0] ?? "", code: "", tag: "", clearCode: false, clearTag: false };
  }

  editUser(user: SecurityUser) {
    this.userForm = { previousName: user.name, name: user.name, groups: user.groups.join(", "), code: "", tag: "", clearCode: false, clearTag: false };
  }

  cancelUser() {
    this.userForm = undefined;
  }

  get codeValid(): boolean {
    const code = this.userForm?.code ?? "";
    const min = this.state?.minCodeLength ?? 4;
    return code === "" || new RegExp(`^[0-9]{${min},12}$`).test(code);
  }

  async saveUser() {
    const form = this.userForm;
    if (!form || !form.name.trim() || !this.codeValid) {
      return;
    }
    const args: { [key: string]: string } = { name: form.name.trim(), groups: form.groups };
    if (form.previousName) args.previousName = form.previousName;
    if (form.code) args.code = form.code;
    if (form.tag.trim()) args.tag = form.tag.trim();
    if (form.clearCode) args.clearCode = "true";
    if (form.clearTag) args.clearTag = "true";

    // the code leaves the form right away
    form.code = "";
    if (await this.send("user-save", args)) {
      this.userForm = undefined;
    } else {
      this.notifyService.notifyError(this.translate.translate("UI.VISU.SECURITY_SAVE_FAILED"));
    }
  }

  async deleteUser(user: SecurityUser) {
    const ok = await confirm(`${user.name}: ${this.translate.translate("UI.VISU.SECURITY_DELETE_CONFIRM")}`,
      this.translate.translate("UI.VISU.SECURITY_CONFIRM_TITLE"));
    if (ok) {
      await this.send("user-delete", { name: user.name });
    }
  }

  async learnTag(user: SecurityUser) {
    await this.send("learn-tag", { name: user.name });
  }

  async cancelLearn() {
    await this.send("learn-cancel");
  }
}
