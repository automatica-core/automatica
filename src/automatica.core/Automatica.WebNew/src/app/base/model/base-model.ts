import { EventEmitter } from "@angular/core";
import { L10nTranslationService } from "angular-l10n";


export interface JsonFieldInfo {
    name: string;
    isMandatory: boolean;
    onlyToServer: boolean;
    setDefaultValueIfNotPresent: boolean;
    jsonType: JsonType;
    ignoreToServer: boolean;
}

interface MandatoryFieldInfo {
    propertyName: string;
    onNewObject: boolean;
}

export enum JsonType {
    Any,
    Array
}

function firstLetterLowerCase(string) {
    return string;
}

export enum TrackingState {
    Unchanged,
    Added,
    Modified,
    Deleted
}

export const JSON_FIELDS = new Map<string, Map<string, JsonFieldInfo>>();
export function JsonProperty(isMandatory: boolean = false, onlyToServer: boolean = false) {
    return function (target: any, propertyKey: string) {
        const clsName = getTemplateName(target);

        let list: Map<string, JsonFieldInfo>;

        if (JSON_FIELDS.has(clsName)) {
            list = JSON_FIELDS.get(clsName);
        } else {
            list = new Map<string, JsonFieldInfo>();
            JSON_FIELDS.set(clsName, list);
        }

        list.set(propertyKey, { name: firstLetterLowerCase(propertyKey), isMandatory: isMandatory, onlyToServer: onlyToServer, setDefaultValueIfNotPresent: false, jsonType: JsonType.Any, ignoreToServer: false });
    }

}

export function JsonPropertyName(name: string, isMandatory: boolean = false, onlyToServer: boolean = false, ignoreToServer: boolean = false) {
    return function (target: any, propertyKey: string) {
        const clsName = getTemplateName(target);

        let list: Map<string, JsonFieldInfo>;

        if (JSON_FIELDS.has(clsName)) {
            list = JSON_FIELDS.get(clsName);
        } else {
            list = new Map<string, JsonFieldInfo>();
            JSON_FIELDS.set(clsName, list);
        }

        list.set(propertyKey, { name: name, isMandatory: isMandatory, onlyToServer: onlyToServer, setDefaultValueIfNotPresent: false, jsonType: JsonType.Any, ignoreToServer: ignoreToServer });
    }
}

export function JsonPropertyType(isMandatory: boolean = false, jsonType: JsonType) {

    return function (target: any, propertyKey: string) {
        const clsName = getTemplateName(target);

        let list: Map<string, JsonFieldInfo>;

        if (JSON_FIELDS.has(clsName)) {
            list = JSON_FIELDS.get(clsName);
        } else {
            list = new Map<string, JsonFieldInfo>();
            JSON_FIELDS.set(clsName, list);
        }

        list.set(firstLetterLowerCase(propertyKey), { name: firstLetterLowerCase(propertyKey), isMandatory: isMandatory, onlyToServer: false, setDefaultValueIfNotPresent: true, jsonType: jsonType, ignoreToServer: false });
    }
}

function getTemplateName(target: BaseModel) {
    return target.typeInfo();
}


const MANDATORY_FIELDS = new Map<string, Map<string, MandatoryFieldInfo>>();
export function Mandatory(onNewObject: boolean = false) {
    return function (target: any, propertyKey: string) {
        const clsName = target.type();
        let list: Map<string, MandatoryFieldInfo>;

        if (MANDATORY_FIELDS.has(clsName)) {
            list = MANDATORY_FIELDS.get(clsName);
        } else {
            list = new Map<string, MandatoryFieldInfo>();
            MANDATORY_FIELDS.set(clsName, list);
        }

        list.set(firstLetterLowerCase(propertyKey), { propertyName: firstLetterLowerCase(propertyKey), onNewObject: onNewObject });
    }
}

export const CONSTRUCTORS = new Map<string, Object>(); // key: modelName, value: target

export function ModelName(modelName: string) {
    return function (target: Object) {
        if (CONSTRUCTORS.has(modelName)) {
            console.error("Model", modelName, "already exists: ", CONSTRUCTORS)
        } else {
            CONSTRUCTORS.set(modelName, target);
        }
    }
}
export function Model() {
    return function (target: any) {
        const targ = new target();
        if (CONSTRUCTORS.has(targ.typeInfo())) {
            console.error("Model", targ.typeInfo(), "already exists: ", CONSTRUCTORS)
        } else {
            CONSTRUCTORS.set(targ.typeInfo(), target);
        }
    }
}


export interface ValidationErrorInfo {
    propertyName: string;
    validationType: "MANDATORY" | "EQUAL" | "INVALID" | undefined;
    message?: string;
    data?: any;
}

export interface NotifyChangeInfo {
    propertyName: string;
    object: BaseModel;
    oldValue?: any;
    newValue?: any;
}
// @dynamic
export abstract class BaseModel {

    private _notifyChanges: boolean = false;
    private _itemChanges: Map<string, NotifyChangeInfo> = new Map<string, NotifyChangeInfo>();
    protected _undoRedoEnabled: boolean;
    private _originalJson: any;

    protected translationService: L10nTranslationService;


    private notifyChangeSub: any;
    public notifyChangeEvent: EventEmitter<NotifyChangeInfo> = new EventEmitter<NotifyChangeInfo>();

    private _isDirty: boolean = false;
    private _isNewObject: boolean = true;
    private _typeInfo: string;

    @JsonPropertyName("TrackingState")
    private _trackingState: TrackingState = TrackingState.Added;


    // tslint:disable-next-line: member-ordering
    public static getBaseModelFromJson<T extends BaseModel>(json: any, parent?: BaseModel, translate?: L10nTranslationService): T {
        if (!json || !(json.typeInfo || json.TypeInfo)) {
            console.error("cannot find property TypeInfo in json: ", json);

            console.error("Every class needs to have an TypeInfo entry, otherwise you cannot use the default functions to load your data");
            console.error("Use @Model('<TypeInfo>') for your class");
            console.error("Not forget to register your class in DecoratorInitializerService!");

        }
        if (json.TypeInfo) {
            json.typeInfo = json.TypeInfo;
        }

        const targ: any = CONSTRUCTORS.get(json.typeInfo);
        if (targ === undefined) {
            console.error("Not forget to register your class in DecoratorInitializerService!");
            console.error("cannot find type for " + json.typeInfo);


            return null;
        }
        const baseModel: T = new targ(parent);
        baseModel._typeInfo = json.typeInfo;
        baseModel.fromJson(json, translate);
        baseModel.commitChanges();
        return baseModel;
    }

    // tslint:disable-next-line: member-ordering
    private static getJsonField(typeInfo: string, object: BaseModel): Map<string, JsonFieldInfo> {
        const json = new Map<string, JsonFieldInfo>();
        const originTypes = JSON_FIELDS.get(typeInfo);

        if (originTypes) {
            for (const key of Array.from(originTypes.keys())) {
                json.set(key, originTypes.get(key));
            }
        }
        const baseModelTypes = JSON_FIELDS.get("BaseModel");

        if (baseModelTypes) {
            for (const key of Array.from(baseModelTypes.keys())) {
                json.set(key, baseModelTypes.get(key));
            }
        }

        const specData = object.getJsonProperty();
        if (specData) {
            for (const key of Array.from(specData.keys())) {
                json.set(key, specData.get(key));
            }
        }
        return json;
    }

    // tslint:disable-next-line: member-ordering
    private static inferType(jsonType: JsonType): any {
        switch (jsonType) {
            case JsonType.Array: {
                return [];
            }
            default: {
                return void 0;
            }
        }
    }

    // tslint:disable-next-line: member-ordering
    private static lowerFirstLetter(val: string) {
        return val.charAt(0).toLowerCase() + val.slice(1);
    }

    public static fromJson(json: any, object: BaseModel, translate?: L10nTranslationService) {
        object._originalJson = json;
        object.unregisterChanges();

        object.translationService = translate;

        const jsonFields = this.getJsonField(object.typeInfo(), object);

        if (jsonFields === undefined) {
            return object;
        }

        for (const key of Array.from(jsonFields.keys())) {
            try {
                const value = jsonFields.get(key);

                if (value.onlyToServer) {
                    continue;
                }
                let ob = json[value.name];
                let valueName = value.name;



                if (ob === void 0 || ob === null) {
                    ob = json[this.lowerFirstLetter(value.name)];
                    valueName = this.lowerFirstLetter(value.name);

                }

                if (!json.hasOwnProperty(valueName) && value.isMandatory) {
                    if (value.setDefaultValueIfNotPresent) {
                        object[key] = this.inferType(value.jsonType);
                        console.warn(`No value was provided for property ${valueName}. Using default value of type.`);
                    }
                    console.error("fromJson " + object.typeInfo() + " does not contain a value for " + valueName + " which is mandatory");
                    console.log(json);

                }

                if (json.hasOwnProperty(valueName)) {
                    if (object[key] instanceof Date) {
                        object[key] = new Date(json[valueName]);
                    } else if (json[valueName] && json[valueName].constructor === {}.constructor) {
                        if (ob.hasOwnProperty("TypeInfo") || ob.hasOwnProperty("typeInfo")) {
                            object[key] = BaseModel.getBaseModelFromJson(json[valueName], object, translate);
                        }
                    } else if (json[valueName] instanceof Array) {
                        object[key] = [];

                        for (const obj of json[valueName]) {
                            if (obj.hasOwnProperty("TypeInfo") || obj.hasOwnProperty("typeInfo")) {
                                const model = BaseModel.getBaseModelFromJson(obj, object, translate);
                                model._typeInfo = obj.typeInfo;
                                object[key].push(model);
                            } else {
                                object[key].push(obj);
                            }
                        }
                    } else if (object[key] instanceof BaseModel) {
                        const baseModel = json[valueName];
                        if (baseModel != null) {
                            object[key] = BaseModel.getBaseModelFromJson(baseModel, object, translate);
                        } else {
                            object[key] = void 0;
                        }
                    } else {
                        object[key] = ob;
                    }
                }
            } catch (error) {
                console.log(error);
            }
        }

        object.isNewObject = false;
        object.afterFromJson();
        object._isDirty = false;
        object.registerChanges();
    }

    public get trackingState(): TrackingState {
        return this._trackingState;
    }

    protected abstract getJsonProperty(): Map<string, JsonFieldInfo>;

    public typeInfo(): string {
        return "BaseModel";
    }

    protected afterFromJson() {

    }

    private unregisterChanges() {
        this._notifyChanges = false;
        if (this.notifyChangeSub) {
            this.notifyChangeSub.unsubscribe();
            this.notifyChangeSub = null;
        }
    }

    protected createInstance(): BaseModel {
        return void 0;
    }

    public copy(): BaseModel {
        const model = this.createInstance();
        if (model) {
            if (!this._originalJson) {
                this._originalJson = this.toJson();
            }
            model.fromJson(this._originalJson, this.translationService);
            model._isNewObject = true;
            model._isDirty = true;
        }
        return model;
    }

    public setDeleted() {
        this._trackingState = TrackingState.Deleted;
    }
    public setSaved() {
        
    }

    private registerChanges() {
        this._notifyChanges = true;
        if (this.notifyChangeSub) {
            this.notifyChangeSub.unsubscribe();
        }
        this.notifyChangeSub = this.notifyChangeEvent.subscribe((name: NotifyChangeInfo) => {
            if (name == null) {
                return;
            }

            if (this._trackingState === TrackingState.Unchanged) {
                this._trackingState = TrackingState.Modified;
            }

            this._itemChanges.set(name.propertyName, name);
            this._isDirty = true;
        });

        this.afterRegisterChanges();
    }

    protected afterRegisterChanges() {

    }

    public commitChanges() {
        this.unregisterChanges();
        if (this._undoRedoEnabled) {
            this._originalJson = this.toJson();
            this.fromJson(this._originalJson, this.translationService);
        }

        this._isDirty = false;
        this.registerChanges();
        this._itemChanges.clear();
    }

    protected notifyChange(name: string) {
        this.notifyChangeEvent.emit({ propertyName: name, object: this });
        // if (!this._notifyChanges || !this._undoRedoEnabled) {
        //     return;
        // }
        // if (this._originalJson != null && this._originalJson.hasOwnProperty(name)) {
        //     const oldValue = this._originalJson[name];
        //     this.notifyChangeEvent.emit({ propertyName: name, object: this, newValue: this[name], oldValue: oldValue });
        // } else {
        //     this.notifyChangeEvent.emit({ propertyName: name, object: this, newValue: this[name], oldValue: undefined });
        // }
    }

    public get isDirty(): boolean {
        return this._isDirty;
    }

    public setDirty(): void {
        this._isDirty = true;
    }

    public get isNewObject(): boolean {
        return this._isNewObject;
    }
    public set isNewObject(value: boolean) {
        this._isNewObject = value;
    }


    private getBaseModelFromJson<T extends BaseModel>(json: any, translation?: L10nTranslationService): T {
        const baseModel = BaseModel.getBaseModelFromJson<T>(json, void 0, translation);
        return baseModel;
    }


    public fromJson(json: any, translation?: L10nTranslationService) {
        BaseModel.fromJson(json, this, translation);
    }

    protected useBaseModelInstanceForJson(baseMode: BaseModel): boolean {
        return true;
    }

    toJson(): { [name: string]: any } {
        const json = {};
        const jsonFields = BaseModel.getJsonField(this.typeInfo(), this);

        if (jsonFields === undefined) {
            console.error("No JsonProperties definied for this object");
            return;
        }
        json["TypeInfo"] = this._typeInfo;
        json["typeInfo"] = this._typeInfo;
        for (const key of Array.from(jsonFields.keys())) {
            const value = jsonFields.get(key);

            if (this[key] === void 0 || this[key] === null) { // ignore null values
                continue;
            }

            if (value.ignoreToServer) {
                continue;
            }

            if (this[key] instanceof BaseModel) {
                if (this.useBaseModelInstanceForJson(this[key])) {
                    json[value.name] = this[key].toJson();
                }
            } else if (this[key] instanceof Array) {
                const ar: Array<any> = this[key];
                const addArray: any[] = [];
                for (let i = 0; i < ar.length; i++) {
                    if (ar[i] instanceof BaseModel) {
                        if (this.useBaseModelInstanceForJson(ar[i])) {
                            addArray.push(ar[i].toJson());
                        }
                    } else {
                        addArray.push(ar[i]);
                    }
                }
                json[value.name] = addArray;
            } else if (this[key] instanceof Date) {
                const date: Date = this[key];
                json[value.name] = date.toISOString();
            } else {
                json[value.name] = this[key];
            }
        }



        return json;
    }


}
