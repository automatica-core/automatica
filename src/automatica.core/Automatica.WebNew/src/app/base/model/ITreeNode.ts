import { PropertyInstance } from "./property-instance";

export interface ITreeNode {
    Id: string;
    ParentId: string;
    Children: ITreeNode[];
    DisplayName: string;
    Description: string;
    Properties: PropertyInstance[];
    Parent: ITreeNode;

    ValidationOk: boolean;
    Validate: boolean;

    Value?: any;

    Icon: string;

    getPropertyValue(property: string): any;
    getPropertyValueById(id: string): any;

    validate(): boolean;
}
