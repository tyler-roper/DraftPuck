var LobbyEventType;
(function (LobbyEventType) {
    LobbyEventType[LobbyEventType["LobbyCreated"] = 0] = "LobbyCreated";
    LobbyEventType[LobbyEventType["UserJoined"] = 1] = "UserJoined";
    LobbyEventType[LobbyEventType["UserNameChanged"] = 2] = "UserNameChanged";
    LobbyEventType[LobbyEventType["NewPick"] = 3] = "NewPick";
    LobbyEventType[LobbyEventType["DrinkAwarded"] = 4] = "DrinkAwarded";
    LobbyEventType[LobbyEventType["DrinkAssigned"] = 5] = "DrinkAssigned";
    LobbyEventType[LobbyEventType["DrinkInvalidated"] = 6] = "DrinkInvalidated";
    LobbyEventType[LobbyEventType["DrinkRevoked"] = 7] = "DrinkRevoked";
    LobbyEventType[LobbyEventType["GoalChanged"] = 8] = "GoalChanged";
})(LobbyEventType || (LobbyEventType = {}));
export default LobbyEventType;
//# sourceMappingURL=lobbyEventType.js.map