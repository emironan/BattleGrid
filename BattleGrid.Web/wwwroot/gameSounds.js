window.battleGridSounds = {
    playWarning: function () {
        try {
            const Ctx = window.AudioContext || window.webkitAudioContext;
            if (!Ctx) return;
            const ctx = new Ctx();
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();
            osc.type = "sine";
            osc.frequency.value = 380;
            gain.gain.value = 0.06;
            osc.connect(gain);
            gain.connect(ctx.destination);
            osc.start();
            const stopAt = ctx.currentTime + 0.12;
            gain.gain.exponentialRampToValueAtTime(0.0001, stopAt);
            osc.stop(stopAt + 0.02);
            setTimeout(function () {
                try {
                    ctx.close();
                } catch (e) { }
            }, 400);
        } catch (e) { }
    }
};
