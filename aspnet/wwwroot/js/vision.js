/* ============================================================
   ✨ The Vision — page animations (anime.js v3, global `anime`)
   Performance contract:
     • only transform / opacity (+ SVG stroke-dashoffset and
       JS-driven counter text) are animated
     • all scroll triggers use IntersectionObserver
     • prefers-reduced-motion: reduce → skip everything, the
       markup/CSS already shows final states
   ============================================================ */
(function () {
    'use strict';

    function showEverything() {
        document.querySelectorAll('.v-reveal').forEach(function (el) {
            el.style.opacity = '1';
        });
    }

    if (typeof anime === 'undefined') { showEverything(); return; }

    var reduceMotion = window.matchMedia &&
        window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduceMotion) return; /* CSS shows final states; nothing to do */

    /* ── 1. Hero headline — per-letter staggered reveal ──────── */
    function splitHeadline() {
        var title = document.querySelector('.vision-title');
        if (!title) return [];

        var words = title.textContent.trim().split(/\s+/);
        title.textContent = '';
        var letters = [];

        words.forEach(function (word, w) {
            var wordEl = document.createElement('span');
            wordEl.className = 'v-word';
            var isGrad = word.indexOf('Learns') === 0; /* the gradient word */
            var chars = Array.from(word);

            chars.forEach(function (ch, i) {
                var s = document.createElement('span');
                s.className = 'v-letter' + (isGrad ? ' v-letter-grad' : '');
                s.textContent = ch;
                if (isGrad) {
                    /* slide a 600%-wide gradient across the split spans so
                       it reads as one continuous gradient */
                    var p = chars.length > 1 ? (i / (chars.length - 1)) * 100 : 0;
                    s.style.backgroundPosition = p + '% 0';
                }
                wordEl.appendChild(s);
                letters.push(s);
            });

            title.appendChild(wordEl);
            if (w < words.length - 1) title.appendChild(document.createTextNode(' '));
        });
        return letters;
    }

    var letters = splitHeadline();
    if (letters.length) {
        anime.set(letters, { opacity: 0, translateY: '0.55em', rotate: '8deg' });
        anime({
            targets: letters,
            opacity: [0, 1],
            translateY: ['0.55em', '0em'],
            rotate: ['8deg', '0deg'],
            easing: 'easeOutExpo',
            duration: 950,
            delay: anime.stagger(34, { start: 250 })
        });
    }

    /* eyebrow + sub + CTAs fade in after the headline starts */
    anime({
        targets: ['.vision-hero .eyebrow-pill', '.vision-sub', '.vision-hero .hero-ctas'],
        opacity: [0, 1],
        translateY: [14, 0],
        easing: 'easeOutCubic',
        duration: 800,
        delay: anime.stagger(160, { start: 700 })
    });

    /* ── 2. Floating orbs — gentle endless drift (transform only) */
    document.querySelectorAll('.v-orb').forEach(function (orb, i) {
        function drift() {
            anime({
                targets: orb,
                translateX: anime.random(-28, 28),
                translateY: anime.random(-34, 34),
                scale: anime.random(80, 120) / 100,
                easing: 'easeInOutSine',
                duration: anime.random(3200, 5200),
                delay: i * 180,
                complete: drift
            });
        }
        drift();
    });

    /* ── Shared IntersectionObserver factory ─────────────────── */
    function onVisible(els, handler, threshold) {
        if (!els.length) return;
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (e) {
                if (!e.isIntersecting) return;
                io.unobserve(e.target);
                handler(e.target);
            });
        }, { threshold: threshold || 0.35 });
        els.forEach(function (el) { io.observe(el); });
    }

    /* ── 3. Metrics — cards pop in, numbers count up ─────────── */
    var metricCards = Array.prototype.slice.call(document.querySelectorAll('.v-metric'));
    onVisible(metricCards, function (card) {
        var index = metricCards.indexOf(card);
        anime({
            targets: card,
            opacity: [0, 1],
            translateY: [26, 0],
            scale: [0.96, 1],
            easing: 'easeOutCubic',
            duration: 650,
            delay: index * 90
        });

        var value = card.querySelector('[data-v-count]');
        if (value) {
            var target = parseFloat(value.getAttribute('data-v-count')) || 0;
            var prefix = value.getAttribute('data-v-prefix') || '';
            var suffix = value.getAttribute('data-v-suffix') || '';
            var decimals = parseInt(value.getAttribute('data-v-decimals') || '0', 10);
            var from = parseFloat(value.getAttribute('data-v-from') || '0');
            var state = { n: from };
            anime({
                targets: state,
                n: target,
                easing: 'easeOutExpo',
                duration: 1700,
                delay: index * 90 + 150,
                update: function () {
                    value.textContent = prefix + state.n.toFixed(decimals) + suffix;
                }
            });
        }

        /* the ∞ card just pops — infinity resists interpolation */
        var pop = card.querySelector('[data-v-pop]');
        if (pop) {
            anime({
                targets: pop,
                opacity: [0, 1],
                scale: [0.2, 1],
                easing: 'spring(1, 70, 9, 0)',
                delay: index * 90 + 200
            });
        }
    }, 0.4);

    /* counters animate from 0 — blank the markup's final values now */
    metricCards.forEach(function (card) {
        var value = card.querySelector('[data-v-count]');
        if (!value) return;
        var prefix = value.getAttribute('data-v-prefix') || '';
        var suffix = value.getAttribute('data-v-suffix') || '';
        var decimals = parseInt(value.getAttribute('data-v-decimals') || '0', 10);
        var from = parseFloat(value.getAttribute('data-v-from') || '0');
        value.textContent = prefix + from.toFixed(decimals) + suffix;
    });

    /* ── 4. SVG line drawing — neural roulette draws itself ──── */
    var svg = document.querySelector('.vision-svg');
    if (svg) {
        var strokes = svg.querySelectorAll('.v-stroke');
        strokes.forEach(function (path) {
            var len = anime.setDashoffset(path);
            path.setAttribute('stroke-dasharray', len);
        });
        onVisible([svg], function () {
            anime({
                targets: strokes,
                strokeDashoffset: [anime.setDashoffset, 0],
                easing: 'easeInOutSine',
                duration: 1500,
                delay: anime.stagger(70)
            });
            /* slow ceremonial spin of the wheel group, once drawn */
            var wheel = svg.querySelector('#v-wheel-group');
            if (wheel) {
                anime({
                    targets: wheel,
                    rotate: 360,
                    easing: 'linear',
                    duration: 60000,
                    loop: true,
                    delay: 1800
                });
            }
        }, 0.3);
    }

    /* ── 5. Roadmap timeline — staggered slide-in cards ──────── */
    var tlItems = Array.prototype.slice.call(document.querySelectorAll('.v-tl-item'));
    onVisible(tlItems, function (item) {
        anime({
            targets: item,
            opacity: [0, 1],
            translateX: [-34, 0],
            easing: 'easeOutCubic',
            duration: 700
        });
    }, 0.25);

    /* ── 6. Section headings — quiet fade-up ─────────────────── */
    onVisible(Array.prototype.slice.call(
        document.querySelectorAll('.vision-section-head, .vision-svg-wrap, .vision-svg-caption')
    ), function (el) {
        anime({
            targets: el,
            opacity: [0, 1],
            translateY: [18, 0],
            easing: 'easeOutCubic',
            duration: 700
        });
    }, 0.2);

    /* ── 7. Closing CTA — reveal, then breathe forever ───────── */
    var cta = document.querySelector('.vision-cta');
    if (cta) {
        onVisible([cta], function () {
            anime({
                targets: cta,
                opacity: [0, 1],
                translateY: [24, 0],
                scale: [0.985, 1],
                easing: 'easeOutCubic',
                duration: 750,
                complete: function () {
                    var pulse = cta.querySelector('.v-cta-pulse');
                    if (!pulse) return;
                    anime({
                        targets: pulse,
                        scale: [1, 1.045],
                        easing: 'easeInOutSine',
                        duration: 1100,
                        direction: 'alternate',
                        loop: true
                    });
                }
            });
        }, 0.4);
    }
})();
