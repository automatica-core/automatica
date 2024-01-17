import { Appointment } from "devextreme/ui/scheduler";
import { BaseModel, JsonFieldInfo, JsonProperty, Model } from "./base-model"
import { L10nTranslationService } from "angular-l10n";


@Model()
export class CalendarPropertyData extends BaseModel {

    @JsonProperty()
    public Value: CalendarPropertyDataEntry[] = [];

    toJson(): { [name: string]: any } {

        var newValues = [];
        this.Value.forEach(a => {
            if (a instanceof CalendarPropertyDataEntry) {
                newValues.push(a);
            }
            else {
                var calendarPropertyDataEntry = new CalendarPropertyDataEntry();
                calendarPropertyDataEntry.fromJson(a);
                newValues.push(calendarPropertyDataEntry);
            }

        });
        this.Value = newValues;

        return super.toJson();
    }

    public fromJson(json: any, translation?: L10nTranslationService): void {
        super.fromJson(json, translation);
        var list = [];
        this.Value.forEach(a => {

            if (a instanceof CalendarPropertyDataEntry) {
                list.push(a);
            }
            else {
                var calendarPropertyDataEntry = new CalendarPropertyDataEntry();
                calendarPropertyDataEntry.fromJson(a);
                a = calendarPropertyDataEntry;
                list.push(calendarPropertyDataEntry);
            }

        });
        this.Value = list;
    }

    protected createInstance(): BaseModel {
        return new CalendarPropertyData();
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public typeInfo(): string {
        return "CalendarPropertyData";
    }


}

@Model()
export class CalendarPropertyDataEntry extends BaseModel {
    @JsonProperty()
    public Text: string;
    
    @JsonProperty()
    public Description: string;

    @JsonProperty()
    public StartDate: Date;

    @JsonProperty()
    public EndDate: Date;

    @JsonProperty()
    public RecurrenceRule: string;

    @JsonProperty()
    public RecurrenceException: string;
    
    @JsonProperty()
    public AllDay: boolean;

    @JsonProperty()
    public Disabled: boolean;

    constructor() {
        super();
        this.StartDate = new Date();
        this.EndDate = new Date();
        this.EndDate.setHours(this.StartDate.getHours() + 1);


    }

    protected afterFromJson() {

    }


    toJson(): { [name: string]: any } {

        return super.toJson();
    }

    protected createInstance(): BaseModel {
        return new CalendarPropertyData();
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public typeInfo(): string {
        return "CalendarPropertyDataEntry";
    }
}
