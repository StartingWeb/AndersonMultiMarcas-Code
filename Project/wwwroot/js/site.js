// Lazy-load video sources with data-src to prevent immediate download.
(() => {
  const videos = document.querySelectorAll("video[data-src]");
  if (!videos.length) return;

  const loadVideo = (video) => {
    if (!video.dataset.src) return;
    video.src = video.dataset.src;
    video.removeAttribute("data-src");
    video.load();
  };

  if (!("IntersectionObserver" in window)) {
    videos.forEach(loadVideo);
    return;
  }

  const observer = new IntersectionObserver(
    (entries, obs) => {
      entries.forEach((entry) => {
        if (!entry.isIntersecting) return;
        loadVideo(entry.target);
        obs.unobserve(entry.target);
      });
    },
    { rootMargin: "300px 0px" }
  );

  videos.forEach((video) => observer.observe(video));
})();
