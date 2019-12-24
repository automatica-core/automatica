import { EventEmitter } from "@angular/core";
import { IGridsterItem } from ".";
import { VisuObjectInstance } from "../visu-object-instance";
import { Model, JsonFieldInfo, JSON_FIELDS } from "../base-model";
import { VisuObjectTemplate } from "../visu-object-template";

@Model()
export class VisuObjectMobileInstance extends VisuObjectInstance implements IGridsterItem {
    itemResized = new EventEmitter<void>();
    public isSelected: boolean = false;


    private _isLoading: boolean;

    public static CreateFromTemplate(template: VisuObjectTemplate): VisuObjectMobileInstance {
        const instance = new VisuObjectMobileInstance();

        instance.x = instance.X;
        instance.y = instance.Y;
        instance.cols = instance.Width;
        instance.rows = instance.Height;

        super.FillNewInstance(instance, template);

        return instance;
    }


    public get x(): number {
        return this.X;
    }
    public set x(v: number) {
        this.X = v;
    }
    public get y(): number {
        return this.Y;
    }
    public set y(v: number) {
        this.Y = v;
    }
    public get rows(): number {
        return this.Height;
    }
    public set rows(v: number) {
        this.Height = v;
    }
    public get cols(): number {
        return this.Width;
    }
    public set cols(v: number) {
        this.Width = v;
    }

    public get minItemRows(): number {
        const value = this.VisuObjectTemplate.MinHeight;

        if (!value) {
            return void 0;
        }
        return value;
    }
    public get minItemCols(): number {
        const value = this.VisuObjectTemplate.MinWidth;

        if (!value) {
            return void 0;
        }
        return value;
    }
    public get maxItemRows(): number {
        const value = this.VisuObjectTemplate.MaxHeight;

        if (!value) {
            return void 0;
        }
        return value;
    }
    public get maxItemCols(): number {
        const value = this.VisuObjectTemplate.MaxWidth;

        if (!value) {
            return void 0;
        }
        return value;
    }

    public typeInfo(): string {
        return super.typeInfo() + "Mobile";
    }

    validate() {
        return true;
    }


    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        const json = new Map<string, JsonFieldInfo>();
        const baseModelTypes = JSON_FIELDS.get(super.typeInfo());

        if (baseModelTypes) {
            for (const key of Array.from(baseModelTypes.keys())) {
                json.set(key, baseModelTypes.get(key));
            }
        }
        return json;

    }


    public get isLoading(): boolean {
        return this._isLoading;
    }
    public set isLoading(v: boolean) {
        this._isLoading = v;
    }


}
