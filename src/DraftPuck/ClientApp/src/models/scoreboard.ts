import format from 'date-fns/format';
import GameStatus from '@/models/nhlApi/enums/gameStatusCode';

export default class Scoreboard {
    gamePk: number;
    time: string;
    linescore: LineScore;
    boxscore: BoxScore;
    gameStatus: GameStatus;
    teams: { home: Team; away: Team };
    players: Array<Player>;
    startTime: Date;

    constructor(gamePk: number, time: string, linescore: LineScore, boxscore: BoxScore, gameStatus: GameStatus, teams: { home: Team; away: Team }, players: Array<Player>, startTime: Date) {
        this.gamePk = gamePk;
        this.time = time;
        this.linescore = linescore;
        this.boxscore = boxscore;
        this.gameStatus = gameStatus;
        this.teams = teams;
        this.players = players;
        this.startTime = startTime;
    }

    get isInProgress() {
        return [GameStatus.InProgress, GameStatus.InProgressCritical].includes(this.gameStatus);
    }

    get isOver() {
        return [GameStatus.Final, GameStatus.Final2, GameStatus.GameOver].includes(this.gameStatus);
    }

    get isStarted() {
        return this.isInProgress || this.isOver;
    }

    static fromLiveGame(game: LiveGame) {
        const status = game.gameData.status.statusCode;
        let time = format(game.gameData.datetime.dateTime, "p");

        const gameIsInProgress = [GameStatus.InProgress, GameStatus.InProgressCritical].includes(status);
        const gameIsOver = [GameStatus.Final, GameStatus.Final2, GameStatus.GameOver].includes(status);

        if (gameIsInProgress) {
            const timeRemaining = game.liveData.linescore.currentPeriodTimeRemaining;
            const ordinal = game.liveData.linescore.currentPeriodOrdinal;

            if (!timeRemaining.includes(":")) {
                time = `${timeRemaining} - ${ordinal}`;
            } else {
                const timeParts = timeRemaining.split(":");
                const timeWithoutLeadingZero = `${Number(timeParts[0])}:${timeParts[1]}`;
                time = `${timeWithoutLeadingZero} - ${ordinal}`;
            }
        } else if (status === GameStatus.Postponed) {
            time = "Postponed";
        } else if (gameIsOver) { 
            time = "Final";
            if (game.liveData.linescore.currentPeriod > 3)
                time += ` (${game.liveData.linescore.currentPeriodOrdinal})`;
        }

        return new this(game.gamePk, time, game.liveData.linescore, game.liveData.boxscore, status, game.gameData.teams, Object.values(game.gameData.players), game.gameData.datetime.dateTime);
    } 
}