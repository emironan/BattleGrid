window.battleGridQueueSession = {
    setIntent: function () {
        try {
            sessionStorage.setItem('bg_queueIntent', '1');
        } catch (e) { /* private mode */ }
    },
    clearIntent: function () {
        try {
            sessionStorage.removeItem('bg_queueIntent');
        } catch (e) { }
    },
    hasIntent: function () {
        try {
            return sessionStorage.getItem('bg_queueIntent') === '1';
        } catch (e) {
            return false;
        }
    }
};