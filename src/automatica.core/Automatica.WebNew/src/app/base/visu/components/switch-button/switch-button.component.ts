import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from "@angular/core";

@Component({
    selector: "visu-component-switch-button",
    templateUrl: "./switch-button.component.html",
    styleUrls: ["./switch-button.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class SwitchButtonComponent {

  private _state: boolean;
  @Input()
  public get state(): boolean {
    return this._state;
  }
  public set state(v: boolean) {

    if (v === undefined) {
      this._state = false;
      return;
    }

    this._state = v;
    this.stateChanged.emit(v);
  }

  @Input()
  disabled: boolean = false;


  @Output()
  stateChanged = new EventEmitter<boolean>();

  switch($event, value) {
    this.state = value;
  }
}
