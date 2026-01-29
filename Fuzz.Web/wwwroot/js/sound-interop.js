
window.soundInterop = {
    synth: null,
    tune: null,

    // Renders ABC notation and prepares audio
    renderAbc: function (visualObjId, audioObjId, abcString) {
        console.log("DEBUG: renderAbc called. Checking ABCJS presence...");
        if (typeof ABCJS === "undefined") {
            console.error("ERROR: ABCJS is not defined at runtime!");
            return Promise.resolve(false);
        }

        // 1. Render Visual Sheet Music
        var visualObj = ABCJS.renderAbc(visualObjId, abcString, { responsive: "resize" });

        // 2. Setup Audio Synth
        if (ABCJS.synth && ABCJS.synth.supportsAudio()) {
            this.synth = new ABCJS.synth.CreateSynth();

            var audioContext = new (window.AudioContext || window.webkitAudioContext)();

            // Create a Promise to handle the async init
            return this.synth.init({
                visualObj: visualObj[0],
                audioContext: audioContext,
                millisecondsPerMeasure: visualObj[0].millisecondsPerMeasure()
            }).then(function () {
                // Audio loaded
                return this.synth.prime();
            }.bind(this)).then(function () {
                // Ready to play
                return true;
            });
        } else {
            console.warn("ABCJS.synth or audio support is not available in this version of abcjs.");
        }
        return Promise.resolve(false);
    },

    playAudio: function () {
        if (this.synth) {
            this.synth.start();
        }
    },

    stopAudio: function () {
        if (this.synth) {
            this.synth.stop();
        }
    },

    // Create a downloadable MIDI file
    createMidiDownload: function (abcString, containerId) {
        if (!ABCJS.synth) {
            console.warn("MIDI generation is not available (ABCJS.synth is missing).");
            return;
        }
        var midi = ABCJS.synth.getMidiFile(abcString, { midiOutputType: "encoded" });
        var container = document.getElementById(containerId);
        if (container) {
            container.href = midi;
            container.download = "generated_music.mid";
            container.style.display = "block";
        }
    }
};
