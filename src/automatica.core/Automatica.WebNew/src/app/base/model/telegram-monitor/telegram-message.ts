import { BaseModel, Model, JsonFieldInfo, JsonProperty } from "../base-model";

@Model()
export class TelegramMessage extends BaseModel {

    private _Id: string;
    private _Direction: number;
    private _SourceAddress: string;
    private _TargetAddress: string;
    private _Data: string;
    private _AdditionalMessageString: string;
    private _TimeStamp: Date;

    @JsonProperty()
    public get BusId(): string {
        return this._Id;
    }
    public set BusId(v: string) {
        this._Id = v;
    }

    @JsonProperty()
    public get Direction(): number {
        return this._Direction;
    }
    public set Direction(v: number) {
        this._Direction = v;
    }

    @JsonProperty()
    public get SourceAddress(): string {
        return this._SourceAddress;
    }
    public set SourceAddress(v: string) {
        this._SourceAddress = v;
    }

    @JsonProperty()
    public get TargetAddress(): string {
        return this._TargetAddress;
    }
    public set TargetAddress(v: string) {
        this._TargetAddress = v;
    }

    @JsonProperty()
    public get Data(): string {
        return this._Data;
    }
    public set Data(v: string) {
        this._Data = v;
    }

    @JsonProperty()
    public get AdditionalMessageString(): string {
        return this._AdditionalMessageString;
    }
    public set AdditionalMessageString(v: string) {
        this._AdditionalMessageString = v;
    }

    @JsonProperty()
    public get TimeStamp(): Date {
        return this._TimeStamp;
    }
    public set TimeStamp(v: Date) {
        this._TimeStamp = v;
    }

    public typeInfo(): string {
        return "TelegramMessage";
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }
}
