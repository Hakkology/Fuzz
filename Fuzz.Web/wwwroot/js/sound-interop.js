
window.soundInterop = {
    synth: null,
    audioContext: null,
    currentProgram: 0,

    // Sanitize ABC notation and inject MIDI program for instrument
    sanitizeAbc: function (abcString, midiProgram) {
        if (!abcString) return "";

        // Extract only the first tune block (X:1)
        var tunes = abcString.split(/(?=X:\d+)/);
        var firstTune = tunes.find(t => t.trim().startsWith("X:"));
        if (!firstTune) return abcString;

        var cleaned = firstTune
            .replace(/[A-G]Sus\d/gi, function (match) { return match[0]; })
            .replace(/\s*---\s*/g, " ")
            .replace(/\|\|\s*---\s*\|\|/g, "|]")
            .replace(/\|\|\s*$/gm, "|]")
            .replace(/\|{3,}/g, "||")
            .replace(/\|\s*\|(?!\|)/g, "|")
            .replace(/\r\n/g, "\n")
            .trim();

        // Inject MIDI program after K: line if not already present
        if (midiProgram !== undefined && !cleaned.includes("%%MIDI program")) {
            cleaned = cleaned.replace(/(K:[^\n]+\n)/, "$1%%MIDI program " + midiProgram + "\n");
        }

        return cleaned;
    },

    // Renders ABC notation with specified instrument
    renderAbc: function (visualObjId, audioObjId, abcString, midiProgram) {
        if (typeof ABCJS === "undefined") {
            console.error("ABCJS is not loaded");
            return Promise.resolve(false);
        }

        this.currentProgram = midiProgram || 0;
        var cleanedAbc = this.sanitizeAbc(abcString, midiProgram);
        if (!cleanedAbc) return Promise.resolve(false);

        var self = this;

        try {
            var visualObj = ABCJS.renderAbc(visualObjId, cleanedAbc, {
                responsive: "resize",
                add_classes: true
            });

            if (!visualObj || visualObj.length === 0) return Promise.resolve(false);

            if (ABCJS.synth && ABCJS.synth.supportsAudio()) {
                if (!self.audioContext) {
                    self.audioContext = new (window.AudioContext || window.webkitAudioContext)();
                }

                if (self.audioContext.state === 'suspended') {
                    self.audioContext.resume();
                }

                self.synth = new ABCJS.synth.CreateSynth();

                var synthOptions = {
                    options: {
                        swing: false,
                        voicesOff: false,
                        program: midiProgram || 0
                    }
                };

                return self.synth.init({
                    visualObj: visualObj[0],
                    audioContext: self.audioContext,
                    millisecondsPerMeasure: visualObj[0].millisecondsPerMeasure(),
                    options: synthOptions.options
                }).then(function () {
                    return self.synth.prime(synthOptions);
                }).then(function () {
                    return true;
                }).catch(function (err) {
                    console.error("Synth init failed:", err);
                    return false;
                });
            }
            return Promise.resolve(true);
        } catch (error) {
            console.error("renderAbc failed:", error);
            return Promise.resolve(false);
        }
    },

    playAudio: function () {
        if (!this.synth) return;

        var self = this;
        if (this.audioContext && this.audioContext.state === 'suspended') {
            this.audioContext.resume().then(function () {
                self.synth.start();
            });
        } else if (this.audioContext) {
            this.synth.start();
        }
    },

    stopAudio: function () {
        if (this.synth) {
            this.synth.stop();
        }
    },

    createMidiDownload: function (abcString, containerId, midiProgram) {
        if (!ABCJS.synth || !ABCJS.synth.getMidiFile) return;

        var cleanedAbc = this.sanitizeAbc(abcString, midiProgram);
        try {
            var midi = ABCJS.synth.getMidiFile(cleanedAbc, { midiOutputType: "encoded" });
            var container = document.getElementById(containerId);
            if (container && midi) {
                container.href = midi;
                container.download = "generated_music.mid";
                container.style.display = "inline-flex";
            }
        } catch (error) {
            console.error("MIDI generation failed:", error);
        }
    }
};
