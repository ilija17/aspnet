/* ============================================================
   ✨ Playground — full-screen anime.js demo reel (v3, global `anime`)
   Performance contract:
     • only transform / opacity animate (no layout properties)
     • every looping animation is registered with its section's
       controller and PAUSED via IntersectionObserver the moment
       the section leaves the viewport — nothing loops off-screen
     • element counts capped (308 grid dots, 40 bars, 9 SVG
       pieces, 5 chips)
     • no scroll/resize handlers; prefers-reduced-motion: reduce
       → static final states, zero animation
   ============================================================ */
(function () {
    'use strict';

    var GRID_COLS = 22, GRID_ROWS = 14;   /* 308 dots */
    var BAR_COUNT = 40;

    /* ── Build static DOM first (grid dots + bars) so the page is
          complete even for reduced-motion / no-anime visitors ──── */
    var gridEl = document.getElementById('pgGrid');
    var dots = [];
    if (gridEl) {
        var frag = document.createDocumentFragment();
        for (var d = 0; d < GRID_COLS * GRID_ROWS; d++) {
            var dot = document.createElement('div');
            dot.className = 'pg-dot';
            frag.appendChild(dot);
            dots.push(dot);
        }
        gridEl.appendChild(frag);
    }

    var barsEl = document.getElementById('pgBars');
    var barWraps = [], barFills = [];
    if (barsEl) {
        var bfrag = document.createDocumentFragment();
        for (var b = 0; b < BAR_COUNT; b++) {
            var wrap = document.createElement('div');
            wrap.className = 'pg-bar';
            var fill = document.createElement('div');
            fill.className = 'pg-bar-fill';
            wrap.appendChild(fill);
            bfrag.appendChild(wrap);
            barWraps.push(wrap);
            barFills.push(fill);
        }
        barsEl.appendChild(bfrag);
    }

    function showEverything() {
        document.querySelectorAll('.pg-reveal').forEach(function (el) {
            el.style.opacity = '1';
        });
    }

    if (typeof anime === 'undefined') { showEverything(); return; }

    var reduceMotion = window.matchMedia &&
        window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduceMotion) return; /* CSS already shows static final states */

    /* gentle scroll-snap between the full-screen stages */
    document.documentElement.classList.add('pg-snap');

    /* ── Section controller: starts a section's animations on first
          entry, then pauses/resumes its registered loops as the
          section leaves/re-enters the viewport ─────────────────── */
    function makeSection(el, start) {
        if (!el) return;
        var ctrl = {
            map: {},
            set: function (key, inst) {
                if (this.map[key]) this.map[key].pause();
                this.map[key] = inst;
                return inst;
            },
            each: function (fn) {
                for (var k in this.map) {
                    if (Object.prototype.hasOwnProperty.call(this.map, k)) fn(this.map[k]);
                }
            }
        };
        var started = false;
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (e) {
                if (e.isIntersecting) {
                    if (!started) { started = true; start(ctrl); }
                    else ctrl.each(function (inst) { inst.play(); });
                } else if (started) {
                    ctrl.each(function (inst) { inst.pause(); });
                }
            });
        }, { threshold: 0.12 });
        io.observe(el);
        return ctrl;
    }

    /* one-shot fade-up for a section's .pg-reveal copy */
    function revealCopy(sectionEl) {
        var els = sectionEl.querySelectorAll('.pg-reveal');
        if (!els.length) return;
        anime({
            targets: els,
            opacity: [0, 1],
            translateY: [18, 0],
            easing: 'easeOutCubic',
            duration: 750,
            delay: anime.stagger(130)
        });
    }

    /* split a headline into per-letter spans (gradient-aware) */
    function splitLetters(el, gradPredicate) {
        var words = el.textContent.trim().split(/\s+/);
        el.textContent = '';
        el.classList.add('pg-split');
        var letters = [];
        words.forEach(function (word, w) {
            var wordEl = document.createElement('span');
            wordEl.className = 'pg-word';
            var isGrad = gradPredicate ? gradPredicate(word, w) : false;
            var chars = Array.from(word);
            chars.forEach(function (ch, i) {
                var s = document.createElement('span');
                s.className = 'pg-letter' + (isGrad ? ' pg-letter-grad' : '');
                s.textContent = ch;
                if (isGrad) {
                    var p = chars.length > 1 ? (i / (chars.length - 1)) * 100 : 0;
                    s.style.backgroundPosition = p + '% 0';
                }
                wordEl.appendChild(s);
                letters.push(s);
            });
            el.appendChild(wordEl);
            if (w < words.length - 1) el.appendChild(document.createTextNode(' '));
        });
        return letters;
    }

    /* ════ 01 · Hero — staggered grid ripple + kinetic headline ═══ */
    var heroSection = document.getElementById('pg-hero');
    makeSection(heroSection, function (ctrl) {
        /* per-letter headline reveal (one-shot) */
        var title = document.getElementById('pgTitle');
        if (title) {
            var letters = splitLetters(title, function (word) {
                return word.indexOf('agentic') === 0;
            });
            anime.set(letters, { opacity: 0, translateY: '0.6em', rotate: '10deg' });
            anime({
                targets: letters,
                opacity: [0, 1],
                translateY: ['0.6em', '0em'],
                rotate: ['10deg', '0deg'],
                easing: 'easeOutExpo',
                duration: 900,
                delay: anime.stagger(30, { start: 200 })
            });
        }
        revealCopy(heroSection);

        /* the animejs.com signature: grid ripple from center, looping */
        if (dots.length) {
            ctrl.set('ripple', anime({
                targets: dots,
                scale: [
                    { value: 0.35, duration: 550, easing: 'easeInOutSine' },
                    { value: 1.0,  duration: 550, easing: 'easeInOutSine' }
                ],
                opacity: [
                    { value: 0.25, duration: 550, easing: 'easeInOutSine' },
                    { value: 0.95, duration: 550, easing: 'easeInOutSine' }
                ],
                delay: anime.stagger(42, { grid: [GRID_COLS, GRID_ROWS], from: 'center' }),
                loop: true
            }));
        }
    });

    /* ════ 02 · Stagger symphony — dancing bars + click ripple ════ */
    var barsSection = document.getElementById('pg-bars-section');
    makeSection(barsSection, function (ctrl) {
        revealCopy(barsSection);

        /* endless dance: each fill gets its own random range/tempo
           (function-based values), alternating forever */
        ctrl.set('dance', anime({
            targets: barFills,
            scaleY: function () {
                return [0.12 + Math.random() * 0.25, 0.5 + Math.random() * 0.5];
            },
            duration: function () { return anime.random(850, 1500); },
            easing: 'easeInOutSine',
            direction: 'alternate',
            loop: true,
            delay: anime.stagger(22)
        }));

        /* market correction: ripple outward from the clicked bar
           (wrappers translate; fills keep dancing untouched) */
        barsEl.addEventListener('click', function (e) {
            var rect = barsEl.getBoundingClientRect();
            var idx = Math.floor(((e.clientX - rect.left) / rect.width) * BAR_COUNT);
            idx = Math.max(0, Math.min(BAR_COUNT - 1, idx));
            ctrl.set('correction', anime({
                targets: barWraps,
                translateY: [
                    { value: -30, duration: 240, easing: 'easeOutQuad' },
                    { value: 0,   duration: 480, easing: 'easeOutElastic(1, 0.5)' }
                ],
                delay: anime.stagger(16, { from: idx })
            }));
        });
    });

    /* ════ 03 · Timeline orchestration — chip assembles itself ════ */
    var chipSection = document.getElementById('pg-chip-section');
    makeSection(chipSection, function (ctrl) {
        revealCopy(chipSection);

        var pieces = chipSection.querySelectorAll('.pg-piece');
        if (pieces.length) {
            /* scatter the parts, then choreograph them home; the
               timeline alternates so it disassembles again forever */
            anime.set(pieces, {
                translateX: function () { return anime.random(-170, 170); },
                translateY: function () { return anime.random(-130, 130); },
                rotate: function () { return anime.random(-180, 180); },
                scale: 0.4,
                opacity: 0
            });
            var tl = anime.timeline({
                loop: true,
                direction: 'alternate',
                endDelay: 1100
            });
            tl.add({
                targets: pieces,
                translateX: 0,
                translateY: 0,
                rotate: 0,
                scale: 1,
                opacity: 1,
                duration: 850,
                easing: 'easeOutBack',
                delay: anime.stagger(95)
            });
            tl.add({
                targets: '#pgChipCore',
                scale: [1, 1.14, 1],
                duration: 520,
                easing: 'easeInOutSine'
            }, '-=120');
            ctrl.set('assemble', tl);

            /* slow ceremonial spin of the assembled whole */
            ctrl.set('spin', anime({
                targets: '#pgChipSpin',
                rotate: 360,
                duration: 26000,
                easing: 'linear',
                loop: true
            }));
        }
    });

    /* ════ 04 · Spring physics — autonomous hops + pokeable chips ═ */
    var springSection = document.getElementById('pg-springs-section');
    makeSection(springSection, function (ctrl) {
        revealCopy(springSection);

        var chips = springSection.querySelectorAll('.pg-chip');
        chips.forEach(function (chip, i) {
            /* autonomous hop loop — rises, then spring-snaps down.
               Recursive so each cycle gets fresh height/timing; the
               controller pauses the live instance off-screen, which
               also halts the recursion until the section returns. */
            function hop() {
                ctrl.set('hop' + i, anime({
                    targets: chip,
                    translateY: [
                        { value: -anime.random(18, 52), duration: 430, easing: 'easeOutCubic' },
                        { value: 0, easing: 'spring(1, 120, 8, 0)' }
                    ],
                    delay: anime.random(150, 1100),
                    complete: hop
                }));
            }
            hop();

            /* poke: squash + spring back (inner face only, so the
               hop on the wrapper never fights it) */
            var face = chip.querySelector('.pg-chip-face');
            chip.addEventListener('pointerdown', function () {
                anime({
                    targets: face,
                    scale: [
                        { value: 0.78, duration: 110, easing: 'easeOutQuad' },
                        { value: 1, easing: 'spring(1, 200, 6, 0)' }
                    ],
                    rotate: [
                        { value: anime.random(-24, 24), duration: 110, easing: 'easeOutQuad' },
                        { value: 0, easing: 'spring(1, 150, 7, 0)' }
                    ]
                });
            });
        });
    });

    /* ════ 05 · Finale — kinetic typography wave ══════════════════ */
    var finaleSection = document.getElementById('pg-finale');
    makeSection(finaleSection, function (ctrl) {
        revealCopy(finaleSection);

        var title = document.getElementById('pgFinaleTitle');
        if (!title) return;
        var letters = splitLetters(title, function () { return true; });

        /* entrance, then an endless mexican wave through the letters */
        anime({
            targets: letters,
            opacity: [0, 1],
            translateY: ['0.5em', '0em'],
            easing: 'easeOutExpo',
            duration: 800,
            delay: anime.stagger(26),
            complete: function () {
                ctrl.set('wave', anime({
                    targets: letters,
                    translateY: [0, -12, 0],
                    easing: 'easeInOutSine',
                    duration: 1000,
                    delay: anime.stagger(38),
                    endDelay: 500,
                    loop: true
                }));
            }
        });
    });
})();
