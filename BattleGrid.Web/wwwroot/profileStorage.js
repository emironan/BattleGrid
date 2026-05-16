(function () {
    var KEY_SELF_ID = "battleGrid.selfUserId";
    var KEY_SELF_NAME = "battleGrid.selfDisplayName";


    window.battleGridProfile = {
        /** Persists only the signed-in user's display name (not opponents). */
        setSelf: function (userId, displayName) {
            if (userId == null || displayName == null) return;
            var s = String(displayName).trim();
            if (!s) return;
            try {
                localStorage.setItem(KEY_SELF_ID, String(userId));
                localStorage.setItem(KEY_SELF_NAME, s);
            } catch { }
        },
        /** Returns cached display name only when userId matches the last setSelf user. */
        getUserName: function (userId) {
            if (userId == null) return null;
            try {
                var selfId = localStorage.getItem(KEY_SELF_ID);
                if (selfId == null || String(userId) !== selfId) return null;
                var n = localStorage.getItem(KEY_SELF_NAME);
                var t = n == null ? "" : String(n).trim();
                return t ? t : null;
            } catch {
                return null;
            }
        },
        clear: function () {
            try {
                localStorage.removeItem(KEY_SELF_ID);
                localStorage.removeItem(KEY_SELF_NAME);
            } catch { }
        }
    };
})();
