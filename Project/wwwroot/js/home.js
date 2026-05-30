(() => {
  const setupHeroPreviewSlider = () => {
    const slider = document.querySelector("[data-home-preview-slider]");
    const track = slider?.querySelector("[data-home-preview-track]");
    if (!slider || !track) return;

    const realSlides = Array.from(track.querySelectorAll(".home-hero-preview-slide"));
    if (realSlides.length <= 1) return;

    const firstClone = realSlides[0].cloneNode(true);
    const lastClone = realSlides[realSlides.length - 1].cloneNode(true);
    firstClone.setAttribute("data-home-clone", "first");
    lastClone.setAttribute("data-home-clone", "last");
    track.appendChild(firstClone);
    track.insertBefore(lastClone, track.firstChild);

    const slides = Array.from(track.querySelectorAll(".home-hero-preview-slide"));

    const dots = Array.from((slider.parentElement || document).querySelectorAll("[data-home-preview-dot]"));
    let current = 1;
    let timer = null;
    let isTransitioning = false;

    const paintDots = () => {
      const logicalIndex = (current - 1 + realSlides.length) % realSlides.length;
      dots.forEach((dot, i) => dot.classList.toggle("is-active", i === logicalIndex));
    };

    const render = (index, withTransition = true) => {
      if (isTransitioning && withTransition) return;
      if (withTransition) isTransitioning = true;
      track.style.transition = withTransition ? "transform .75s ease-in-out" : "none";
      current = index;
      track.style.transform = `translateX(-${current * 100}%)`;
      paintDots();
    };

    const start = () => {
      if (timer) return;
      timer = window.setInterval(() => render(current + 1), 4200);
    };

    const stop = () => {
      if (!timer) return;
      window.clearInterval(timer);
      timer = null;
    };

    dots.forEach((dot) => {
      dot.addEventListener("click", () => {
        const index = Number(dot.getAttribute("data-home-preview-dot") || "0");
        render(index + 1);
      });
    });

    track.addEventListener("transitionend", () => {
      isTransitioning = false;
      if (current === slides.length - 1) {
        render(1, false);
        return;
      }

      if (current === 0) {
        render(slides.length - 2, false);
      }
    });

    slider.addEventListener("mouseenter", stop);
    slider.addEventListener("mouseleave", start);
    slider.addEventListener("touchstart", stop, { passive: true });
    slider.addEventListener("touchend", start);

    render(1, false);
    start();
  };

  const setupSellerSlider = () => {
    const slider = document.querySelector("[data-home-seller-slider]");
    const track = slider?.querySelector("[data-home-seller-track]");
    if (!slider || !track) return;

    const slides = Array.from(track.querySelectorAll(".home-seller-slide"));
    if (slides.length <= 1) return;

    let current = 0;
    let timer = null;

    const render = (index) => {
      current = (index + slides.length) % slides.length;
      track.style.transform = `translateX(-${current * 100}%)`;
    };

    const start = () => {
      if (timer) return;
      timer = window.setInterval(() => render(current + 1), 3600);
    };

    const stop = () => {
      if (!timer) return;
      window.clearInterval(timer);
      timer = null;
    };

    slider.addEventListener("mouseenter", stop);
    slider.addEventListener("mouseleave", start);
    slider.addEventListener("touchstart", stop, { passive: true });
    slider.addEventListener("touchend", start);

    render(0);
    start();
  };

  setupHeroPreviewSlider();
  setupSellerSlider();

  const navbar = document.getElementById("mainNavbar");
  const btnTop = document.getElementById("btnTop");
  const btnWhatsapp = document.getElementById("btnWhatsapp");
  const mobileToggle = document.getElementById("mobileMenuToggle");
  const mobileMenu = document.getElementById("mobileMenu");

  navbar?.classList.add("show");

  const onScroll = () => {
    const y = window.scrollY;
    btnTop?.classList.toggle("show", y > 250);
    btnWhatsapp?.classList.toggle("show", y > 250);
  };

  window.addEventListener("scroll", onScroll, { passive: true });
  window.addEventListener("load", onScroll);

  btnTop?.addEventListener("click", () => window.scrollTo({ top: 0, behavior: "smooth" }));
  mobileToggle?.addEventListener("click", () => mobileMenu?.classList.toggle("show"));

  const mapFrames = document.querySelectorAll("[data-map-frame]");
  if ("IntersectionObserver" in window) {
    const obs = new IntersectionObserver((entries, observer) => {
      entries.forEach((entry) => {
        if (!entry.isIntersecting) return;
        const frame = entry.target;
        const src = frame.getAttribute("data-src");
        if (src) frame.setAttribute("src", src);
        observer.unobserve(frame);
      });
    }, { rootMargin: "300px" });
    mapFrames.forEach((f) => obs.observe(f));
  }

})();
