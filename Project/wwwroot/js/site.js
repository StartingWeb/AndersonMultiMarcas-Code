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
