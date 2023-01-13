import TeamColorLookup from '@/models/teamColorLookup';
import EventType from '@/models/nhlApi/enums/eventType';
import FeedItemType from '@/enums/feedItemType';
import PlayerType from '@/models/nhlApi/enums/playerType';
import LobbyEventType from '@/enums/lobbyEventType';
import GoalTexts from '@/models/goalTexts';
import '@/extensions/arrayExtensions';

export default class FeedItem {
    gamePk: number | null;
    type: FeedItemType;
    subType: EventType | LobbyEventType;
    time: Date;
    title: string;
    text: string;
    subtext: string;
    teamColor: string | null;
    images: Array<string>;
    player: PlayerSummary | null;

    constructor(gamePk: number | null, type: FeedItemType, subType: LobbyEventType, time: Date, title: string, text: string, subtext: string, teamColor: string | null, images: Array<string>, player: PlayerSummary | null) {
        this.gamePk = gamePk;
        this.type = type;
        this.subType = subType;
        this.time = time;
        this.title = title;
        this.text = text;
        this.subtext = subtext;
        this.teamColor = teamColor;
        this.images = images;
        this.player = player;
    }

    get isGoal() {
        return this.subType === EventType.Goal;
    }

    get isPenalty() {
        return this.subType === EventType.Penalty;
    }

    get isScoringPlay() {
        return this.isPenalty || this.isGoal;
    }

    static fromPlay(gamePk: number, lineScoreTeams: { home: LineScoreTeam; away: LineScoreTeam }, play: Play) {
        const homeAbbreviation = lineScoreTeams.home.team.abbreviation;
        const awayAbbreviation = lineScoreTeams.away.team.abbreviation;

        let title = play.result.eventTypeId.replace("_", " ");
        let subtext = `${play.about.periodTime} ${play.about.ordinalNum}`;
        let teamColor: string | null = null;
        let player: PlayerSummary | null = null;
        let images: Array<string> = [
            `${homeAbbreviation}.png`,
            `${awayAbbreviation}.png`
        ];
        let text = play.result.description;

        //set color, player, image
        if (play.result.eventTypeId === EventType.Penalty || play.result.eventTypeId === EventType.Goal) {
            const scoringPlay = play as ScoringPlay;
            teamColor = TeamColorLookup[scoringPlay.team.id];

            const primaryPlayer = scoringPlay.players.find(player => player.playerType === PlayerType.Scorer || player.playerType === PlayerType.PenaltyOn);
            if (primaryPlayer) player = primaryPlayer.player;

            const scoringTeamAbbreviation = scoringPlay.team.id === lineScoreTeams.home.team.id
                ? homeAbbreviation
                : awayAbbreviation;

            if (play.result.eventTypeId === EventType.Goal && scoringTeamAbbreviation.toLowerCase() === "tbl")
                images = [`${scoringTeamAbbreviation}_LIGHT.png`];
            else
                images = [`${scoringTeamAbbreviation}.png`];
        }

        //set title
        if (play.result.eventTypeId === EventType.Goal) {
            const winningScore = Math.max(play.about.goals.away, play.about.goals.home);
            const losingScore = Math.min(play.about.goals.away, play.about.goals.home);

            if (winningScore === losingScore) {
                title = `${winningScore}-${losingScore} TIE`;
            } else {
                const homeTeamIsWinning = winningScore === play.about.goals.home;
                title = homeTeamIsWinning
                    ? `${winningScore}-${losingScore} ${homeAbbreviation}`
                    : `${winningScore}-${losingScore} ${awayAbbreviation}`;
            }

            if (player != null)
                text = this.getRandomGoalText(player.fullName, play as ScoringPlay);
            else
                text = "Scorer not yet assigned...";
        }

        if (play.result.eventTypeId === EventType.Challenge) {
            const challengePlay = play as ScoringPlay;
            if (challengePlay.team?.id) {
                const challengingTeamAbbreviation = challengePlay.team.id === lineScoreTeams.home.team.id
                    ? homeAbbreviation
                    : awayAbbreviation;

                text = `${challengingTeamAbbreviation} are challenging the play.`;
                images = [`${challengingTeamAbbreviation}.png`];
            }
        }

        //set subtext
        if (play.about.periodTime === "20:00" || play.about.periodTime == "00:00") {
            subtext = "";
        }

        if (play.result.eventTypeId === EventType.PeriodStart) {
            text = `Start of ${play.about.ordinalNum} Period`;
        }

        if (play.result.eventTypeId === EventType.GameEnd) {
            const winningScore = Math.max(play.about.goals.away, play.about.goals.home);
            const losingScore = Math.min(play.about.goals.away, play.about.goals.home);
            const homeTeamIsWinning = winningScore === play.about.goals.home;
            text = homeTeamIsWinning
                ? `${homeAbbreviation} wins ${winningScore}-${losingScore}`
                : `${awayAbbreviation} wins ${winningScore}-${losingScore}`;
        }

        return new this(gamePk, FeedItemType.GameEvent, play.result.eventTypeId, play.about.dateTime, title, text, subtext, teamColor, images, player);
    }

    static fromLobbyEvent(lobbyEvent: LobbyEvent) {
        return new this(lobbyEvent.gamePk, FeedItemType.LobbyEvent, lobbyEvent.lobbyEventType, lobbyEvent.timeUtc, lobbyEvent.title, lobbyEvent.text, lobbyEvent.subtext, lobbyEvent.teamColor, [], null);
    }

    private static getRandomGoalText(playerName: string, scoringPlay: ScoringPlay): string {
        const seed = Math.floor(Number(scoringPlay.about.periodTime.split(":")[1]));
        const randomString = GoalTexts.seed(seed);
        const replaced = randomString.replace("{{player}}", `<strong>${playerName}</strong>`);

        return `&#x1F6A8; ${replaced}!`;
    }
}