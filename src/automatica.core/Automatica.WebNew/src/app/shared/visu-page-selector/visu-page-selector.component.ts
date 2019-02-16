import { Component, OnInit, Output, EventEmitter, Input } from "@angular/core";

@Component({
  selector: "app-visu-page-selector",
  templateUrl: "./visu-page-selector.component.html",
  styleUrls: ["./visu-page-selector.component.scss"]
})
export class VisuPageSelectorComponent implements OnInit {


  private _popupVisible: boolean;
  public get popupVisible(): boolean {
    return this._popupVisible;
  }
  public set popupVisible(v: boolean) {
    this._popupVisible = v;
  }

  private _value: number;

  @Output()
  valueChange = new EventEmitter<number>();
  @Input()
  public get value(): number {
    return this._value;
  }
  public set value(v: number) {
    this._value = v;
    this.valueChange.emit(v);
  }


  private _readOnly: number;
  @Input()
  public get readOnly(): number {
    return this._readOnly;
  }
  public set readOnly(v: number) {
    this._readOnly = v;
  }

  @Output()
  onValueChanged = new EventEmitter<any>();

  @Output()
  openDialog = new EventEmitter<void>();

  constructor() { }

  ngOnInit() {

  }

  openClick($event) {
    this.openDialog.emit();
  }

}
