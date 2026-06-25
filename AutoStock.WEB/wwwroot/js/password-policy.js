(function () {
    function initPasswordPolicy(form) {
        if (!form || form.dataset.passwordPolicyBound === "true") return;

        const password = form.querySelector("[data-password-input]");
        const confirmation = form.querySelector("[data-password-confirm]");
        const submit = form.querySelector("button[type='submit']");
        const requirements = form.querySelector("[data-password-requirements]");

        if (!password || !confirmation || !submit || !requirements) return;

        form.dataset.passwordPolicyBound = "true";

        const rules = {
            length: value => value.length >= 6,
            upper: value => /\p{Lu}/u.test(value),
            lower: value => /\p{Ll}/u.test(value),
            digit: value => /\d/.test(value),
            match: value => value.length > 0 && value === confirmation.value
        };

        function updateRule(name, isValid, touched) {
            const element = requirements.querySelector(`[data-password-rule='${name}']`);
            if (!element) return;

            element.classList.toggle("is-valid", isValid);
            element.classList.toggle("is-invalid", touched && !isValid);
        }

        function validate() {
            const value = password.value;
            const touched = value.length > 0 || confirmation.value.length > 0;
            const results = {
                length: rules.length(value),
                upper: rules.upper(value),
                lower: rules.lower(value),
                digit: rules.digit(value),
                match: rules.match(value)
            };

            Object.entries(results).forEach(([name, isValid]) => {
                updateRule(name, isValid, touched);
            });

            const isValid = Object.values(results).every(Boolean);
            submit.disabled = !isValid;
            submit.setAttribute("aria-disabled", String(!isValid));
            return isValid;
        }

        password.addEventListener("input", validate);
        confirmation.addEventListener("input", validate);

        form.addEventListener("submit", function (event) {
            if (validate()) return;

            event.preventDefault();
            password.focus();
        });

        validate();
    }

    document.addEventListener("DOMContentLoaded", function () {
        document.querySelectorAll("[data-password-policy-form]").forEach(initPasswordPolicy);
    });
})();
