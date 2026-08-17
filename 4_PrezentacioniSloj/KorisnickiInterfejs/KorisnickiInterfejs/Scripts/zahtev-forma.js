(function ($) {
    "use strict";

    function osveziIndikatorPravila() {
        var nepodnetaDokumenta = [];

        $("input[type='checkbox'][data-obavezno-odobrenje='true']").each(function () {
            if (!this.checked) {
                nepodnetaDokumenta.push($(this).attr("data-naziv-dokumenta"));
            }
        });

        var testJePolozen = $("#RezultatTestaSposobnosti").val() === "Položen";
        var $indikator = $("#indikator-poslovnog-pravila");

        if (!$indikator.length) {
            return;
        }

        $indikator.removeClass("alert-success alert-warning");

        if (testJePolozen && nepodnetaDokumenta.length === 0) {
            $indikator
                .addClass("alert-success")
                .text("Test i obavezna dokumentacija ispunjavaju deo uslova. Konačno odobrenje proverava server prema datumu pregleda i REST parametru X.");
            return;
        }

        var razlozi = [];

        if (!testJePolozen) {
            razlozi.push("test sposobnosti nije označen kao Položen");
        }

        if (nepodnetaDokumenta.length > 0) {
            razlozi.push("nedostaje: " + nepodnetaDokumenta.join(", "));
        }

        $indikator
            .addClass("alert-warning")
            .text("Zahtev trenutno nije spreman za odobrenje — " + razlozi.join("; ") + ".");
    }

    $(function () {
        $(document).on(
            "change",
            "#RezultatTestaSposobnosti, input[type='checkbox'][data-obavezno-odobrenje='true']",
            osveziIndikatorPravila);

        osveziIndikatorPravila();
    });
}(jQuery));
