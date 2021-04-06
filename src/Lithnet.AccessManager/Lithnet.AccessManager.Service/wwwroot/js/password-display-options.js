
$('#button-monochrome').on('click', clickMonoButton);
$('#button-large-font').on('click', clickLargeFontButton);
$('#button-increase-spacing').on('click', clickIncreaseSpacingButton);

var keyPrefMonochrome = 'pref-monochrome';
var keyPrefLargeFont = 'pref-large-font';
var keyPrefWideSpace = 'pref-wide-space';

if (window.localStorage.getItem(keyPrefMonochrome) === "1") {
    enableOption($('#button-monochrome'), "monochrome", null, keyPrefMonochrome);
}

if (window.localStorage.getItem(keyPrefLargeFont) === "1") {
    enableOption($('#button-large-font'), "password-font-size-large", "password-font-size-normal", keyPrefLargeFont);
}

if (window.localStorage.getItem(keyPrefWideSpace) === "1") {
    enableOption($('#button-increase-spacing'), "password-font-spacing-wide", "password-font-spacing-normal", keyPrefWideSpace);
}

function clickMonoButton(event) {
    var clickedButton = event.currentTarget;

    if ($(clickedButton).hasClass("dropdown-item-checked")) {
        disableOption(clickedButton, null, "monochrome", keyPrefMonochrome);
    } else {
        enableOption(clickedButton, "monochrome", null, keyPrefMonochrome);
    }
}

function clickLargeFontButton(event) {
    var clickedButton = event.currentTarget;

    if ($(clickedButton).hasClass("dropdown-item-checked")) {
        disableOption(clickedButton, "password-font-size-normal", "password-font-size-large", keyPrefLargeFont);
    } else {
        enableOption(clickedButton, "password-font-size-large", "password-font-size-normal", keyPrefLargeFont);
    }
}

function clickIncreaseSpacingButton(event) {
    var clickedButton = event.currentTarget;

    if ($(clickedButton).hasClass("dropdown-item-checked")) {
        disableOption(clickedButton, "password-font-spacing-normal", "password-font-spacing-wide", keyPrefWideSpace);
    } else {
        enableOption(clickedButton, "password-font-spacing-wide", "password-font-spacing-normal", keyPrefWideSpace);
    }
}

function enableOption(clickedButton, classToEnable, classToDisable, preferenceName) {

    var passwordContentFields = document.querySelectorAll('.password-content');

    passwordContentFields.forEach(function (currentField) {
        if (classToDisable != null) {
            $(currentField).removeClass(classToDisable);
        }

        if (classToEnable != null) {
            $(currentField).addClass(classToEnable);
        }
    });

    window.localStorage.setItem(preferenceName, "1");
    $(clickedButton).addClass("dropdown-item-checked");
}

function disableOption(clickedButton, classToEnable, classToDisable, preferenceName) {

    var passwordContentFields = document.querySelectorAll('.password-content');

    passwordContentFields.forEach(function (currentField) {
        if (classToDisable != null) {
            $(currentField).removeClass(classToDisable);
        }

        if (classToEnable != null) {
            $(currentField).addClass(classToEnable);
        }
    });

    window.localStorage.removeItem(preferenceName);
    $(clickedButton).removeClass("dropdown-item-checked");
}
