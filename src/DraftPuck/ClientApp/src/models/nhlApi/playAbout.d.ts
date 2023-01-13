interface PlayAbout {
    eventIdx: number;
    eventId: number;
    period: number;
    periodType: PeriodType;
    ordinalNum: string;
    periodTime: string;
    periodTimeRemaining: string;
    dateTime: Date;
    goals: { away: number; home: number; }
}