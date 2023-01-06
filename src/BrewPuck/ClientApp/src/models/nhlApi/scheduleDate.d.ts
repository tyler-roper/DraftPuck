interface ScheduleDate {
    date: string;
    totalItems: number;
    totalEvents: number;
    totalGames: number;
    totalMatches: number;
    games: Array<GameSummary>;
}