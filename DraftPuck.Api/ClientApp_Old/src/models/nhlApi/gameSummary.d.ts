interface GameSummary {
    gamePk: number;
    link: string;
    gameType: GameType;
    season: string;
    gameDate: Date;
    status: GameStatus;
    teams: { away: GameTeam; home: GameTeam; }
    scoringPlays: Array<ScoringPlay>
    venue: VenueSummary;
    content: Content;
}