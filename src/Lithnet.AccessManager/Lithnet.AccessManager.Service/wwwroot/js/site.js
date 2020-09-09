var clipboardjs = new ClipboardJS('.clipboard-copy-button');

$(function () {
    $(".button-loading-overlay").click(function () {
        $(".loading-overlay").fadeIn();
    });
});