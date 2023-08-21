
export enum AggregationType {
    Raw = "raw",
    Hourly = "hourly",
    Daily = "daily",
    Weekly = "weekly",
    Monthly = "monthly",
    Yearly = "yearly"
}

export interface AggregatedValueRecord {
   
    Timestamp: Date;
    AvergeValue: number;
    DifferenceValue: number;
    MaxValue: number;
    MinValue: number;
    Count: number;
    NodeInstanceId: string;
}