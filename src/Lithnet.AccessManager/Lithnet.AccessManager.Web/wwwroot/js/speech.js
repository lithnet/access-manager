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

    var utterance = new SpeechSynthesisUtterance(text);
    utterance.addEventListener('start', () => {
        $(clickedButton).addClass("active");
        speakingButton = clickedButton;
    });

    utterance.addEventListener('end', () => {
        $(clickedButton).removeClass("active");
    });

    speechSynthesis.speak(utterance);
}
