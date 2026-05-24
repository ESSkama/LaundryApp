<script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
<script>
    $(document).ready(function() {
        console.log("Document ready - Initializing order form");
        
        // ========== ORDER FORM VARIABLES ==========
        let selectedServicePrice = 0;
        let currentDiscount = 0;
        let freeDeliveriesRemaining = 0;
        let hasSubscription = false;
        let currentTier = '';
        let selectedExtras = [];
        let weight = 5;
        let baseServicePrice = 0;
        let selectedPayment = 'creditcard';

        // ========== PACKAGE BENEFITS MAPPING ==========
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

        // ========== LOAD SUBSCRIPTION INFO ==========
        function loadSubscriptionInfo() {
            $.get('/Subscription/GetCurrentSubscription', function(data) {
                if (data.success) {
                    hasSubscription = data.hasSubscription;
                    freeDeliveriesRemaining = data.freeDeliveriesRemaining || 0;
                    currentDiscount = data.discountRate || 0;
                    currentTier = data.hasSubscription ? data.tier : 'Standard';

                    if (hasSubscription) {
                        $('#subscriptionInfo').html(`
                            <div class="d-flex justify-content-between align-items-center">
                                <div>
                                    <i class="fas fa-crown me-2"></i>
                                    <strong>${data.tier} Plan Active</strong> - ${data.discountRate * 100}% off everything
                                    <br>
                                    <small><i class="fas fa-truck me-1"></i>Free deliveries left: ${data.freeDeliveriesRemaining} / ${data.totalFreeDeliveries}</small>
                                </div>
                                <a href="/Subscription/Manage" class="btn btn-sm btn-light">Manage</a>
                            </div>
                        `);
                        $('#upgradePrompt').hide();
                    } else {
                        $('#subscriptionInfo').html(`
                            <div class="text-center">
                                <i class="fas fa-info-circle me-2"></i>
                                ${data.message}
                                <a href="/Subscription/Upgrade" class="btn btn-sm btn-light ms-2">View Plans</a>
                            </div>
                        `);
                        $('#upgradePrompt').show();
                    }
                    
                    updatePackageBenefits(currentTier);
                    updateServiceAvailability(currentTier);
                }
            }).fail(function(error) {
                console.error("Error loading subscription:", error);
            });
        }

        // ========== UPDATE PACKAGE BENEFITS ==========
        function updatePackageBenefits(tier) {
            const benefits = packageBenefits[tier] || packageBenefits['Standard'];
            $('#packagingBenefit').text(`Packaging: ${benefits.packaging.name}`);
            $('#deliveryBenefit').text(`Delivery: ${benefits.delivery.name}`);
            $('#detergentBenefit').text(`Detergent: ${benefits.detergent.name}`);
            
            $('#PackagingType').val(benefits.packaging.name);
            $('#DeliveryMethod').val(benefits.delivery.name);
            $('#DetergentType').val(benefits.detergent.name);
            
            $('#packagingText').text(benefits.packaging.name);
            $('#packagingFee').text(`R${benefits.packaging.fee.toFixed(2)}`);
            $('#deliveryText').text(benefits.delivery.name);
            $('#detergentText').text(benefits.detergent.name);
            $('#detergentFee').text(`R${benefits.detergent.fee.toFixed(2)}`);
            
            calculateTotal();
        }

        // ========== SERVICE SELECTION ==========
        $('.service-card').click(function() {
            if ($(this).hasClass('disabled')) return;
            
            $('.service-card').removeClass('selected');
            $(this).addClass('selected');
            
            const serviceKey = $(this).data('service-key');
            $(`input[name="SelectedService"][value="${serviceKey}"]`).prop('checked', true);
            
            baseServicePrice = $(this).data('price');
            selectedServicePrice = baseServicePrice;
            $('#selectedService').text($(this).data('service'));
            $('#servicePrice').text(`R${selectedServicePrice.toFixed(2)}`);
            
            calculateTotal();
        });

        // ========== UPDATE SERVICE AVAILABILITY ==========
        function updateServiceAvailability(tier) {
            const requiresPremium = ['dryclean', 'ironing'];
            const requiresBusiness = ['express'];
            
            $('.service-card').each(function() {
                const serviceKey = $(this).data('service-key');
                let isAvailable = true;
                
                if (requiresPremium.includes(serviceKey) && tier !== 'Premium' && tier !== 'Business') {
                    isAvailable = false;
                }
                if (requiresBusiness.includes(serviceKey) && tier !== 'Business') {
                    isAvailable = false;
                }
                
                if (!isAvailable) {
                    $(this).addClass('disabled').css('opacity', '0.5');
                    $(this).find('input').prop('disabled', true);
                } else {
                    $(this).removeClass('disabled').css('opacity', '1');
                    $(this).find('input').prop('disabled', false);
                }
            });
        }

        // ========== EXTRA SELECTION ==========
        $('.extra-card').click(function() {
            const extraValue = $(this).data('extra');
            const extraPrice = $(this).data('price');
            
            if ($(this).hasClass('selected')) {
                $(this).removeClass('selected');
                selectedExtras = selectedExtras.filter(e => e.value !== extraValue);
            } else {
                $(this).addClass('selected');
                selectedExtras.push({ value: extraValue, price: extraPrice });
            }
            
            $('#SelectedExtras').val(selectedExtras.map(e => e.value).join(','));
            calculateTotal();
        });

        // ========== WEIGHT CHANGE ==========
        $('#WeightKg').on('input', function() {
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
            selectedExtras.forEach(extra => {
                extrasTotal += extra.price;
            });
            
            let subtotal = selectedServicePrice + packagingPrice + deliveryPrice + detergentPrice + weightFee + extrasTotal;
            
            $('#weightFee').text(`R${weightFee.toFixed(2)}`);
            $('#extrasTotal').text(`R${extrasTotal.toFixed(2)}`);
            
            if (selectedServicePrice === 0) {
                $('#subtotal').text('R0.00');
                $('#total').text('R0.00');
                return;
            }
            
            let discountAmount = 0;
            if (hasSubscription && currentDiscount > 0) {
                discountAmount = subtotal * currentDiscount;
                $('#discountRow').show();
                $('#discountPercent').text(currentDiscount * 100);
                $('#discountAmount').text(`-R${discountAmount.toFixed(2)}`);
            } else {
                $('#discountRow').hide();
            }
            
            let afterDiscount = subtotal - discountAmount;
            let finalTotal = afterDiscount;
            
            if (freeDeliveriesRemaining > 0 && deliveryPrice > 0 && hasSubscription) {
                finalTotal = afterDiscount - deliveryPrice;
                $('#deliveryFee').html('R0.00 <small class="text-success">(Free!)</small>');
                $('#freeDeliveryRow').show();
                $('#freeDeliverySavings').text(`-R${deliveryPrice.toFixed(2)}`);
            } else {
                $('#deliveryFee').text(`R${deliveryPrice.toFixed(2)}`);
                $('#freeDeliveryRow').hide();
                if (freeDeliveriesRemaining === 0 && hasSubscription) {
                    $('#deliveryFee').append(' <small class="text-warning">(No free deliveries left)</small>');
                }
            }
            
            $('#subtotal').text(`R${subtotal.toFixed(2)}`);
            $('#total').text(`R${finalTotal.toFixed(2)}`);
        }

        // ========== PAYMENT METHOD SELECTION ==========
        // Hide all payment detail forms initially
        $('#creditCardDetails').show();
        $('#eftDetails').hide();
        $('#payfastDetails').hide();
        
        // Payment card click handler - using event delegation for reliability
        $(document).on('click', '.payment-card', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            console.log("Payment card clicked:", $(this).data('payment'));
            
            // Remove selected class from all payment cards
            $('.payment-card').removeClass('selected');
            
            // Add selected class to clicked card
            $(this).addClass('selected');
            
            // Get the payment method
            var paymentMethod = $(this).data('payment');
            console.log("Selected payment method:", paymentMethod);
            
            // Update hidden field
            $('#SelectedPayment').val(paymentMethod);
            
            // Hide all payment detail forms
            $('#creditCardDetails').hide();
            $('#eftDetails').hide();
            $('#payfastDetails').hide();
            
            // Show the selected payment detail form
            if (paymentMethod === 'creditcard') {
                $('#creditCardDetails').show();
                console.log("Showing credit card form");
            } else if (paymentMethod === 'eft') {
                $('#eftDetails').show();
                console.log("Showing EFT form");
            } else if (paymentMethod === 'payfast') {
                $('#payfastDetails').show();
                console.log("Showing PayFast form");
            }
        });
        
        // ========== CARD FORMATTING ==========
        $('#CardNumber').on('input', function() {
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

        $('#CardExpiry').on('input', function() {
            let value = $(this).val().replace(/\//g, '');
            if (value.length >= 2) {
                value = value.substring(0, 2) + '/' + value.substring(2, 4);
                $(this).val(value);
            }
        });
        
        // Set default selected payment card
        $('.payment-card[data-payment="creditcard"]').addClass('selected');
        
        console.log("Payment method initialization complete");
    });

        // ========== FORM SUBMISSION VALIDATION ==========
        $('#orderForm').submit(function(e) {
            console.log("Form submitting...");
            
            if (selectedServicePrice === 0) {
                e.preventDefault();
                alert('Please select a service');
                return false;
            }
            
            if (!$('#PickupAddress').val().trim() || !$('#DeliveryAddress').val().trim()) {
                e.preventDefault();
                alert('Please enter both pickup and delivery addresses');
                return false;
            }
            
            // Get selected payment method
            const selectedCard = $('.payment-card.selected');
            const paymentMethod = selectedCard.data('payment');
            
            if (!paymentMethod) {
                e.preventDefault();
                alert('Please select a payment method');
                return false;
            }
            
            // Set the hidden field
            $('#SelectedPayment').val(paymentMethod);
            
            // Validate payment details
            if (paymentMethod === 'creditcard') {
                const cardNumber = $('#CardNumber').val().replace(/\s/g, '');
                const expiry = $('#CardExpiry').val();
                const cvv = $('#CardCvv').val();
                
                if (cardNumber.length < 15 || cardNumber.length > 16) {
                    e.preventDefault();
                    alert('Please enter a valid card number (15-16 digits)');
                    return false;
                }
                
                if (!expiry || !expiry.match(/^(0[1-9]|1[0-2])\/([0-9]{2})$/)) {
                    e.preventDefault();
                    alert('Please enter a valid expiry date (MM/YY)');
                    return false;
                }
                
                if (!cvv || cvv.length < 3) {
                    e.preventDefault();
                    alert('Please enter a valid CVV code');
                    return false;
                }
            } else if (paymentMethod === 'payfast') {
                const email = $('#PayFastEmail').val();
                if (!email || !email.includes('@')) {
                    e.preventDefault();
                    alert('Please enter a valid email address for PayFast');
                    return false;
                }
            }
            
            return true;
        });

        // ========== INITIALIZE EVERYTHING ==========
        loadSubscriptionInfo();
        initPaymentMethods();
        
        // Set default selected payment (Credit Card)
        $('.payment-card[data-payment="creditcard"]').addClass('selected');
        $('#SelectedPayment').val('creditcard');
        $('#paymentDetails').show();
        $('#creditCardDetails').show();
        
        // Set initial weight display
        $('#displayWeight').text(weight);
        
        console.log("Initialization complete");
    });
</script>