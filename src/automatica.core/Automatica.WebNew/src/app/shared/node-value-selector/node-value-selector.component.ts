import { Component, OnInit, Input, EventEmitter, Output } from "@angular/core";
import { ConfigService } from "../../services/config.service";
import { ITreeNode } from "src/app/base/model/ITreeNode";

@Component({
  selector: "node-value-selector",
  templateUrl: "./node-value-selector.component.html",
  styleUrls: ["./node-value-selector.component.scss"]
})
export class NodeValueSelectorComponent implements OnInit {


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


  config: ITreeNode[];

  constructor(private configService: ConfigService) { }

  ngOnInit() {

  }

  openClick($event) {
    this.openDialog.emit();
  }
}
