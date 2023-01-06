import TeamColorLookup from '@/models/teamColorLookup';
export default class FeedItem {
    constructor(type, subType, animate, time, title, text, images, subtext, teamColor) {
        this.type = type;
        this.subType = subType;
        this.animate = animate;
        this.time = time;
        this.title = title;
        this.text = text;
        this.images = images;
        this.subtext = subtext;
        this.teamColor = teamColor;
    }
    static fromPlay(game, play) {
        let title = play.result.eventTypeId.replace("_", " ");
        let images = [
            require(`@/assets/img/logos/${game.gameData.teams.home.abbreviation}.png`),
            require(`@/assets/img/logos/${game.gameData.teams.away.abbreviation}.png`)
        ];
        let subtext = play.about.periodTime;
        let teamColor = null;
        //set title
        if (play.result.eventTypeId === "GOAL" /* Goal */) {
            const winningScore = Math.max(game.liveData.linescore.teams.away.goals, game.liveData.linescore.teams.home.goals);
            const losingScore = Math.min(game.liveData.linescore.teams.away.goals, game.liveData.linescore.teams.home.goals);
            if (winningScore === losingScore) {
                title = `${winningScore}-${losingScore} TIE`;
            }
            else {
                const homeTeamIsWinning = winningScore === game.liveData.linescore.teams.home.goals;
                title = homeTeamIsWinning
                    ? `${winningScore}-${losingScore} ${game.gameData.teams.home.abbreviation}`
                    : `${winningScore}-${losingScore} ${game.gameData.teams.away.abbreviation}`;
            }
        }
        //set image(s) and color
        if (play.result.eventTypeId === "PENALTY" /* Penalty */ || play.result.eventTypeId === "GOAL" /* Goal */) {
            const scoringPlay = play;
            const abbreviation = scoringPlay.team.id === game.gameData.teams.away.id
                ? game.gameData.teams.away.abbreviation
                : game.gameData.teams.home.abbreviation;
            let image = require(`@/assets/img/logos/${abbreviation}.png`);
            try {
                image = require(`@/assets/img/logos/${abbreviation}_LIGHT.png`);
            }
            catch { }
            images = [image];
            teamColor = TeamColorLookup[scoringPlay.team.id];
        }
        //set subtext
        if (play.about.periodTime === "20:00" || play.about.periodTime == "00:00") {
            subtext = "";
        }
        return new this(1 /* GameEvent */, play.result.eventTypeId, false, play.about.dateTime, title, play.result.description, images, subtext, teamColor);
    }
}
//# sourceMappingURL=feedItem.js.map