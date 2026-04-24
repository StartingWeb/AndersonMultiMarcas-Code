(function () {
    const carousels = Array.from(document.querySelectorAll('[data-catalog-carousel]'));

    if (!carousels.length) {
        return;
    }

    carousels.forEach(function (carousel) {
        const images = Array.from(carousel.querySelectorAll('.catalog-thumb-image'));
        const dots = Array.from(carousel.querySelectorAll('.catalog-thumb-dot'));
        const navButtons = Array.from(carousel.querySelectorAll('.catalog-thumb-nav'));

        if (images.length < 2) {
            return;
        }

        let currentIndex = 0;

        function showImage(index) {
            currentIndex = (index + images.length) % images.length;

            images.forEach(function (image, imageIndex) {
                image.classList.toggle('is-active', imageIndex === currentIndex);
            });

            dots.forEach(function (dot, dotIndex) {
                dot.classList.toggle('is-active', dotIndex === currentIndex);
            });
        }

        navButtons.forEach(function (button) {
            button.addEventListener('click', function (event) {
                event.preventDefault();
                event.stopPropagation();
                const direction = Number(button.dataset.direction || 0);
                showImage(currentIndex + direction);
            });
        });

        dots.forEach(function (dot) {
            dot.addEventListener('click', function (event) {
                event.preventDefault();
                event.stopPropagation();
                showImage(Number(dot.dataset.photoIndex || 0));
            });
        });

        showImage(0);
    });
})();

(function () {
    const modalElement = document.getElementById('sellerContactModal');
    if (!modalElement || typeof bootstrap === 'undefined') {
        return;
    }

    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    const contextLabel = modalElement.querySelector('[data-seller-contact-context]');
    const actions = Array.from(modalElement.querySelectorAll('[data-seller-phone][data-seller-name]'));
    const triggers = Array.from(document.querySelectorAll('[data-whatsapp-modal]'));

    if (!triggers.length) {
        return;
    }

    function buildMessage(sellerName, vehicleTitle) {
        const normalizedTitle = (vehicleTitle || '').trim();
        if (normalizedTitle) {
            return `Ola ${sellerName}, quero saber mais sobre ${normalizedTitle}.`;
        }

        return `Ola ${sellerName}, quero falar com um vendedor da Anderson Multimarcas.`;
    }

    function updateModalForContext(vehicleTitle) {
        const contextTitle = (vehicleTitle || '').trim();

        if (contextLabel) {
            contextLabel.textContent = contextTitle
                ? `Escolha um vendedor para continuar sobre: ${contextTitle}`
                : 'Escolha um contato para continuar no WhatsApp.';
        }

        actions.forEach(function (action) {
            const sellerName = action.getAttribute('data-seller-name') || 'vendedor';
            const sellerPhone = action.getAttribute('data-seller-phone');

            if (!sellerPhone) {
                action.setAttribute('href', '#');
                return;
            }

            const message = buildMessage(sellerName, contextTitle);
            const href = `https://wa.me/${sellerPhone}?text=${encodeURIComponent(message)}`;
            action.setAttribute('href', href);
        });
    }

    triggers.forEach(function (trigger) {
        trigger.addEventListener('click', function (event) {
            event.preventDefault();
            const vehicleTitle = trigger.getAttribute('data-vehicle-title') || '';
            updateModalForContext(vehicleTitle);
            modal.show(trigger);
        });
    });
})();

(function () {
    const videos = Array.from(document.querySelectorAll('video[data-autoplay-video]'));

    if (!videos.length) {
        return;
    }

    function tryPlay(video) {
        video.muted = true;
        video.defaultMuted = true;
        video.setAttribute('muted', '');
        video.setAttribute('playsinline', '');
        video.setAttribute('webkit-playsinline', 'true');

        const playPromise = video.play();

        if (playPromise && typeof playPromise.catch === 'function') {
            playPromise.catch(function () {
                // Safari may block the first attempt until the media is ready or the page becomes visible.
            });
        }
    }

    videos.forEach(function (video) {
        if (video.readyState >= 2) {
            tryPlay(video);
        }

        video.addEventListener('loadedmetadata', function () {
            tryPlay(video);
        });

        video.addEventListener('canplay', function () {
            tryPlay(video);
        });

        document.addEventListener('visibilitychange', function () {
            if (!document.hidden && video.paused) {
                tryPlay(video);
            }
        });
    });
})();
