import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from "@angular/core";

@Component({
    selector: "visu-component-slider",
    templateUrl: "./slider.component.html",
    styleUrls: ["./slider.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class SliderComponent {

  private _state: number;
  @Input()
  public get state(): number {
    return this._state;
  }
  public set state(v: number) {
    if (this._state !== v) {
      this.stateChanged.emit(v);
    }
    this._state = v;
  }


  @Output()
  stateChanged = new EventEmitter<number>();

  format(value: number) {
    return `${Math.round(value)} %`;
  }

  switch($event) {
    this.state = $event.value;

  }
}
