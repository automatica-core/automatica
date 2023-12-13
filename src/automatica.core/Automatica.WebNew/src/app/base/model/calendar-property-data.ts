import { Appointment } from "devextreme/ui/scheduler";
import { BaseModel, JsonFieldInfo, JsonProperty, Model } from "./base-model"
import { L10nTranslationService } from "angular-l10n";

export interface DxAppointment extends Appointment {
    allDay: boolean;
    description: string;
    disabled: boolean;
    endDate: Date | string;
    recurrenceException: string;
    recurrenceRule: string;
    startDate: Date | string;
    text: string;
}

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
export class CalendarPropertyDataEntry extends BaseModel implements DxAppointment {
    @JsonProperty()
    public Text: string;
    
    @JsonProperty()
    public Description: string;

    @JsonProperty()
    public StartDate: Date | string;

    @JsonProperty()
    public EndDate: Date | string;

    @JsonProperty()
    public RecurrenceRule: string;

    @JsonProperty()
    public RecurrenceException: string;
    
    @JsonProperty()
    public AllDay: boolean;

    @JsonProperty()
    public Disabled: boolean;

    public get text(): string {
        return this.Text;
    }
    public set text(value: string) {
        this.Text = value;
    }
    public get endDate(): Date | string {
        return this.EndDate;
    }
    public set endDate(value: Date | string) {
        this.EndDate = value;
    }
    public get startDate(): Date | string {
        return this.StartDate;
    }
    public set startDate(value: Date | string) {
        this.StartDate = value;
    }
    public get recurrenceRule(): string {
        return this.RecurrenceRule;
    }
    public set recurrenceRule(value: string) {
        this.RecurrenceRule = value;
    }
    public get recurrenceException(): string {
        return this.RecurrenceException;
    }
    public set recurrenceException(value: string) {
        this.RecurrenceException = value;
    }
    
    public get allDay(): boolean {
        return this.AllDay;
    }
    public set allDay(value: boolean) {
        this.AllDay = value;
    }

    
    public get disabled(): boolean {
        return this.Disabled;
    }
    public set disabled(value: boolean) {
        this.Disabled = value;
    }

    
    public get description(): string {
        return this.Description;
    }
    public set description(value: string) {
        this.Description = value;
    }


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
