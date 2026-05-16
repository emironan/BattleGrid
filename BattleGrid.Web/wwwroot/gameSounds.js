// One shared AudioContext: browsers suspend new contexts until a user gesture; we resume() before each cue
// and listen once for pointer/key to unlock early (e.g. after "Join queue" click).
(function () {
    var sharedCtx = null;

    function getCtx() {
        if (sharedCtx) return sharedCtx;
        var Ctx = window.AudioContext || window.webkitAudioContext;
        if (!Ctx) return null;
        sharedCtx = new Ctx();
        return sharedCtx;
    }

    function unlock() {
        var c = getCtx();
        if (c && c.state === "suspended")
            c.resume().catch(function () { });
    }

    document.addEventListener("pointerdown", unlock, { capture: true, passive: true });
    document.addEventListener("touchstart", unlock, { capture: true, passive: true });
    document.addEventListener("keydown", unlock, { capture: true, passive: true });

    /** Run playFn(ctx, t0) once context is running (async resume if needed). */
    function withRunningContext(playFn) {
        try {
            var c = getCtx();
            if (!c) return;
            var run = function () {
                try {
                    if (c.state !== "running") return;
                    playFn(c, c.currentTime);
                } catch (e) { }
            };
            if (c.state === "running")
                run();
            else
                c.resume().then(run).catch(function () { run(); });
        } catch (e) { }
    }

    window.battleGridSounds = {
        playWarning: function () {
            withRunningContext(function (ctx, t0) {
                var osc = ctx.createOscillator();
                var gain = ctx.createGain();
                osc.type = "sine";
                osc.frequency.value = 380;
                gain.gain.value = 0.08;
                osc.connect(gain);
                gain.connect(ctx.destination);
                osc.start(t0);
                var stopAt = t0 + 0.12;
                gain.gain.exponentialRampToValueAtTime(0.0001, stopAt);
                osc.stop(stopAt + 0.02);
            });
        },

        // Short ascending triad - match found / accepted.
        playMatchFound: function () {
            withRunningContext(function (ctx, t0) {
                function tone(freq, start, dur, vol) {
                    var osc = ctx.createOscillator();
                    var g = ctx.createGain();
                    osc.type = "sine";
                    osc.frequency.value = freq;
                    g.gain.value = vol;
                    osc.connect(g);
                    g.connect(ctx.destination);
                    osc.start(start);
                    var end = start + dur;
                    g.gain.exponentialRampToValueAtTime(0.0001, end);
                    osc.stop(end + 0.03);
                }
                tone(523.25, t0, 0.11, 0.12);
                tone(659.25, t0 + 0.09, 0.12, 0.12);
                tone(783.99, t0 + 0.2, 0.18, 0.13);
            });
        },

        // Victory: bright ascending motif.
        playMatchVictory: function () {
            withRunningContext(function (ctx, t0) {
                function tone(freq, start, dur, vol) {
                    var osc = ctx.createOscillator();
                    var g = ctx.createGain();
                    osc.type = "sine";
                    osc.frequency.value = freq;
                    g.gain.value = vol;
                    osc.connect(g);
                    g.connect(ctx.destination);
                    osc.start(start);
                    var end = start + dur;
                    g.gain.exponentialRampToValueAtTime(0.0001, end);
                    osc.stop(end + 0.03);
                }
                tone(523.25, t0 + 0, 0.14, 0.11);
                tone(659.25, t0 + 0.1, 0.14, 0.11);
                tone(783.99, t0 + 0.2, 0.14, 0.12);
                tone(1046.5, t0 + 0.31, 0.28, 0.13);
            });
        },

        // Defeat: slow descending tones.
        playMatchDefeat: function () {
            withRunningContext(function (ctx, t0) {
                function tone(freq, start, dur, vol) {
                    var osc = ctx.createOscillator();
                    var g = ctx.createGain();
                    osc.type = "triangle";
                    osc.frequency.value = freq;
                    g.gain.value = vol;
                    osc.connect(g);
                    g.connect(ctx.destination);
                    osc.start(start);
                    var end = start + dur;
                    g.gain.exponentialRampToValueAtTime(0.0001, end);
                    osc.stop(end + 0.03);
                }
                tone(415.3, t0, 0.22, 0.105);
                tone(311.13, t0 + 0.18, 0.28, 0.095);
                tone(246.94, t0 + 0.38, 0.42, 0.088);
            });
        },

        // Brief low thud - open water / miss.
        playShotMiss: function () {
            withRunningContext(function (ctx, t0) {
                function tone(type, freq, start, dur, vol) {
                    var osc = ctx.createOscillator();
                    var g = ctx.createGain();
                    osc.type = type;
                    osc.frequency.setValueAtTime(freq, start);
                    g.gain.setValueAtTime(vol, start);
                    osc.connect(g);
                    g.connect(ctx.destination);
                    osc.start(start);
                    var end = start + dur;
                    g.gain.exponentialRampToValueAtTime(0.0001, end);
                    osc.stop(end + 0.02);
                }
                tone("sine", 195, t0, 0.085, 0.11);
                tone("triangle", 95, t0 + 0.012, 0.14, 0.09);
            });
        },

        // Short metallic impact - hull hit (including sinking shot).
        playShotHit: function () {
            withRunningContext(function (ctx, t0) {
                function oscHit(type, freq, start, dur, vol) {
                    var osc = ctx.createOscillator();
                    var g = ctx.createGain();
                    osc.type = type;
                    osc.frequency.value = freq;
                    g.gain.setValueAtTime(vol, start);
                    osc.connect(g);
                    g.connect(ctx.destination);
                    osc.start(start);
                    var end = start + dur;
                    g.gain.exponentialRampToValueAtTime(0.0001, end);
                    osc.stop(end + 0.02);
                }
                oscHit("triangle", 640, t0, 0.065, 0.12);
                oscHit("triangle", 950, t0 + 0.004, 0.052, 0.06);
                oscHit("sine", 330, t0 + 0.018, 0.08, 0.06);
            });
        }
    };
})();
