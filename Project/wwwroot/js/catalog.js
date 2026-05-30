(() => {
  const filterForm = document.querySelector("#catalog-filter-form");
  const catalogShell = document.querySelector("#catalog-shell");
  const filterToggle = document.querySelector("#catalog-filter-toggle");
  const filterToggleText = document.querySelector(".catalog-filter-toggle-text");
  const filterDisclosure = document.querySelector(".filter-disclosure");
  const desktopMediaQuery = window.matchMedia("(min-width: 992px)");

  if (!filterForm) {
    return;
  }

  const syncToggleLabel = () => {
    if (!catalogShell || !filterToggleText) {
      return;
    }

    const collapsed = catalogShell.classList.contains("is-filter-collapsed");
    filterToggleText.textContent = collapsed ? "Expandir filtros" : "Ocultar filtros";
  };

  const syncResponsiveState = (isDesktop) => {
    if (!catalogShell || !filterDisclosure) {
      return;
    }

    if (isDesktop) {
      filterDisclosure.setAttribute("open", "open");
    } else {
      catalogShell.classList.remove("is-filter-collapsed");
      filterDisclosure.removeAttribute("open");
    }

    syncToggleLabel();
  };

  const closeFiltersOnMobile = () => {
    if (desktopMediaQuery.matches || !filterDisclosure) {
      return;
    }

    filterDisclosure.open = false;
    filterDisclosure.removeAttribute("open");
  };

  if (filterToggle && catalogShell) {
    filterToggle.addEventListener("click", () => {
      if (!desktopMediaQuery.matches) {
        if (filterDisclosure) {
          filterDisclosure.toggleAttribute("open");
        }

        return;
      }

      catalogShell.classList.toggle("is-filter-collapsed");
      syncToggleLabel();
    });
  }

  syncResponsiveState(desktopMediaQuery.matches);
  desktopMediaQuery.addEventListener("change", (event) => {
    syncResponsiveState(event.matches);
  });
  window.addEventListener("pageshow", closeFiltersOnMobile);

  let submitTimeout;
  const autoSubmit = () => {
    window.clearTimeout(submitTimeout);
    closeFiltersOnMobile();
    submitTimeout = window.setTimeout(() => filterForm.requestSubmit(), 120);
  };

  filterForm.querySelectorAll("select").forEach((field) => {
    field.addEventListener("change", autoSubmit);

    if (window.jQuery) {
      window.jQuery(field).on(
        "change.catalogAutoSubmit select2:select.catalogAutoSubmit select2:clear.catalogAutoSubmit",
        autoSubmit
      );
    }
  });

  if (!window.jQuery || !window.jQuery.fn || !window.jQuery.fn.select2) {
    return;
  }

  const $brand = window.jQuery('select[data-brand-select2="true"]');
  if (!$brand.length || $brand.hasClass("select2-hidden-accessible")) {
    return;
  }

  $brand.select2({
    theme: "bootstrap-5",
    width: "100%",
    placeholder: "Todas as marcas",
    allowClear: true,
  });

  $brand.on("select2:select select2:clear change", function () {
    const form = this.form || document.querySelector("#catalog-filter-form");
    if (!form || typeof form.requestSubmit !== "function") {
      return;
    }

    window.setTimeout(() => form.requestSubmit(), 50);
  });
})();
