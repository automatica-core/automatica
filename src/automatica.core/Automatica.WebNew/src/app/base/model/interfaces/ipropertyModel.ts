import { PropertyInstance } from "../property-instance";

export interface IPropertyModel {
    Properties: PropertyInstance[];
    isNewObject: boolean;
}
