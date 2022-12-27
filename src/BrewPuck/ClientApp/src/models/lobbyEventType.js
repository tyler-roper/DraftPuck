"use strict";
var LobbyEventType;
(function (LobbyEventType) {
    LobbyEventType[LobbyEventType["UserJoined"] = 0] = "UserJoined";
    LobbyEventType[LobbyEventType["UserRejoined"] = 1] = "UserRejoined";
    LobbyEventType[LobbyEventType["UserLeft"] = 2] = "UserLeft";
    LobbyEventType[LobbyEventType["UserNewPick"] = 3] = "UserNewPick";
    LobbyEventType[LobbyEventType["UserPickScored"] = 4] = "UserPickScored";
    LobbyEventType[LobbyEventType["UserNameChanged"] = 5] = "UserNameChanged";
    LobbyEventType[LobbyEventType["GoalChanged"] = 6] = "GoalChanged";
})(LobbyEventType || (LobbyEventType = {}));
//# sourceMappingURL=lobbyEventType.js.map