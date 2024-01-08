interface PlayerStatsResponse {
    copyright: string;
    stats: Array<Stat>;
}

interface Stat {
    type: StatType;
    splits: Array<StatSplit>;
}

interface StatType {
    displayName: string;
    gameType: StatGameType;
}

interface StatGameType {
    id: string;
    description: string;
    postseason: boolean;
}

interface StatSplit {
    season: string;
    stat: PlayerSeasonStats;
}
