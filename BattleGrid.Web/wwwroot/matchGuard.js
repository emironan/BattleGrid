window.battleGridMatchGuard = {
    setActive: function (active) {
        if (active) {
            window.onbeforeunload = function (e) {
                e.preventDefault();
                e.returnValue = "";
            };
        } else {
            window.onbeforeunload = null;
        }
    },
    confirmLeave: function () {
        return window.confirm("Leave this match? You will forfeit and the other player will win.");
    }
};
