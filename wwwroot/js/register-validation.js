$(document).ready(function () {
    // Real-time validation for all fields
    const $form = $('#registerForm');
    const $submitBtn = $('#submitBtn');

    // Full Name validation (letters only)
    $('#FullName').on('input', function () {
        const value = $(this).val();
        const isValid = /^[a-zA-Z\s]*$/.test(value);
        const $field = $(this);

        if (value.trim() === '') {
            $field.removeClass('is-valid is-invalid');
        } else if (isValid && value.length >= 2) {
            $field.addClass('is-valid').removeClass('is-invalid');
        } else if (value.length > 0 && !isValid) {
            $field.addClass('is-invalid').removeClass('is-valid');
            $field.siblings('.invalid-feedback').text('Full name can only contain letters and spaces');
        } else if (value.length > 0 && value.length < 2) {
            $field.addClass('is-invalid').removeClass('is-valid');
            $field.siblings('.invalid-feedback').text('Full name must be at least 2 characters');
        } else {
            $field.removeClass('is-valid is-invalid');
        }
    });

    // Email validation with real-time existence check
    let emailTimeout;
    $('#Email').on('input', function () {
        const email = $(this).val();
        const $field = $(this);
        const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

        clearTimeout(emailTimeout);

        if (email === '') {
            $field.removeClass('is-valid is-invalid');
            return;
        }

        if (!emailPattern.test(email)) {
            $field.addClass('is-invalid').removeClass('is-valid');
            $field.siblings('.invalid-feedback').text('Please enter a valid email address');
            return;
        }

        // Check if email exists in database
        emailTimeout = setTimeout(function () {
            $.ajax({
                url: '/Account/CheckEmailExists',
                type: 'POST',
                data: { email: email },
                success: function (response) {
                    if (response.exists) {
                        $field.addClass('is-invalid').removeClass('is-valid');
                        $field.siblings('.invalid-feedback').text('This email is already registered. Please login instead.');
                        $submitBtn.prop('disabled', true);
                    } else {
                        $field.addClass('is-valid').removeClass('is-invalid');
                        $field.siblings('.invalid-feedback').text('');
                        checkFormValidity();
                    }
                }
            });
        }, 500);
    });

    // Phone number validation with real-time existence check
    let phoneTimeout;
    $('#PhoneNumber').on('input', function () {
        const phone = $(this).val();
        const $field = $(this);
        const phonePattern = /^[0-9]{10}$/;

        clearTimeout(phoneTimeout);

        if (phone === '') {
            $field.removeClass('is-valid is-invalid');
            return;
        }

        if (!phonePattern.test(phone)) {
            $field.addClass('is-invalid').removeClass('is-valid');
            $field.siblings('.invalid-feedback').text('Phone number must be exactly 10 digits');
            return;
        }

        // Check if phone exists in database
        phoneTimeout = setTimeout(function () {
            $.ajax({
                url: '/Account/CheckPhoneExists',
                type: 'POST',
                data: { phone: phone },
                success: function (response) {
                    if (response.exists) {
                        $field.addClass('is-invalid').removeClass('is-valid');
                        $field.siblings('.invalid-feedback').text('This phone number is already registered. Please login instead.');
                        $submitBtn.prop('disabled', true);
                    } else {
                        $field.addClass('is-valid').removeClass('is-invalid');
                        $field.siblings('.invalid-feedback').text('');
                        checkFormValidity();
                    }
                }
            });
        }, 500);
    });

    // Password validation with requirements tracking
    $('#Password').on('input', function () {
        const password = $(this).val();
        const $field = $(this);

        // Check each requirement
        const hasLength = password.length >= 8;
        const hasUppercase = /[A-Z]/.test(password);
        const hasLowercase = /[a-z]/.test(password);
        const hasNumber = /[0-9]/.test(password);
        const hasSpecial = /[@$!%*?&]/.test(password);

        // Update requirement list
        updateRequirement('reqLength', hasLength);
        updateRequirement('reqUppercase', hasUppercase);
        updateRequirement('reqLowercase', hasLowercase);
        updateRequirement('reqNumber', hasNumber);
        updateRequirement('reqSpecial', hasSpecial);

        const isValid = hasLength && hasUppercase && hasLowercase && hasNumber && hasSpecial;

        if (password === '') {
            $field.removeClass('is-valid is-invalid');
        } else if (isValid) {
            $field.addClass('is-valid').removeClass('is-invalid');
            $field.siblings('.invalid-feedback').text('');
        } else {
            $field.addClass('is-invalid').removeClass('is-valid');
            let missing = [];
            if (!hasLength) missing.push('8+ characters');
            if (!hasUppercase) missing.push('uppercase letter');
            if (!hasLowercase) missing.push('lowercase letter');
            if (!hasNumber) missing.push('number');
            if (!hasSpecial) missing.push('special character (@$!%*?&)');
            $field.siblings('.invalid-feedback').text(`Missing: ${missing.join(', ')}`);
        }

        // Also validate confirm password if it has value
        if ($('#ConfirmPassword').val()) {
            $('#ConfirmPassword').trigger('input');
        }
    });

    // Confirm password validation
    $('#ConfirmPassword').on('input', function () {
        const password = $('#Password').val();
        const confirmPassword = $(this).val();
        const $field = $(this);

        if (confirmPassword === '') {
            $field.removeClass('is-valid is-invalid');
        } else if (password === confirmPassword && password !== '') {
            $field.addClass('is-valid').removeClass('is-invalid');
            $field.siblings('.invalid-feedback').text('');
        } else {
            $field.addClass('is-invalid').removeClass('is-valid');
            $field.siblings('.invalid-feedback').text('Passwords do not match');
        }

        checkFormValidity();
    });

    // Address validation
    $('#Address').on('input', function () {
        const value = $(this).val().trim();
        const $field = $(this);

        if (value === '') {
            $field.removeClass('is-valid is-invalid');
        } else if (value.length >= 5) {
            $field.addClass('is-valid').removeClass('is-invalid');
        } else {
            $field.addClass('is-invalid').removeClass('is-valid');
            $field.siblings('.invalid-feedback').text('Please enter a complete address');
        }
    });

    // City validation
    $('#City').on('input', function () {
        const value = $(this).val().trim();
        const $field = $(this);

        if (value === '') {
            $field.removeClass('is-valid is-invalid');
        } else if (value.length >= 2) {
            $field.addClass('is-valid').removeClass('is-invalid');
        } else {
            $field.addClass('is-invalid').removeClass('is-valid');
            $field.siblings('.invalid-feedback').text('Please enter a valid city name');
        }
    });

    // Postal code validation
    $('#PostalCode').on('input', function () {
        const value = $(this).val();
        const $field = $(this);
        const isValid = /^[0-9]{4}$/.test(value);

        if (value === '') {
            $field.removeClass('is-valid is-invalid');
        } else if (isValid) {
            $field.addClass('is-valid').removeClass('is-invalid');
        } else {
            $field.addClass('is-invalid').removeClass('is-valid');
            $field.siblings('.invalid-feedback').text('Postal code must be exactly 4 digits');
        }

        checkFormValidity();
    });

    // Helper function to update requirement display
    function updateRequirement(id, isValid) {
        const $element = $(`#${id}`);
        if (isValid) {
            $element.css('color', '#2A9D8F');
            $element.prepend('<i class="fas fa-check-circle me-1"></i>');
        } else {
            $element.css('color', '#dc3545');
            $element.prepend('<i class="fas fa-times-circle me-1"></i>');
        }
        // Remove old icons to prevent duplication
        $element.find('i').first().remove();
        if (isValid) {
            $element.prepend('<i class="fas fa-check-circle me-1"></i>');
        } else {
            $element.prepend('<i class="fas fa-times-circle me-1"></i>');
        }
    }

    // Check if entire form is valid
    function checkFormValidity() {
        const allValid =
            $('#FullName').hasClass('is-valid') &&
            $('#Email').hasClass('is-valid') &&
            $('#PhoneNumber').hasClass('is-valid') &&
            $('#Address').hasClass('is-valid') &&
            $('#City').hasClass('is-valid') &&
            $('#PostalCode').hasClass('is-valid') &&
            $('#Password').hasClass('is-valid') &&
            $('#ConfirmPassword').hasClass('is-valid');

        $submitBtn.prop('disabled', !allValid);
    }

    // Form submission validation
    $form.on('submit', function (e) {
        if ($submitBtn.prop('disabled')) {
            e.preventDefault();
            alert('Please fix all validation errors before submitting.');
            return false;
        }
    });
});