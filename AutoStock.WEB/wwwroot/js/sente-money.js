(function () {
    function normalize(value) {
        if (value === null || value === undefined) {
            return "";
        }

        let text = String(value)
            .trim()
            .replaceAll("₺", "")
            .replace(/TL/gi, "")
            .replace(/\s/g, "");

        if (!text) {
            return "";
        }

        const hasComma = text.includes(",");
        const hasDot = text.includes(".");

        if (hasComma && hasDot) {
            text = text.lastIndexOf(",") > text.lastIndexOf(".")
                ? text.replaceAll(".", "").replace(",", ".")
                : text.replaceAll(",", "");
        } else if (hasComma) {
            text = text.replaceAll(".", "").replace(",", ".");
        } else if (hasDot) {
            const parts = text.split(".");
            text = parts.length === 2 && parts[1].length <= 2
                ? text
                : text.replaceAll(".", "");
        }

        return text;
    }

    function parse(value) {
        const text = normalize(value);

        if (!text) {
            return 0;
        }

        const number = Number(text);

        return Number.isFinite(number) ? number : 0;
    }

    function isValid(value) {
        const text = normalize(value);

        return !text || Number.isFinite(Number(text));
    }

    function toRaw(value) {
        const number = parse(value);

        if (!number) {
            return "0";
        }

        return number.toFixed(2).replace(/\.?0+$/, "");
    }

    function toEditValue(value) {
        const raw = toRaw(value);

        return raw.replace(".", ",");
    }

    function format(value) {
        const number = parse(value);
        const hasFraction = Math.abs(number % 1) > Number.EPSILON;
        const moneyFormatter = new Intl.NumberFormat("tr-TR", {
            minimumFractionDigits: hasFraction ? 2 : 0,
            maximumFractionDigits: 2
        });

        return moneyFormatter.format(number) + " ₺";
    }

    function cleanInput(input) {
        if (!input || input.value === "") {
            return;
        }

        input.value = toRaw(input.value);
    }

    function formatInput(input) {
        if (!input || input.value === "") {
            return;
        }

        input.value = format(input.value);
    }

    function prepareInput(input) {
        if (!input || input.value === "") {
            return;
        }

        input.value = toEditValue(input.value);
        window.setTimeout(() => {
            try {
                input.setSelectionRange(input.value.length, input.value.length);
            } catch {
                // Some input types do not support selection ranges.
            }
        }, 0);
    }

    function bindInput(input) {
        if (!input || input.dataset.senteMoneyBound === "true") {
            return;
        }

        input.dataset.senteMoneyBound = "true";
        input.type = "text";
        input.inputMode = "decimal";
        input.autocomplete = input.autocomplete || "off";

        formatInput(input);

        input.addEventListener("focus", () => prepareInput(input));
        input.addEventListener("blur", () => formatInput(input));
    }

    function bind(root = document) {
        root.querySelectorAll("[data-sente-money-input]").forEach(bindInput);
        root.querySelectorAll("[data-sente-money-display]").forEach(element => {
            if (element.dataset.senteMoneyDisplayBound === "true") {
                return;
            }

            element.dataset.senteMoneyDisplayBound = "true";
            element.textContent = format(element.dataset.senteMoneyValue ?? element.textContent);
        });
    }

    function applyValidationAdapter() {
        if (!window.jQuery?.validator?.methods?.number || window.jQuery.validator.methods.number.senteMoneyAware) {
            return;
        }

        const defaultNumberValidator = window.jQuery.validator.methods.number;

        window.jQuery.validator.methods.number = function (value, element) {
            if (element?.matches?.("[data-sente-money-input]")) {
                return this.optional(element) || isValid(value);
            }

            return defaultNumberValidator.call(this, value, element);
        };

        window.jQuery.validator.methods.number.senteMoneyAware = true;
    }

    document.addEventListener("focusin", event => {
        if (event.target?.matches?.("[data-sente-money-input]")) {
            bindInput(event.target);
        }
    });

    document.addEventListener("submit", event => {
        event.target?.querySelectorAll?.("[data-sente-money-input]").forEach(cleanInput);
    }, true);

    document.addEventListener("DOMContentLoaded", () => {
        bind();
        applyValidationAdapter();
    });

    applyValidationAdapter();

    window.SenteMoney = {
        bind,
        parse,
        toRaw,
        toEditValue,
        format,
        isValid,
        cleanInput,
        formatInput
    };
})();
