import { VirtualGenericPropertyInstance } from "../virtual-generic-property-instance";
import { PropertyTemplateType, ListExtendedPropertyTemplate, EnumExtendedPropertyTemplate } from "../../property-template";
import { INodeInstance } from "../../INodeInstance";

export class VirtualGenericTrendingPropertyInstance<T> extends VirtualGenericPropertyInstance<T> {
    constructor(private nodeInstance: INodeInstance, name: string, order: number, model: any, get: () => T,
        set: (value: T) => void, isReadOnly = false, propertyType: PropertyTemplateType = PropertyTemplateType.Text, metaData: string = void 0, category: string = "COMMON.CATEGORY.TRENDING") {
        super(name, order, model, get, set, isReadOnly, propertyType, category);


        this.PropertyTemplate.Meta = metaData;
        if (metaData) {
            this.PropertyTemplate.ExtendedType = new EnumExtendedPropertyTemplate(this.PropertyTemplate);
        }
    }



    // public get IsVisible(): boolean {
    //     return true;
    // }

    // public set IsVisible(v: boolean) {

    // }

}
