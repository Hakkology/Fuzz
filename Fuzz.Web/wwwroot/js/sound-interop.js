
window.soundInterop = {
    synth: null,
    tune: null,
    
    // Renders ABC notation and prepares audio
    renderAbc: function (visualObjId, audioObjId, abcString) {
        // 1. Render Visual Sheet Music
        var visualObj = abcjs.renderAbc(visualObjId, abcString, { responsive: "resize" });
        
        // 2. Setup Audio Synth
        if (abcjs.synth.supportsAudio()) {
            this.synth = new abcjs.synth.CreateSynth();
            
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
        }
        return Promise.resolve(false);
    },

    playAudio: function() {
        if(this.synth) {
            this.synth.start();
        }
    },

    stopAudio: function() {
        if(this.synth) {
            this.synth.stop();
        }
    },
    
    // Create a downloadable MIDI file
    createMidiDownload: function(abcString, containerId) {
        var midi = abcjs.synth.getMidiFile(abcString, { midiOutputType: "encoded" });
        var container = document.getElementById(containerId);
        if(container) {
             container.href = midi;
             container.download = "generated_music.mid";
             container.style.display = "block";
        }
    }
};
