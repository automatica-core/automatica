import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"


export enum DayOfWeek {
    Sunday = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 3,
    Thursday = 4,
    Friday = 5,
    Saturday = 6
}

interface WeekDay {
    dayOfWeek: DayOfWeek;
    checked: boolean;
    name: string;
}

@Model()
export class TimerPropertyData extends BaseModel {

    @JsonProperty()
    public StartTime: Date;

    @JsonProperty()
    public StopTime: Date;

    private _EnabledDays: DayOfWeek[] = [];
    @JsonProperty()
    public get EnabledDays(): DayOfWeek[] {
        return this._EnabledDays;
    }
    public set EnabledDays(v: DayOfWeek[]) {
        this._EnabledDays = v;
    }

    public WeekDaysArray: WeekDay[] = [];

    constructor() {
        super();
        this.StartTime = new Date();
        this.StopTime = new Date();
        this.StopTime.setHours(this.StartTime.getHours() + 1);

        this.WeekDaysArray.push({
            checked: true,
            dayOfWeek: DayOfWeek.Monday,
            name: "MONDAY"
        });
        this.WeekDaysArray.push({
            checked: true,
            dayOfWeek: DayOfWeek.Tuesday,
            name: "TUESDAY"
        });
        this.WeekDaysArray.push({
            checked: true,
            dayOfWeek: DayOfWeek.Wednesday,
            name: "WEDNESDAY"
        });
        this.WeekDaysArray.push({
            checked: true,
            dayOfWeek: DayOfWeek.Thursday,
            name: "THURSDAY"
        });
        this.WeekDaysArray.push({
            checked: true,
            dayOfWeek: DayOfWeek.Friday,
            name: "FRIDAY"
        });
        this.WeekDaysArray.push({
            checked: true,
            dayOfWeek: DayOfWeek.Saturday,
            name: "SATURDAY"
        });
        this.WeekDaysArray.push({
            checked: true,
            dayOfWeek: DayOfWeek.Sunday,
            name: "SUNDAY"
        });
    }
    protected afterFromJson() {
        for (const x of this._EnabledDays) {
            this.WeekDaysArray.find(a => a.dayOfWeek === x).checked = true;
        }
    }


    toJson(): { [name: string]: any } {

        this._EnabledDays = [];
        for (const x of this.WeekDaysArray) {
            if (x.checked) {
                this._EnabledDays.push(x.dayOfWeek);
            }
        }

        return super.toJson();
    }

    protected createInstance(): BaseModel {
        return new TimerPropertyData();
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public typeInfo(): string {
        return "TimerPropertyData";
    }
}
