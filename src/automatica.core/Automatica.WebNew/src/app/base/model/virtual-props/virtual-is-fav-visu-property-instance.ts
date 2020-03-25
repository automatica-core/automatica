import { PropertyInstance } from "../property-instance"
import { PropertyTemplate, PropertyTemplateType } from "../property-template"
import { PropertyType } from "../property-type"
import { NodeInstance } from "../node-instance"
import { VirtualPropertyInstance } from "./virtual-property-instance"
import { RuleInstance } from "../rule-instance";

export class VirtualIsFavoriteVisuPropertyInstance extends VirtualPropertyInstance {
    /**
     *
     */
    constructor(private instance: NodeInstance | RuleInstance) {
        super(instance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.IS_FAVORITE.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.IS_FAVORITE.DESCRIPTION";
        this.PropertyTemplate.Key = "is_favorite";

        this.PropertyTemplate.Group = "COMMON.CATEGORY.VISU";
        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Bool;
        this.PropertyTemplate.IsReadonly = false;

        this.PropertyTemplate.Order = 2;
    }

    get Value(): any {
        return this.instance.IsFavorite;
    }
    set Value(value: any) {
        this.instance.IsFavorite = value;
    }

}
