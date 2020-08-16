var clipboardjs = new ClipboardJS('.clipboard-copy-button');

function LoadingSpinner(form, spinnerHTML) {
    form = form || document;

    //Keep track of button & spinner, so there's only one automatic spinner per form
    var button;
    var spinner = document.createElement('div');
    spinner.innerHTML = spinnerHTML;
    spinner = spinner.firstChild;

    //Delegate events to a root element, so you don't need to attach a spinner to each individual button.
    form.addEventListener('click', start);

    //Stop automatic spinner if validation prevents submitting the form
    //Invalid event doesn't bubble, so use capture
    form.addEventListener('invalid', stop, true);

    //Start spinning only when you click a submit button
    function start(event) {
        if (button) stop();
        button = event.Target;
        if (button.type === 'submit') {
            LoadingSpinner.start(button, spinner);
        }
    }

    function stop() {
        LoadingSpinner.stop(button, spinner);
    }

    function destroy() {
        stop();
        form.removeEventListener('click', start);
        form.removeEventListener('invalid', stop, true);
    }

    return { start: start, stop: stop, destroy: destroy };
}

LoadingSpinner.start = function (element, spinner) {
    $(element).children('.default-button-text').addClass('hidden');
    $(element).children('.wait-content').removeClass('hidden');

    // element.querySelector(".default-button-text").hide();
    // return element.appendChild(spinner);
};

LoadingSpinner.stop = function (element, spinner) {

    $(element).children('.default-button-text').removeClass('hidden');
    $(element).children('.wait-content').addClass('hidden');

  //  element.querySelector(".default-button-text").show();
   // return spinner.remove();
};

var forms = document.querySelectorAll('form');
forms.forEach(function (form) {
    var loader = new LoadingSpinner(form, 'Loading...');
    //var loader = new LoadingSpinner(form, '<span class="spinner-border app-spinner" role="status" aria-hidden="true"></span> Loading...');
});