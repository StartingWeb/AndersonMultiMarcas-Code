(function () {
    const progressiveImages = Array.from(document.querySelectorAll('img[data-progressive-image="true"]'));

    if (progressiveImages.length) {
        const markImage = function (image, state) {
            const frame = image.closest('.vehicle-media, .home-ranking-thumb');
            if (!frame) return;

            frame.classList.remove('is-loading');
            frame.classList.add(state);
        };

        progressiveImages.forEach(function (image) {
            if (image.complete && image.naturalWidth > 0) {
                markImage(image, 'is-loaded');
                return;
            }

            if (image.complete) {
                markImage(image, 'is-error');
                return;
            }

            image.addEventListener('load', function () {
                markImage(image, 'is-loaded');
            }, { once: true });

            image.addEventListener('error', function () {
                markImage(image, 'is-error');
            }, { once: true });
        });

        if ('IntersectionObserver' in window) {
            const preloadObserver = new IntersectionObserver(function (entries, observer) {
                entries.forEach(function (entry) {
                    if (!entry.isIntersecting) return;

                    const image = entry.target;
                    image.loading = 'eager';
                    image.fetchPriority = 'high';
                    observer.unobserve(image);
                });
            }, { rootMargin: '900px 0px' });

            progressiveImages
                .filter(function (image) { return image.loading === 'lazy'; })
                .forEach(function (image) { preloadObserver.observe(image); });
        }
    }
})();

(function () {
    const trackerSelector = '[data-track-vehicle-click="true"]';

    function trackVehicleClick(id) {
        if (!id) return;

        const beacon = new Image();
        beacon.src = `/veiculo/${encodeURIComponent(id)}?handler=RegistrarClique&_=${Date.now()}`;
    }

    document.addEventListener('click', function (event) {
        const target = event.target.closest?.(trackerSelector);
        if (!target) return;

        trackVehicleClick(target.getAttribute('data-vehicle-id'));
    }, true);
})();

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
    const roots = Array.from(document.querySelectorAll('[data-search-autocomplete]'));
    if (!roots.length) {
        return;
    }

    const searchIconSvg = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="11" cy="11" r="7"></circle><path d="m20 20-3.5-3.5"></path></svg>';

    function syncBackdropState() {
        const hasOpen = roots.some(function (entry) { return entry.classList.contains('is-open'); });
        document.body.classList.toggle('search-focus-open', hasOpen);
    }

    roots.forEach(function (root, rootIndex) {
        const endpoint = root.getAttribute('data-endpoint');
        const input = root.querySelector('[data-search-input]');
        const results = root.querySelector('[data-search-results]');
        const body = root.querySelector('[data-search-results-body]');
        const loading = root.querySelector('[data-search-loading]');
        if (!endpoint || !input || !results || !body || !loading) {
            return;
        }

        const resultsId = `siteSearchResults-${rootIndex + 1}`;
        results.id = resultsId;
        input.setAttribute('aria-controls', resultsId);

        let timer = null;
        let activeIndex = -1;
        let abortController = null;

        function closeResults() {
            results.hidden = true;
            input.setAttribute('aria-expanded', 'false');
            activeIndex = -1;
            root.classList.remove('is-open');
            syncBackdropState();
        }

        function openResults() {
            results.hidden = false;
            input.setAttribute('aria-expanded', 'true');
            root.classList.add('is-open');
            syncBackdropState();
        }

        function renderGroups(groups) {
            body.innerHTML = '';
            activeIndex = -1;

            if (!groups || !groups.length) {
                closeResults();
                return;
            }

            groups.forEach(function (group) {
                if (!group || !Array.isArray(group.items) || !group.items.length) {
                    return;
                }

                const title = document.createElement('div');
                title.className = 'site-search-group-title';
                title.textContent = group.title || 'Sugestões';
                body.appendChild(title);

                group.items.forEach(function (item) {
                    const anchor = document.createElement('a');
                    anchor.className = 'site-search-item';
                    anchor.href = item.url || '#';
                    anchor.innerHTML = '<span class=\"site-search-item-icon\">' + searchIconSvg + '</span><span class=\"site-search-item-content\"><span class=\"site-search-item-label\"></span><span class=\"site-search-item-meta\"></span></span>';
                    anchor.querySelector('.site-search-item-label').textContent = item.label || '';
                    anchor.querySelector('.site-search-item-meta').textContent = item.meta || '';
                    body.appendChild(anchor);
                });
            });

            if (!body.children.length) {
                closeResults();
                return;
            }

            openResults();
        }

        function updateActive(nextIndex) {
            const items = Array.from(body.querySelectorAll('.site-search-item'));
            if (!items.length) {
                activeIndex = -1;
                return;
            }

            activeIndex = ((nextIndex % items.length) + items.length) % items.length;
            items.forEach(function (item, i) {
                item.classList.toggle('is-active', i === activeIndex);
            });
        }

        async function fetchSuggestions(term) {
            if (abortController) {
                abortController.abort();
            }

            abortController = new AbortController();
            loading.hidden = false;

            try {
                const url = new URL(endpoint, window.location.origin);
                url.searchParams.set('term', term);
                const response = await fetch(url.toString(), {
                    headers: { 'Accept': 'application/json' },
                    signal: abortController.signal
                });

                if (!response.ok) {
                    closeResults();
                    return;
                }

                const payload = await response.json();
                renderGroups(payload.groups || []);
            } catch (err) {
                if (err && err.name !== 'AbortError') {
                    closeResults();
                }
            } finally {
                loading.hidden = true;
            }
        }

        input.addEventListener('input', function () {
            const term = input.value.trim();
            if (timer) {
                window.clearTimeout(timer);
            }

            if (term.length < 2) {
                closeResults();
                return;
            }

            timer = window.setTimeout(function () {
                fetchSuggestions(term);
            }, 190);
        });

        input.addEventListener('keydown', function (event) {
            const items = Array.from(body.querySelectorAll('.site-search-item'));
            if (!items.length || results.hidden) {
                return;
            }

            if (event.key === 'ArrowDown') {
                event.preventDefault();
                updateActive(activeIndex + 1);
            } else if (event.key === 'ArrowUp') {
                event.preventDefault();
                updateActive(activeIndex - 1);
            } else if (event.key === 'Enter' && activeIndex >= 0) {
                event.preventDefault();
                items[activeIndex].click();
            } else if (event.key === 'Escape') {
                closeResults();
            }
        });

        document.addEventListener('click', function (event) {
            if (!root.contains(event.target)) {
                closeResults();
            }
        });
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

    if (!actions.length || !triggers.length) {
        return;
    }

    function normalizePhone(phone) {
        return String(phone || '').replace(/\D+/g, '');
    }

    function buildMessage(sellerName, vehicleTitle) {
        const normalizedTitle = String(vehicleTitle || '').trim();
        if (normalizedTitle) {
            return `Olá ${sellerName}, quero saber mais sobre ${normalizedTitle}.`;
        }

        return `Olá ${sellerName}, quero falar com um vendedor da Anderson Multimarcas.`;
    }

    function updateModalForContext(vehicleTitle) {
        const contextTitle = (vehicleTitle || '').trim();

        if (contextLabel) {
            contextLabel.textContent = contextTitle
                ? `Escolha quem vai te atender sobre: ${contextTitle}`
                : 'Escolha um vendedor para continuar no WhatsApp.';
        }

        actions.forEach(function (action) {
            const sellerName = String(action.getAttribute('data-seller-name') || 'vendedor').trim() || 'vendedor';
            const sellerPhone = normalizePhone(action.getAttribute('data-seller-phone'));

            if (!sellerPhone) {
                action.setAttribute('href', '#');
                action.setAttribute('aria-disabled', 'true');
                action.classList.add('is-disabled');
                return;
            }

            const message = buildMessage(sellerName, contextTitle);
            const href = `https://wa.me/${sellerPhone}?text=${encodeURIComponent(message)}`;
            action.setAttribute('href', href);
            action.setAttribute('target', '_blank');
            action.setAttribute('rel', 'noopener noreferrer');
            action.removeAttribute('aria-disabled');
            action.classList.remove('is-disabled');
        });
    }

    updateModalForContext('');

    triggers.forEach(function (trigger) {
        trigger.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopPropagation();
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

    function loadVideo(video) {
        const source = video.getAttribute('data-src');
        if (source && !video.getAttribute('src')) {
            video.setAttribute('src', source);
            video.load();
        }

        if (video.readyState >= 2) {
            tryPlay(video);
        }
    }

    videos.forEach(function (video) {
        video.addEventListener('canplay', function () {
            tryPlay(video);
        });

        document.addEventListener('visibilitychange', function () {
            if (!document.hidden && video.paused) {
                loadVideo(video);
            }
        });
    });

    function loadVideos() {
        videos.forEach(loadVideo);
    }

    window.addEventListener('load', function () {
        if ('requestIdleCallback' in window) {
            window.requestIdleCallback(loadVideos, { timeout: 2500 });
            return;
        }

        window.setTimeout(loadVideos, 1200);
    }, { once: true });
})();
