var speechButtons = document.querySelectorAll('.speech-button');

if ('speechSynthesis' in window) {
    speechButtons.forEach(function (currentBtn) {
        currentBtn.addEventListener('click', clickSpeakButton);
    });
} else {
    // Speech Synthesis Not Supported
    speechButtons.forEach(function (currentBtn) {
        $(currentBtn).hide();
    });
}

var speakingButton;

function clickSpeakButton(event) {
    var clickedButton = event.currentTarget;

    if (speechSynthesis.speaking) {
        speechSynthesis.cancel();

        if (speakingButton === clickedButton) {
            return;
        }

        speakingButton = null;
    }

    var text = $(clickedButton).attr('speech-data');

    if (!(text)) {
        return;
    }

    this.utterance = new SpeechSynthesisUtterance(text);
    this.utterance.rate = 0.9;

    this.utterance.addEventListener('start', () => {
        $(clickedButton).addClass("active");
        speakingButton = clickedButton;
    });

    this.utterance.addEventListener('end', () => {
        $(clickedButton).removeClass("active");
    });

    speechSynthesis.speak(this.utterance);
}
