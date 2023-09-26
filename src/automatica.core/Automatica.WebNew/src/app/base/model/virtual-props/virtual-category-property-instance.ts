import { PropertyTemplateType } from "../property-template"
import { VirtualPropertyInstance } from "./virtual-property-instance"
import { ICategoryInstanceModel } from "../ICategoryInstanceModel";

export class VirtualCategoryPropertyInstance extends VirtualPropertyInstance {

    private _categorySelectedKeys: any[] = [];
    constructor(private categoryInstance: ICategoryInstanceModel) {
        super(categoryInstance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.CATEGORY.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.CATEGORY.DESCRIPTION";
        this.PropertyTemplate.Key = "category-name";
        this.PropertyTemplate.IsReadonly = false;

        this.PropertyTemplate.Group = "COMMON.CATEGORY.VISU";
        this.PropertyTemplate.Order = 3;
        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.CategoryInstance;

        this._categorySelectedKeys = [this.categoryInstance.This2CategoryInstance];
    }

    get Value(): any {
        return this.categoryInstance.This2CategoryInstance;
    }
    set Value(value: any) {
        this.categoryInstance.This2CategoryInstance = value;
        this.propertyChanged.emit(this);
    }


    public get categorySelectedKeys(): any[] {
        return this._categorySelectedKeys;
    }
    public set categorySelectedKeys(v: any[]) {
        this._categorySelectedKeys = v;

        if (v && v.length >= 1) {
            this.Value = v[0];
        }
    }


}
