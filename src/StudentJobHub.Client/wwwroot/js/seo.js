// SEO / Open Graph helper (Feature #30).
// Upserts <meta>/<link> tags in <head> instead of adding duplicates next to the
// static defaults in index.html, and restores the defaults when a page leaves.
window.seo = (function () {
    const originals = new Map(); // element -> { attr, value } (value === null => we created it)

    function remember(el, attr, created) {
        if (!originals.has(el)) {
            originals.set(el, { attr: attr, value: created ? null : el.getAttribute(attr) });
        }
    }

    function upsertMeta(attrName, key, value) {
        if (value === undefined || value === null) return;
        let el = document.head.querySelector('meta[' + attrName + '="' + key + '"]');
        let created = false;
        if (!el) {
            el = document.createElement('meta');
            el.setAttribute(attrName, key);
            document.head.appendChild(el);
            created = true;
        }
        remember(el, 'content', created);
        el.setAttribute('content', value);
    }

    function upsertCanonical(href) {
        if (!href) return;
        let el = document.head.querySelector('link[rel="canonical"]');
        let created = false;
        if (!el) {
            el = document.createElement('link');
            el.setAttribute('rel', 'canonical');
            document.head.appendChild(el);
            created = true;
        }
        remember(el, 'href', created);
        el.setAttribute('href', href);
    }

    return {
        set: function (o) {
            if (!o) return;
            upsertMeta('name', 'description', o.description);
            upsertMeta('name', 'robots', o.noIndex ? 'noindex, nofollow' : 'index, follow');
            upsertCanonical(o.url);

            upsertMeta('property', 'og:type', o.type);
            upsertMeta('property', 'og:title', o.title);
            upsertMeta('property', 'og:description', o.description);
            upsertMeta('property', 'og:url', o.url);
            upsertMeta('property', 'og:image', o.image);
            upsertMeta('property', 'og:image:alt', o.title);

            upsertMeta('name', 'twitter:title', o.title);
            upsertMeta('name', 'twitter:description', o.description);
            upsertMeta('name', 'twitter:image', o.image);
            upsertMeta('name', 'twitter:image:alt', o.title);
        },
        reset: function () {
            originals.forEach(function (info, el) {
                if (info.value === null) {
                    el.remove();
                } else {
                    el.setAttribute(info.attr, info.value);
                }
            });
            originals.clear();
        }
    };
})();
