interface Schedule {
    totalItems: number;
    totalEvents: number;
    totalGames: number;
    totalMatches: number;
    metaData: Metadata;
    wait: number;
    dates: Array<ScheduleDate>;
}