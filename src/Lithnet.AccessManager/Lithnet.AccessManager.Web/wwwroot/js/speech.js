
$('#buttonSpeak').click(function () {
    var text = $('#divPhoneticText').text();
    if (speechSynthesis.speaking === true) {
        speechSynthesis.cancel();
    } else {
        var utterance = new SpeechSynthesisUtterance(text);
        speechSynthesis.speak(utterance);

        utterance.addEventListener('end', () => {
            $("#buttonSpeak").removeClass("active");
        });
    }
});
