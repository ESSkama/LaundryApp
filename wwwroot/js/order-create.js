$(document).ready(function () {
    // ========== VARIABLES ==========
    let selectedServices = [];
    let selectedServiceTotal = 0;
    let currentDiscount = 0;
    let freeDeliveriesRemaining = 0;
    let hasSubscription = false;
    let currentTier = '';
    let selectedExtras = [];
    let weight = 5;
    let selectedPayment = 'creditcard';
    let lastTier = '';

    // Service prices mapping
    const servicePrices = {
        'washonly': 150,
        'washfold': 250,
        'dryclean': 450,
        'ironing': 180,
        'express': 350
    };

    const serviceNames = {
        'washonly': 'Wash Only',
        'washfold': 'Wash & Fold',
        'dryclean': 'Dry Cleaning',
        'ironing': 'Ironing Service',
        'express': 'Express Same-Day'
    };

    // Service availability by subscription tier
    const servicesByTier = {
        'Standard': ['washonly', 'washfold'],
        'Premium': ['washonly', 'washfold', 'dryclean', 'ironing'],
        'Business': ['washonly', 'washfold', 'dryclean', 'ironing', 'express']
    };

    // Package benefits mapping
    const packageBenefits = {
        'Standard': {
            packaging: { name: 'Plastic Bags', fee: 0 },
            delivery: { name: 'Standard (2-3 days)', fee: 50 },
            detergent: { name: 'Regular', fee: 0 }
        },
        'Premium': {
            packaging: { name: 'Eco-Friendly Bags', fee: 0 },
            delivery: { name: 'Express (Next day)', fee: 80 },
            detergent: { name: 'Premium w/ Softener', fee: 0 }
        },
        'Business': {
            packaging: { name: 'Cardboard Boxes', fee: 0 },
            delivery: { name: 'Priority (Same day)', fee: 120 },
            detergent: { name: 'Business Grade', fee: 0 }
        }
    };

    // ========== LOAD SUBSCRIPTION INFO FROM SERVER DATA ==========
    function loadSubscriptionInfo() {
        console.log("Loading subscription info from server data:", window.subscriptionData);

        // Check if user is new (just registered and selected a plan)
        if (window.userJustRegistered && window.selectedPlanTier) {
            console.log("New user just registered with plan:", window.selectedPlanTier);
            currentTier = window.selectedPlanTier;
            hasSubscription = true;

            // Set default benefits for the selected plan
            if (currentTier === 'Standard') {
                currentDiscount = 0.05;
                freeDeliveriesRemaining = 2;
            } else if (currentTier === 'Premium') {
                currentDiscount = 0.10;
                freeDeliveriesRemaining = 5;
            } else if (currentTier === 'Business') {
                currentDiscount = 0.15;
                freeDeliveriesRemaining = 10;
            }

            updateSubscriptionDisplay(currentTier, currentDiscount, freeDeliveriesRemaining);
            updatePackageBenefits(currentTier);
            updateServiceAvailability(currentTier);
            calculateTotal();
            return;
        }

        if (window.subscriptionData && window.subscriptionData.success) {
            const data = window.subscriptionData;
            const newTier = data.hasSubscription ? data.tier : 'Standard';

            hasSubscription = data.hasSubscription || false;
            freeDeliveriesRemaining = data.freeDeliveriesRemaining || 0;
            currentDiscount = data.discountRate || 0;
            currentTier = newTier;
            lastTier = newTier;

            updateSubscriptionDisplay(newTier, currentDiscount, freeDeliveriesRemaining);
            updatePackageBenefits(currentTier);
            updateServiceAvailability(currentTier);
            calculateTotal();
        } else {
            console.error("No subscription data available");
            $('#subscriptionInfo').html(`
                <div class="text-center text-danger">
                    <i class="fas fa-exclamation-circle me-2"></i>
                    Unable to load subscription information. Please refresh the page.
                </div>
            `);
        }
    }

    function updateSubscriptionDisplay(tier, discount, freeDeliveries) {
        const tierNames = {
            'Standard': 'Standard',
            'Premium': 'Premium',
            'Business': 'Business'
        };

        const tierColors = {
            'Standard': '#6c757d',
            'Premium': '#2A9D8F',
            'Business': '#E76F51'
        };

        const displayTier = tierNames[tier] || 'Standard';
        const discountPercent = (discount * 100).toFixed(0);

        if (hasSubscription) {
            $('#subscriptionInfo').html(`
                <div class="d-flex justify-content-between align-items-center">
                    <div>
                        <i class="fas fa-crown me-2"></i>
                        <strong>${escapeHtml(displayTier)} Plan Active</strong> - ${discountPercent}% off everything
                        <br>
                        <small><i class="fas fa-truck me-1"></i>Free deliveries left: ${freeDeliveries}</small>
                    </div>
                    <div>
                        <a href="/Subscription/Manage" class="btn btn-sm btn-light me-2">Manage</a>
                    </div>
                </div>
            `);
            $('#subscriptionInfo').css('background', `linear-gradient(135deg, ${tierColors[tier] || '#2A9D8F'} 0%, #1E6B61 100%)`);
            $('#upgradePrompt').hide();
        } else {
            $('#subscriptionInfo').html(`
                <div class="text-center">
                    <i class="fas fa-info-circle me-2"></i>
                    No active subscription. <a href="/Subscription/Upgrade" class="btn btn-sm btn-light ms-2">View Plans</a>
                </div>
            `);
            $('#upgradePrompt').show();
        }
    }

    function escapeHtml(str) {
        if (!str) return '';
        return String(str).replace(/[&<>]/g, function (m) {
            if (m === '&') return '&amp;';
            if (m === '<') return '&lt;';
            if (m === '>') return '&gt;';
            return m;
        });
    }

    // ========== UPDATE PACKAGE BENEFITS ==========
    function updatePackageBenefits(tier) {
        console.log("Updating package benefits for tier:", tier);
        const benefits = packageBenefits[tier] || packageBenefits['Standard'];

        $('#packagingBenefit').text('Packaging: ' + benefits.packaging.name);
        $('#deliveryBenefit').text('Delivery: ' + benefits.delivery.name);
        $('#detergentBenefit').text('Detergent: ' + benefits.detergent.name);

        $('#PackagingType').val(benefits.packaging.name);
        $('#DeliveryMethod').val(benefits.delivery.name);
        $('#DetergentType').val(benefits.detergent.name);

        $('#packagingText').text(benefits.packaging.name);
        $('#packagingFee').text('R' + benefits.packaging.fee.toFixed(2));
        $('#deliveryText').text(benefits.delivery.name);
        $('#detergentText').text(benefits.detergent.name);
        $('#detergentFee').text('R' + benefits.detergent.fee.toFixed(2));

        calculateTotal();
    }

    // ========== UPDATE SERVICE AVAILABILITY ==========
    function updateServiceAvailability(tier) {
        console.log("Updating service availability for tier:", tier);

        const availableServices = servicesByTier[tier] || servicesByTier['Standard'];

        $('.service-item').each(function () {
            const serviceCard = $(this).find('.service-card');
            const serviceKey = serviceCard.data('service-key');
            const isAvailable = availableServices.includes(serviceKey);

            if (!isAvailable) {
                $(this).hide();
                const checkbox = $(this).find('.service-checkbox');
                if (checkbox.length && checkbox.is(':checked')) {
                    const serviceValue = checkbox.val();
                    checkbox.prop('checked', false);
                    const index = selectedServices.indexOf(serviceValue);
                    if (index !== -1) {
                        selectedServices.splice(index, 1);
                        updateServiceDisplay();
                        calculateTotal();
                    }
                }
            } else {
                $(this).show();
                $(this).find('.service-checkbox').prop('disabled', false);
            }
        });

        updateServiceDisplay();
    }

    function updateServiceDisplay() {
        const displayNamesList = selectedServices.map(function (s) { return serviceNames[s] || s; }).join(', ');
        $('#selectedServices').text(displayNamesList || 'None selected');
        $('#SelectedServices').val(selectedServices.join(','));

        // Update service total
        selectedServiceTotal = selectedServices.reduce(function (total, service) {
            return total + (servicePrices[service] || 0);
        }, 0);
        $('#servicePrice').text('R' + selectedServiceTotal.toFixed(2));
    }

    // ========== SERVICE SELECTION ==========
    $(document).on('change', '.service-checkbox', function () {
        const serviceValue = $(this).val();
        const serviceCard = $(this).closest('.service-card');
        const availableServices = servicesByTier[currentTier] || servicesByTier['Standard'];
        const isAvailable = availableServices.includes(serviceValue);

        if ($(this).is(':checked')) {
            if (isAvailable) {
                if (!selectedServices.includes(serviceValue)) {
                    selectedServices.push(serviceValue);
                }
                serviceCard.addClass('selected');
            } else {
                $(this).prop('checked', false);
                alert('This service is not available with your current plan. Please upgrade to access this service.');
                return;
            }
        } else {
            selectedServices = selectedServices.filter(function (s) { return s !== serviceValue; });
            serviceCard.removeClass('selected');
        }

        updateServiceDisplay();
        calculateTotal();
    });

    // ========== EXTRA SELECTION ==========
    $(document).on('click', '.extra-card', function () {
        const extraValue = $(this).data('extra');
        const extraPrice = $(this).data('price');

        if ($(this).hasClass('selected')) {
            $(this).removeClass('selected');
            selectedExtras = selectedExtras.filter(function (e) { return e.value !== extraValue; });
        } else {
            $(this).addClass('selected');
            selectedExtras.push({ value: extraValue, price: extraPrice });
        }

        $('#SelectedExtras').val(selectedExtras.map(function (e) { return e.value; }).join(','));
        calculateTotal();
    });

    // ========== WEIGHT CHANGE ==========
    $(document).on('input', '#WeightKg', function () {
        weight = parseFloat($(this).val()) || 0;
        $('#displayWeight').text(weight);
        calculateTotal();
    });

    // ========== CALCULATE TOTAL ==========
    function calculateTotal() {
        const benefits = packageBenefits[currentTier] || packageBenefits['Standard'];
        const packagingPrice = benefits.packaging.fee;
        const deliveryPrice = benefits.delivery.fee;
        const detergentPrice = benefits.detergent.fee;

        let weightFee = 0;
        if (weight > 5) {
            weightFee = (weight - 5) * 5;
        }

        $('#displayWeight').text(weight);

        let extrasTotal = 0;
        selectedExtras.forEach(function (extra) {
            extrasTotal += extra.price;
        });

        let subtotal = selectedServiceTotal + weightFee + extrasTotal + packagingPrice + detergentPrice + deliveryPrice;

        $('#weightFee').text('R' + weightFee.toFixed(2));
        $('#extrasTotal').text('R' + extrasTotal.toFixed(2));

        if (selectedServiceTotal === 0) {
            $('#subtotal').text('R0.00');
            $('#total').text('R0.00');
            return;
        }

        let discountAmount = 0;
        if (hasSubscription && currentDiscount > 0) {
            discountAmount = subtotal * currentDiscount;
            $('#discountRow').show();
            $('#discountPercent').text(currentDiscount * 100);
            $('#discountAmount').text('-R' + discountAmount.toFixed(2));
        } else {
            $('#discountRow').hide();
        }

        let afterDiscount = subtotal - discountAmount;
        let finalTotal = afterDiscount;

        if (freeDeliveriesRemaining > 0 && deliveryPrice > 0 && hasSubscription) {
            finalTotal = afterDiscount - deliveryPrice;
            $('#deliveryFee').html('R0.00 <small class="text-success">(Free!)</small>');
            $('#freeDeliveryRow').show();
            $('#freeDeliverySavings').text('-R' + deliveryPrice.toFixed(2));
        } else {
            $('#deliveryFee').text('R' + deliveryPrice.toFixed(2));
            $('#freeDeliveryRow').hide();
        }

        $('#subtotal').text('R' + subtotal.toFixed(2));
        $('#total').text('R' + finalTotal.toFixed(2));

        console.log("Calculation Summary:");
        console.log("Services Total: R" + selectedServiceTotal);
        console.log("Weight Fee: R" + weightFee);
        console.log("Extras Total: R" + extrasTotal);
        console.log("Subtotal: R" + subtotal);
        console.log("Discount: -R" + discountAmount);
        console.log("Final Total: R" + finalTotal);
    }

    // ========== PAYMENT METHOD SELECTION ==========
    $(document).on('click', '.payment-card', function () {
        $('.payment-card').removeClass('selected');
        $(this).addClass('selected');

        selectedPayment = $(this).data('payment');
        $('#SelectedPayment').val(selectedPayment);

        $('.payment-detail-form').hide();

        if (selectedPayment === 'creditcard') {
            $('#creditCardDetails').show();
        } else if (selectedPayment === 'eft') {
            $('#eftDetails').show();
        } else if (selectedPayment === 'payfast') {
            $('#payfastDetails').show();
        }
    });

    // ========== CARD FORMATTING ==========
    $(document).on('input', '#CardNumber', function () {
        let value = $(this).val().replace(/\s/g, '');
        if (value.length > 0) {
            let formatted = '';
            for (let i = 0; i < value.length; i++) {
                if (i > 0 && i % 4 === 0) formatted += ' ';
                formatted += value[i];
            }
            $(this).val(formatted);
        }
    });

    $(document).on('input', '#CardExpiry', function () {
        let value = $(this).val().replace(/\//g, '');
        if (value.length >= 2) {
            value = value.substring(0, 2) + '/' + value.substring(2, 4);
            $(this).val(value);
        }
    });

    // ========== FORM VALIDATION ==========
    $('#orderForm').submit(function (e) {
        console.log("Form submission - Selected services:", selectedServices);

        // Make sure the hidden field is properly set
        $('#SelectedServices').val(selectedServices.join(','));

        if (selectedServices.length === 0) {
            e.preventDefault();
            alert('Please select at least one service');
            return false;
        }

        if (!$('#PickupAddress').val().trim() || !$('#DeliveryAddress').val().trim()) {
            e.preventDefault();
            alert('Please enter both pickup and delivery addresses');
            return false;
        }

        const paymentMethod = $('.payment-card.selected').data('payment');
        if (!paymentMethod) {
            e.preventDefault();
            alert('Please select a payment method');
            return false;
        }

        return true;
    });

    // ========== INITIALIZE ==========
    loadSubscriptionInfo();

    $('.payment-card[data-payment="creditcard"]').addClass('selected');
    $('#SelectedPayment').val('creditcard');
    $('#displayWeight').text(weight);

    console.log("Order form initialized with tier:", currentTier);
});