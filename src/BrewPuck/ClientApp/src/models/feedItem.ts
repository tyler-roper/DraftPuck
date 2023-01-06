import TeamColorLookup from '@/models/teamColorLookup';
import EventType from '@/models/nhlApi/enums/eventType';
import FeedItemType from '@/enums/feedItemType';
import PlayerType from '@/models/nhlApi/enums/playerType';
import LobbyEventType from '@/enums/lobbyEventType';
import GoalTexts from '@/models/goalTexts';
import DrinkAwardedTexts from '@/models/drinkAwardedTexts';
import TakeDrinkTexts from '@/models/takeDrinkTexts';
import '@/extensions/arrayExtensions';

export default class FeedItem {
    gamePk: number | null;
    type: FeedItemType;
    subType: EventType | LobbyEventType;
    animate: boolean;
    time: Date;
    title: string;
    text: string;
    subtext: string;
    teamColor: string | null;
    images: Array<string>;
    player: PlayerSummary | null;

    constructor(gamePk: number | null, type: FeedItemType, subType: EventType | LobbyEventType, animate: boolean, time: Date, title: string, text: string, subtext: string, teamColor: string | null, images: Array<string>, player: PlayerSummary | null) {
        this.gamePk = gamePk;
        this.type = type;
        this.subType = subType;
        this.animate = animate;
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

        return new this(gamePk, FeedItemType.GameEvent, play.result.eventTypeId, false, play.about.dateTime, title, text, subtext, teamColor, images, player);
    }

    static fromLobbyEvent(gamePk: number | null, lobbyEvent: LobbyEvent, lobby: Lobby, player: Player | null, team: Team | null) {
        let title = "";
        let text = "";

        const drink: Drink | null = lobby.members.flatMap(m => m.picks).flatMap(p => p.drinks).find(drink => drink.id === lobbyEvent.entityId) ?? null;
        let lobbyMemberPick: LobbyMemberPick | null = lobby.members.flatMap(m => m.picks).find(pick => pick.id === lobbyEvent.entityId) ?? null;
        let lobbyMember: LobbyMember | null = lobby.members.find(member => member.id === lobbyEvent.entityId) ?? null;

        if (drink != null && lobbyMemberPick == null) {
            lobbyMemberPick = lobby.members.flatMap(m => m.picks).find(pick => pick.drinks.find(d => d.id === drink.id)) ?? null
        }

        if (lobbyMemberPick != null && lobbyMember == null) {
            lobbyMember = lobby.members.find(member => member.id === lobbyMemberPick?.lobbyMemberId) ?? null;
        }

        if (lobbyEvent.type === LobbyEventType.UserJoined && lobbyMember) {
            title = "New Challenger";
            text = `&#x1F44B; Welcome <strong>${lobbyMember.name}</strong>!`;
        }

        if (lobbyEvent.type === LobbyEventType.UserNameChanged && lobbyMember) {
            title = "User Name Change";
            text = `New name: ${lobbyMember.name}`;
        }

        if (lobbyEvent.type === LobbyEventType.NewPick && lobbyMember && player) {
            title = "New Pick";
            let img = "";

            if (team) {
                let src = "";
                try {
                    src = require(`@/assets/img/logos/${team.abbreviation}_LIGHT.png`);
                } catch {
                    src = require(`@/assets/img/logos/${team.abbreviation}.png`);
                }
                img = `<img style='height: 27px; width: 27px; margin-left: -20px; margin-right: -1px; margin-top: -12px; margin-bottom: -10px;' src="${src}" />`;
                const teamColor = TeamColorLookup[team.id];
                text = `<div class='d-flex align-items-center'><strong class='d-block'>${lobbyMember.name}</strong> <i class="mb-n1 mx-2  fs-5 d-block fi fi-sr-arrow-right"></i> <span class='d-inline-block pl-3 ml-1 badge text-uppercase text-shadow' style='align-self: center; background-color: ${teamColor} !important;'>${img} ${player.lastName}</div>`;
            } else {
                text = `${lobbyMember.name} chooses <strong>${player.lastName}</strong>`;
            }
        }

        if (lobbyEvent.type === LobbyEventType.NewDrink) {
            title = "Drink Awarded";

            if (team && player) {
                let src = "";
                let img = "";
                try {
                    src = require(`@/assets/img/logos/${team.abbreviation}_LIGHT.png`);
                } catch {
                    src = require(`@/assets/img/logos/${team.abbreviation}.png`);
                }
                img = `<img style='height: 27px; width: 27px; margin-left: -20px; margin-right: -1px; margin-top: -12px; margin-bottom: -10px;' src="${src}" />`;
                const teamColor = TeamColorLookup[team.id];
                text = `${this.getRandomNewDrinkText(lobbyMember, lobbyEvent)} <span class='pl-3 ml-1 badge text-uppercase text-shadow' style='background-color: ${teamColor} !important;'>${img} ${player.lastName}</span>`;
            } else {
                text = `${lobbyMember.name} chooses correctly and gets to give out a drink!`;
            }
        }

        if (lobbyEvent.type === LobbyEventType.DrinkAssigned) {
            const drink = lobby.members.flatMap(m => m.picks).flatMap(p => p.drinks).find(drink => drink.id === lobbyEvent.entityId) ?? null;
            const pick = lobby.members.flatMap(m => m.picks).find(pick => pick.drinks.some(d => d.id === drink?.id)) ?? null;
            const assigningMember = lobby.members.find(member => member.id === pick?.lobbyMemberId) ?? null;
            const recipientMember = lobby.members.find(member => member.id === drink?.recipientLobbyMemberId) ?? null;

            title = "Drink Up!";
            if (assigningMember && recipientMember && player) {
                text = this.getRandomTakeDrinkText(assigningMember, recipientMember, lobbyEvent);
            }
        }

        return new this(gamePk, FeedItemType.LobbyEvent, lobbyEvent.type, false, lobbyEvent.time, title, text, "", null, [], null);
    }

    private static getRandomGoalText(playerName: string, scoringPlay: ScoringPlay): string {
        const seed = Math.floor(Number(scoringPlay.about.periodTime.split(":")[1]));
        const randomString = GoalTexts.seed(seed);
        const replaced = randomString.replace("{{player}}", `<strong>${playerName}</strong>`);

        return `&#x1F6A8; ${replaced}!`;
    }

    private static getRandomNewDrinkText(lobbyMember: LobbyMember, lobbyEvent: LobbyEvent): string {
        const d = ""+Number(lobbyEvent.time);
        const seed = Number(d[d.length - 1]);
        const randomString = DrinkAwardedTexts.seed(seed);
        return randomString.replace("{{member}}", `<strong>${lobbyMember.name}</strong>`);
    }

    private static getRandomTakeDrinkText(assigningMember: LobbyMember, recipientMember: LobbyMember, lobbyEvent: LobbyEvent): string {
        const d = "" + Number(lobbyEvent.time);
        const seed = Number(d[d.length - 1]);
        let randomString = TakeDrinkTexts.seed(seed);
        randomString = randomString.replace("{{sender}}", `<strong>${assigningMember.name}</strong>`);
        return randomString.replace("{{recipient}}", `<strong>${recipientMember.name}</strong>`);
    }
}