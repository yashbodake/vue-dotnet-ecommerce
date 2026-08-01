// Spec 06 — event-delegated add-to-cart (works after catalog AJAX re-renders cards)
$(function () {
    function antiforgeryToken() {
        return $('input[name="__RequestVerificationToken"]').val();
    }

    function refreshMiniCart() {
        $('#mini-cart-container').load('/Cart/MiniCart');
    }

    // Initial mini-cart load
    refreshMiniCart();

    $(document).on('click', '.add-to-cart-btn', function (e) {
        e.preventDefault();
        var $btn = $(this);
        if ($btn.is(':disabled')) {
            return;
        }

        var productId = $btn.data('product-id');
        var variantId = $btn.data('variant-id');
        var quantity = parseInt($btn.data('quantity'), 10) || 1;

        $.ajax({
            url: '/Cart/AddToCart',
            type: 'POST',
            data: {
                productId: productId,
                variantId: variantId || null,
                quantity: quantity,
                __RequestVerificationToken: antiforgeryToken()
            },
            headers: {
                'RequestVerificationToken': antiforgeryToken()
            },
            success: function (result) {
                if (result.success) {
                    refreshMiniCart();
                    $btn.addClass('btn-added');
                    setTimeout(function () { $btn.removeClass('btn-added'); }, 600);
                } else {
                    alert(result.message || 'Could not add to cart');
                }
            },
            error: function () {
                alert('Could not add to cart');
            }
        });
    });
});
