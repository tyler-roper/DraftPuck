interface LiveGame {
    copyright: string;
    gamePk: number;
    link: string;
    metaData: Metadata;
    gameData: GameData;
    liveData: LiveData;
}

interface GameData {
    game: GameIdentifier;
    datetime: { dateTime: Date; endDateTime: Date; }
    status: GameStatus;
    teams: { away: Team; home: Team; };
    players: { [key: string]: Player; };
    venue: VenueSummary;
}

interface GameIdentifier {
    pk: number;
    season: string;
    type: GameType;
}

interface Team {
    id: number;
    name: string;
    link: string;
    venue: Venue;
    abbreviation: string;
    triCode: string;
    teamName: string;
    locationName: string;
    firstYearOfPlay: string;
    division: Division;
    conference: Conference;
    franchise: Franchise;
    shortName: string;
    officialSiteUrl: string;
    franchiseId: number;
    active: boolean;
}

interface Division {
    id: number;
    name: string;
    nameShort: string;
    link: string;
    abbreviation: string;
}

interface Conference {
    id: number;
    name: string;
    link: string;
}

interface Franchise {
    franchiseId: number;
    teamName: string;
    link: string;
}

interface Venue {
    id: number;
    name: string;
    link: string;
    city: string;
    timeZone: TimeZone;
}

interface TimeZone {
    id: string;
    offset: number;
    tz: string;
}

interface Player {
    id: number;
    fullName: string;
    link: string;
    firstName: string;
    lastName: string;
    primaryNumber: string;
    birthDate: string;
    currentAge: number;
    birthCity: string;
    birthStateProvince: string;
    birthCountry: string;
    nationality: string;
    height: string;
    weight: number;
    active: boolean;
    alternateCaptain: boolean;
    captain: boolean;
    rookie: boolean;
    shootsCatches: "L" | "R";
    rosterStatus: string;
    currentTeam: TeamSummary;
    primaryPosition: Position;
}

interface Position {
    code: string;
    name: string;
    type: string;
    abbreviation: string;
}

interface LiveData {
    plays: Plays;
    linescore: LineScore;
    boxscore: BoxScore;
    decisions: Decisions;
}

interface Decisions {
    winner: PlayerSummary;
    loser: PlayerSummary;
    firstStar: PlayerSummary;
    secondStar: PlayerSummary;
    thirdStar: PlayerSummary;
}

interface Plays {
    allPlays: Array<Play>;
    scoringPlays: Array<number>;
    penaltyPlays: Array<number>;
    playsByPeriod: Array<{ startIndex: number; plays: Array<Number>; endIndex: number; }>;
    currentPlay: Play;
}

interface LineScore {
    currentPeriod: number;
    currentPeriodOrdinal: string;
    currentPeriodTimeRemaining: string;
    periods: Array<Period>;
    shootoutInfo: ShootoutInfo;
    teams: { home: LineScoreTeam; away: LineScoreTeam; };
    powerPlayStrength: string;
    hasShootout: boolean;
    intermissionInfo: { intermissionTimeRemaining: number; intermissionTimeElapsed: number; inIntermission: boolean; }
    powerPlayInfo: { situationTimeRemaining: number; situationTimeElapsed: number; inSituation: boolean; }
}

interface LineScoreTeam {
    team: ExtendedTeamSummary;
    goals: number;
    shotsOnGoal: number;
    goaliePulled: boolean;
    numSkaters: number;
    powerPlay: boolean;
}

interface ExtendedTeamSummary extends TeamSummary {
    abbreviation: string;
    triCode: string;
}

interface Period {
    periodType: PeriodType;
    startTime: string;
    endTime: string;
    num: number;
    ordinalNum: string;
    home: { goals: number; shotsOnGoal: number; rinkSide: string; }
    away: { goals: number; shotsOnGoal: number; rinkSide: string; }
}

interface ShootoutInfo {
    away: { scores: number; attempts: number; }
    home: { scores: number; attempts: number; }
}

interface BoxScore {
    teams: { away: BoxScoreTeam; home: BoxScoreTeam; }
    officials: Array<Official>;
}

interface BoxScoreTeam {
    team: ExtendedTeamSummary;
    teamStats: { teamSkaterStats: TeamSkaterStats };
    players: { [key: string]: BoxScorePlayer; };
    goalies: Array<number>;
    skaters: Array<number>;
    onIce: Array<number>;
    onIcePlus: Array<{ playerId: number; shiftDuration: number; stamina: number; }>;
    scratches: Array<number>;
    penaltyBox: Array<number>;
    coaches: Array<Coach>;
}

interface Official {
    official: OfficialSummary;
    officialType: "Referee" | "Linesman";
}

interface OfficialSummary {
    id: number;
    fullName: string;
    link: string;
}

interface Coach {
    person: { fullName: string; link: string; }
    position: Position;
}

interface TeamSkaterStats {
    goals: number;
    pim: number;
    shots: number;
    powerPlayPercentage: string;
    powerPlayGoals: number;
    powerPlayOpportunities: number;
    faceOffWinPercentage: string;
    blocked: number;
    takeaways: number;
    giveaways: number;
    hits: number;
}

interface BoxScorePlayer {
    person: PlayerPerson;
    jerseyNumber: string;
    position: Position;
    stats: { skaterStats?: SkaterStats; goalieStats?: GoalieStats }
}

interface PlayerPerson {
    id: number
    fullName: string;
    link: string;
    shootsCatches: "L" | "R";
    rosterStatus: string;
}

interface SkaterStats {
    timeOnIce: string;
    assists: number;
    goals: number;
    shots: number;
    hits: number;
    powerPlayGoals: number;
    powerPlayAssists: number;
    penaltyMinutes: number;
    faceOffPct: number;
    faceOffWins: number
    faceoffTaken: number;
    takeaways: number;
    giveaways: number;
    shortHandedGoals: number;
    shortHandedAssists: number;
    blocked: number;
    plusMinus: number;
    evenTimeOnIce: string;
    powerPlayTimeOnIce: string;
    shortHandedTimeOnIce: string;
}

interface GoalieStats {
    timeOnIce: string;
    assists: number;
    goals: number;
    pim: number;
    shots: number;
    saves: number;
    powerPlaySaves: number;
    shortHandedSaves: number;
    evenSaves: number;
    shortHandedShotsAgainst: number;
    evenShotsAgainst: number;
    powerPlayShotsAgainst: number;
    decision: string;
    savePercentage: number;
    powerPlaySavePercentage: number;
    shortHandedSavePercentage: number;
    evenStrengthSavePercentage: number;
}