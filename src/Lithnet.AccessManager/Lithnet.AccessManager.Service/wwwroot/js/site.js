var clipboardjs = new ClipboardJS(".clipboard-copy-button");

$(function () {
    $(".button-loading-overlay").click(function () {
        $(".loading-overlay").fadeIn();
    });
});

$(function () {
    var elements = document.querySelectorAll("[data-aspnet-utc-time]");
    for (var i = 0; i < elements.length; i++) {
        for (var i = 0; i < elements.length; i++) {
            {
                var element = elements[i];
                var unixMillisecondString = element.dataset.aspnetUtcTime;

                if (unixMillisecondString) {
                    var dateTime = new Date(parseInt(unixMillisecondString));

                    var options = {
                        year: "numeric",
                        day: "numeric",
                        month: "long",
                        hour12: true,
                        hour: "numeric",
                        minute: "numeric",
                        second: "numeric"
                        //timeZoneName: "long"
                    };

                    element.innerHTML = window.Intl.DateTimeFormat("default", options).format(dateTime);
                }
            }
        }
    }
});
