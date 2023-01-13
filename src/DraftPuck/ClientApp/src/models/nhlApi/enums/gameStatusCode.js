var GameStatusCode;
(function (GameStatusCode) {
    GameStatusCode["Scheduled"] = "1";
    GameStatusCode["PreGame"] = "2";
    GameStatusCode["InProgress"] = "3";
    GameStatusCode["InProgressCritical"] = "4";
    GameStatusCode["GameOver"] = "5";
    GameStatusCode["Final"] = "6";
    GameStatusCode["Final2"] = "7";
    GameStatusCode["ScheduledTimeTBD"] = "8";
    GameStatusCode["Postponed"] = "9";
})(GameStatusCode || (GameStatusCode = {}));
export default GameStatusCode;
//# sourceMappingURL=gameStatusCode.js.map