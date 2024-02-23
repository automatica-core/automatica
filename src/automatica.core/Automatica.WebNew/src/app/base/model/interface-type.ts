import { BaseModel, JsonFieldInfo, JsonProperty, Model } from "./base-model"

export enum InterfaceTypeEnum {
    Value = "00000000-0000-0000-0000-000000000001",
    Virtual = "177a9144-3f07-4fd2-a71d-51db61c51ad5",
    Ethernet = "c45eda96-7246-4fa0-9239-9ebb52e7ed66",
    Usb = "4a02532b-4aa0-4b4b-a6a7-7a0ab6dff5bd",
    Rs485 = "fa0b3410-c472-474a-af67-ab298e07e427",
    Rs232 = "d585ecd8-8639-4bc8-a6c7-1641f77a9f08",
    UsbIr = "8cfc6b92-0f68-44d4-8709-f340ce48ff1c",
    Board = "4ff72aff-1604-4865-8d40-4d11bbbe2c56",
    RemoteUsb = "2ab266e9-c16e-462d-a98d-a9c4da55dff0"
}

@Model()
export class InterfaceType extends BaseModel {
    @JsonProperty()
    Type: string;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;

    @JsonProperty()
    MaxChilds: number;

    @JsonProperty()
    MaxInstances: number;

    @JsonProperty()
    CanProvideBoardType: boolean;

    @JsonProperty()
    IsDriverInterface: boolean;


    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public typeInfo(): string {
        return "InterfaceType";
    }
}
