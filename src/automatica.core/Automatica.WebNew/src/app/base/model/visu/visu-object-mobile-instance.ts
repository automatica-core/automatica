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

        return super.FillNewInstance(instance, template);
    }


    get x(): number {
        return this.X;
    }
    set x(v: number) {
        this.X = v;
    }
    get y(): number {
        return this.Y;
    }
    set y(v: number) {
        this.Y = v;
    }
    get rows(): number {
        return this.Height;
    }
    set rows(v: number) {
        this.Height = v;
    }
    get cols(): number {
        return this.Width;
    }
    set cols(v: number) {
        this.Width = v;
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
