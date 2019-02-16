import { BaseModel, Model, JsonFieldInfo, JsonProperty } from "../base-model";
import { TelegramMessage } from "./telegram-message";
import { INameModel } from "../INameModel";

@Model()
export class TelegramMonitorInstance extends BaseModel implements INameModel {

    private _Id: string;
    private _Name: string;
    private _Desription: string;
    private _BusType: string;

    public get DisplayName(): string {
        return this.BusType + " (" + this.Name + ")";
    }

    @JsonProperty()
    public get Id(): string {
        return this._Id;
    }
    public set Id(v: string) {
        this._Id = v;
    }

    @JsonProperty()
    public get Name(): string {
        return this._Name;
    }
    public set Name(v: string) {
        this._Name = v;
    }

    @JsonProperty()
    public get Desription(): string {
        return this._Desription;
    }
    public set Desription(v: string) {
        this._Desription = v;
    }



    @JsonProperty()
    public get BusType(): string {
        return this._BusType;
    }
    public set BusType(v: string) {
        this._BusType = v;
    }


    private _Messages: TelegramMessage[] = [];
    public get Messages(): TelegramMessage[] {
        return this._Messages;
    }
    public set Messages(v: TelegramMessage[]) {
        this._Messages = v;
    }



    public typeInfo(): string {
        return "TelegramMonitorInstance";
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }
}
