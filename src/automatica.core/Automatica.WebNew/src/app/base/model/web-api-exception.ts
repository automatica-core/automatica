import { BaseModel, Model, JsonFieldInfo, JsonProperty, JsonPropertyName } from "./base-model";

export enum ExceptionSeverity {
    Info,
    Warning,
    Error,
    Dead
}

@Model()
export class WebApiException extends BaseModel {
    private _errorText: string;
    private _severity: ExceptionSeverity;

    @JsonPropertyName("severity")
    public get Severity(): ExceptionSeverity {
        return this._severity;
    }
    public set Severity(v: ExceptionSeverity) {
        this._severity = v;
    }


    @JsonPropertyName("errorText")
    public get ErrorText(): string {
        return this._errorText;
    }
    public set ErrorText(v: string) {
        this._errorText = v;
    }



    public typeInfo() {
        return "WebApiException";
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }
}
