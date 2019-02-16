import { PropertyTemplateType } from "../property-template"
import { VirtualPropertyInstance } from "./virtual-property-instance";
import { VisuPage } from "../visu-page";
import { VisuObjectInstance } from "../visu-object-instance";
import { NodeInstance } from "../node-instance";
import { AreaInstance } from "../areas";
import { CategoryInstance } from "../categories";
import { RuleInstance } from "../rule-instance";

export class VirtualUserGroupPropertyInstance extends VirtualPropertyInstance {
    /**
     *
     */
    constructor(private instance: VisuPage | VisuObjectInstance | NodeInstance | AreaInstance | CategoryInstance | RuleInstance) {
        super(instance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.USER_GROUP.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.USER_GROUP.DESCRIPTION";
        this.PropertyTemplate.Key = "user_group";

        this.PropertyTemplate.Group = "COMMON.CATEGORY.VISU";
        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.UserGroup;
        this.PropertyTemplate.IsReadonly = false;

        this.PropertyTemplate.Order = 4;
    }

    get Value(): any {
        return this.instance.This2UserGroup;
    }
    set Value(value: any) {
        this.instance.This2UserGroup = value;
    }

}
