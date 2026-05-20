// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

class CasinoPicker {
    constructor(opts) {
        this.fieldId   = opts.fieldId;
        this.mode      = opts.mode || 'date';
        this.isHr      = (navigator.language || 'en').startsWith('hr');

        this.$hidden  = $('#' + this.fieldId);
        this.$display = $('#' + this.fieldId + '_display');
        this.$popup   = $('#' + this.fieldId + '_cal');

        // calendar view state
        var now = new Date();
        this.viewYear  = now.getFullYear();
        this.viewMonth = now.getMonth();
        this.selYear   = null;
        this.selMonth  = null;
        this.selDay    = null;
        this.selHour   = 12;
        this.selMin    = 0;

        // pre-fill if editing
        if (opts.currentIso) {
            var d = new Date(opts.currentIso);
            if (!isNaN(d.getTime())) {
                this.viewYear  = d.getFullYear();
                this.viewMonth = d.getMonth();
                this.selYear   = d.getFullYear();
                this.selMonth  = d.getMonth();
                this.selDay    = d.getDate();
                this.selHour   = d.getHours();
                this.selMin    = d.getMinutes();
                this.$display.val(this._fmt(d));
            }
        }

        this._bind();
    }

    // ── Format for display ────────────────────────────────────────
    _fmt(d) {
        var dd   = String(d.getDate()).padStart(2,'0');
        var mm   = String(d.getMonth()+1).padStart(2,'0');
        var yyyy = d.getFullYear();
        var HH   = String(d.getHours()).padStart(2,'0');
        var mn   = String(d.getMinutes()).padStart(2,'0');
        if (this.isHr) {
            return this.mode === 'datetime'
                ? dd+'.'+mm+'.'+yyyy+' '+HH+':'+mn
                : dd+'.'+mm+'.'+yyyy;
        }
        return this.mode === 'datetime'
            ? mm+'/'+dd+'/'+yyyy+' '+HH+':'+mn
            : mm+'/'+dd+'/'+yyyy;
    }

    // ── ISO for hidden input ──────────────────────────────────────
    _iso(d) {
        var yyyy = d.getFullYear();
        var mm   = String(d.getMonth()+1).padStart(2,'0');
        var dd   = String(d.getDate()).padStart(2,'0');
        if (this.mode === 'datetime') {
            var HH = String(d.getHours()).padStart(2,'0');
            var mn = String(d.getMinutes()).padStart(2,'0');
            return yyyy+'-'+mm+'-'+dd+'T'+HH+':'+mn;
        }
        return yyyy+'-'+mm+'-'+dd;
    }

    // ── Render calendar HTML ──────────────────────────────────────
    _render() {
        var MO_HR = ['Siječanj','Veljača','Ožujak','Travanj','Svibanj','Lipanj',
                     'Srpanj','Kolovoz','Rujan','Listopad','Studeni','Prosinac'];
        var MO_EN = ['January','February','March','April','May','June',
                     'July','August','September','October','November','December'];
        var DN_HR = ['Pon','Uto','Sri','Čet','Pet','Sub','Ned'];
        var DN_EN = ['Mon','Tue','Wed','Thu','Fri','Sat','Sun'];

        var months = this.isHr ? MO_HR : MO_EN;
        var days   = this.isHr ? DN_HR : DN_EN;
        var y = this.viewYear, m = this.viewMonth;

        // first weekday of month, Mon=0
        var fd = (new Date(y, m, 1).getDay() + 6) % 7;
        var dim = new Date(y, m+1, 0).getDate();

        var today = new Date();
        var tY = today.getFullYear(), tM = today.getMonth(), tD = today.getDate();

        var h = '<div class="dp-header">'
              + '<button type="button" class="dp-nav dp-prev">&#8249;</button>'
              + '<span class="dp-title">' + months[m] + ' ' + y + '</span>'
              + '<button type="button" class="dp-nav dp-next">&#8250;</button>'
              + '</div><div class="dp-grid">';

        days.forEach(function(d) { h += '<div class="dp-day-head">'+d+'</div>'; });

        for (var i=0; i<fd; i++) h += '<div class="dp-cell"></div>';

        for (var d=1; d<=dim; d++) {
            var cls = 'dp-cell dp-day';
            if (d===tD && m===tM && y===tY)           cls += ' dp-today';
            if (d===this.selDay && m===this.selMonth && y===this.selYear) cls += ' dp-selected';
            h += '<div class="'+cls+'" data-d="'+d+'">'+d+'</div>';
        }
        h += '</div>';

        if (this.mode === 'datetime') {
            h += '<div class="dp-time">'
               + '<span class="dp-time-label">'+(this.isHr?'Vrijeme:':'Time:')+'</span>'
               + '<input type="number" class="dp-time-input dp-hour" min="0" max="23" value="'
               +    String(this.selHour).padStart(2,'0')+'" />'
               + '<span class="dp-time-sep">:</span>'
               + '<input type="number" class="dp-time-input dp-min" min="0" max="59" value="'
               +    String(this.selMin).padStart(2,'0')+'" />'
               + '</div>';
        }

        h += '<div class="dp-footer">'
           + '<button type="button" class="dp-clear">'+(this.isHr?'Očisti':'Clear')+'</button>'
           + '<button type="button" class="dp-ok btn btn-sm btn-primary">'+(this.isHr?'Potvrdi':'OK')+'</button>'
           + '</div>';

        return h;
    }

    // ── Open / close popup ────────────────────────────────────────
    _open() {
        this.$popup.html(this._render()).show();
        this._bindCal();
    }
    _close() { this.$popup.hide(); }

    // ── Commit current selection ──────────────────────────────────
    _commit() {
        if (this.selDay === null) return;
        var H = this.mode === 'datetime'
            ? parseInt(this.$popup.find('.dp-hour').val()) || 0 : 0;
        var M = this.mode === 'datetime'
            ? parseInt(this.$popup.find('.dp-min').val())  || 0 : 0;
        this.selHour = H; this.selMin = M;
        var d = new Date(this.selYear, this.selMonth, this.selDay, H, M);
        this.$display.val(this._fmt(d));
        this.$hidden.val(this._iso(d));
        // clear any validation message
        this.$hidden.valid && this.$hidden.valid();
    }

    // ── Bind outer events ─────────────────────────────────────────
    _bind() {
        var self = this;

        this.$display.on('click', function(e) {
            e.stopPropagation();
            if (self.$popup.is(':visible')) self._close();
            else self._open();
        });

        // validate on blur
        this.$display.on('blur', function() {
            setTimeout(function() {
                if (!self.$popup.is(':visible') && !self.$hidden.val()) {
                    // trigger jQuery validate on the hidden field
                    if ($.fn.validate) {
                        self.$hidden.closest('form').validate().element(self.$hidden[0]);
                    }
                }
            }, 200);
        });

        $(document).on('click.dp_' + this.fieldId, function() { self._close(); });
        this.$popup.on('click', function(e) { e.stopPropagation(); });
    }

    // ── Bind calendar events (re-run after each re-render) ────────
    _bindCal() {
        var self = this;

        this.$popup.find('.dp-prev').on('click', function() {
            if (--self.viewMonth < 0) { self.viewMonth = 11; self.viewYear--; }
            self.$popup.html(self._render()); self._bindCal();
        });
        this.$popup.find('.dp-next').on('click', function() {
            if (++self.viewMonth > 11) { self.viewMonth = 0; self.viewYear++; }
            self.$popup.html(self._render()); self._bindCal();
        });

        this.$popup.find('.dp-day').on('click', function() {
            self.selDay   = parseInt($(this).data('d'));
            self.selYear  = self.viewYear;
            self.selMonth = self.viewMonth;
            if (self.mode === 'datetime') {
                // re-render to highlight + preserve time inputs
                self.selHour = parseInt(self.$popup.find('.dp-hour').val()) || self.selHour;
                self.selMin  = parseInt(self.$popup.find('.dp-min').val())  || self.selMin;
            }
            self.$popup.html(self._render()); self._bindCal();
            if (self.mode === 'date') { self._commit(); self._close(); }
        });

        this.$popup.find('.dp-ok').on('click', function() {
            self._commit(); self._close();
        });

        this.$popup.find('.dp-clear').on('click', function() {
            self.selDay = self.selYear = self.selMonth = null;
            self.$display.val(''); self.$hidden.val('');
            self._close();
        });
    }
}

/* ============================================================
   NervSystem — Evangelion / NERV animation layer
   All animations check prefers-reduced-motion before running.
   ============================================================ */
const NervSystem = (() => {

    // ── Motion preference check ──────────────────────────────
    function motionOk() {
        return !window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    }

    // ── Utility: sleep (Promise-based) ───────────────────────
    function sleep(ms) {
        return new Promise(function(resolve) { setTimeout(resolve, ms); });
    }

    // ── Utility: type text char-by-char into an element ─────
    function typeText(el, text, speed) {
        speed = speed || 30;
        return new Promise(function(resolve) {
            var i = 0;
            var timer = setInterval(function() {
                el.textContent += text[i];
                i++;
                if (i >= text.length) {
                    clearInterval(timer);
                    resolve();
                }
            }, speed);
        });
    }

    // ── 1. NERV Boot Sequence ────────────────────────────────
    function boot() {
        if (sessionStorage.getItem('nerv_boot')) return;
        if (!motionOk()) {
            sessionStorage.setItem('nerv_boot', '1');
            return;
        }

        var overlay = document.createElement('div');
        overlay.id = 'nerv-boot';
        overlay.style.opacity = '0';
        overlay.innerHTML = [
            '<div class="nerv-boot-inner">',
            '  <div class="nerv-logo">NERV</div>',
            '  <div class="nerv-subtitle">CENTRAL DOGMA NETWORK</div>',
            '  <div class="magi-status" id="magi-lines"></div>',
            '  <div class="nerv-sync-wrap">',
            '    <span class="nerv-sync-label">SYNCHRONIZATION</span>',
            '    <div class="nerv-sync-bar"><div class="nerv-sync-fill" id="nerv-sync-fill"></div></div>',
            '    <span class="nerv-sync-pct" id="nerv-sync-pct">0%</span>',
            '  </div>',
            '  <div class="nerv-launch-text" id="nerv-launch-text"></div>',
            '</div>'
        ].join('');
        document.body.appendChild(overlay);

        // fade in
        requestAnimationFrame(function() {
            requestAnimationFrame(function() {
                overlay.style.opacity = '1';
            });
        });

        (async function() {
            await sleep(200);

            // Step 1: type MAGI lines
            var magiLines = [
                '◈ CASPER ──────────── ONLINE',
                '◈ BALTHASAR ────────── ONLINE',
                '◈ MELCHIOR ────────────── ONLINE'
            ];
            var magiEl = document.getElementById('magi-lines');
            for (var li = 0; li < magiLines.length; li++) {
                var lineEl = document.createElement('div');
                magiEl.appendChild(lineEl);
                await typeText(lineEl, magiLines[li], 28);
                await sleep(300);
            }

            // Step 2: sync bar 0 → 100% via rAF
            await (function() {
                return new Promise(function(resolve) {
                    var fillEl = document.getElementById('nerv-sync-fill');
                    var pctEl  = document.getElementById('nerv-sync-pct');
                    var start  = null;
                    var duration = 600;
                    function step(ts) {
                        if (!start) start = ts;
                        var progress = Math.min((ts - start) / duration, 1);
                        var pct = Math.round(progress * 100);
                        fillEl.style.width = pct + '%';
                        pctEl.textContent  = pct + '%';
                        if (progress < 1) {
                            requestAnimationFrame(step);
                        } else {
                            resolve();
                        }
                    }
                    requestAnimationFrame(step);
                });
            })();

            // Step 3: type launch text
            var launchEl = document.getElementById('nerv-launch-text');
            await typeText(launchEl, 'CASINO MANAGEMENT SYSTEM — LAUNCH AUTHORIZED', 30);

            await sleep(400);

            // Step 4: white flash
            overlay.style.transition = 'none';
            overlay.style.background = '#ffffff';
            await sleep(80);
            overlay.style.background = '#050305';

            // Step 5: fade out and remove
            await sleep(120);
            overlay.style.transition = 'opacity 0.4s ease';
            overlay.style.opacity = '0';
            await sleep(420);
            if (overlay.parentNode) overlay.parentNode.removeChild(overlay);

            sessionStorage.setItem('nerv_boot', '1');
        })();
    }

    // ── 2. Entity Card Entrance (IntersectionObserver) ───────
    function cardEntrance() {
        if (!motionOk()) return;
        if (!('IntersectionObserver' in window)) return;

        var cards = document.querySelectorAll('.entity-card-wrap');
        if (!cards.length) return;

        cards.forEach(function(card) {
            card.style.transform = 'translateY(48px) scaleY(0.96)';
            card.style.opacity   = '0';
            card.style.transition = 'none';
        });

        var observer = new IntersectionObserver(function(entries) {
            entries.forEach(function(entry) {
                if (!entry.isIntersecting) return;
                var card  = entry.target;
                var index = parseInt(card.dataset.nervIdx || '0', 10);
                setTimeout(function() {
                    card.style.transition = 'transform 420ms cubic-bezier(0.22,1,0.36,1), opacity 380ms ease';
                    card.style.transform  = 'translateY(0) scaleY(1)';
                    card.style.opacity    = '1';
                    card.classList.add('eva-launch-flash');
                    setTimeout(function() { card.classList.remove('eva-launch-flash'); }, 620);
                }, index * 60);
                observer.unobserve(card);
            });
        }, { threshold: 0.1 });

        cards.forEach(function(card, idx) {
            card.dataset.nervIdx = idx;
            observer.observe(card);
        });
    }

    // ── 3. AT Field hover (JS half — CSS does the hex grid) ──
    function atField() {
        if (!motionOk()) return;
        $(document).on('mouseenter', '.entity-card', function() {
            var $card = $(this);
            $card.addClass('at-field-active');
            setTimeout(function() { $card.removeClass('at-field-active'); }, 520);
        });
    }

    // ── 4. NERV Delete Confirmation Modal ────────────────────
    function deleteModal() {
        // inject modal once
        if (!document.getElementById('nerv-modal')) {
            var el = document.createElement('div');
            el.id = 'nerv-modal';
            el.style.display = 'none';
            el.innerHTML = [
                '<div class="nerv-modal-box">',
                '  <div class="nerv-modal-header">',
                '    <span class="nerv-modal-alert">⚠ NERV WARNING ⚠</span>',
                '  </div>',
                '  <div class="nerv-modal-body">',
                '    <div class="nerv-modal-msg">SELF-DESTRUCT SEQUENCE INITIATED</div>',
                '    <div class="nerv-modal-sub" id="nerv-modal-sub">THIS ACTION CANNOT BE REVERSED</div>',
                '    <div class="nerv-modal-countdown" id="nerv-modal-countdown"></div>',
                '  </div>',
                '  <div class="nerv-modal-footer">',
                '    <button class="nerv-btn-cancel" id="nerv-cancel">ABORT</button>',
                '    <button class="nerv-btn-confirm" id="nerv-confirm">EXECUTE</button>',
                '  </div>',
                '</div>'
            ].join('');
            document.body.appendChild(el);
        }

        var $modal      = $('#nerv-modal');
        var $countdown  = $('#nerv-modal-countdown');
        var $confirmBtn = $('#nerv-confirm');
        var $cancelBtn  = $('#nerv-cancel');
        var countdownTimer = null;
        var pendingForm    = null;

        function showModal(form) {
            pendingForm = form;
            $confirmBtn.prop('disabled', true);
            $modal.css('display', 'flex');

            var remaining = 3;
            $countdown.text('0:0' + remaining);
            countdownTimer = setInterval(function() {
                remaining--;
                if (remaining > 0) {
                    $countdown.text('0:0' + remaining);
                } else {
                    clearInterval(countdownTimer);
                    $countdown.text('');
                    $confirmBtn.prop('disabled', false);
                }
            }, 1000);
        }

        function hideModal() {
            clearInterval(countdownTimer);
            $modal.css('display', 'none');
            pendingForm = null;
        }

        $(document).on('submit', 'form[action*="/obrisi"]', function(e) {
            e.preventDefault();
            e.stopPropagation();
            showModal(this);
        });

        $cancelBtn.on('click', function() { hideModal(); });

        $confirmBtn.on('click', function() {
            if ($confirmBtn.prop('disabled')) return;
            var form = pendingForm;
            hideModal();
            if (form) {
                // mark so our submit intercept ignores it
                $(form).data('nerv-deleting', true);
                form.submit();
            }
        });
    }

    // ── 5. Validation hooks (Pattern Red + Sync OK) ──────────
    function validationHooks() {
        if (!motionOk()) return;

        // MutationObserver on .edit-form-wrap for validation span changes
        var formWrap = document.querySelector('.edit-form-wrap');
        if (formWrap) {
            var observer = new MutationObserver(function(mutations) {
                mutations.forEach(function(mutation) {
                    var target = mutation.target;
                    if (target.classList && target.classList.contains('form-validation')) {
                        var field = target.closest('.form-field');
                        if (!field) return;
                        if (target.textContent.trim() !== '') {
                            // error appeared — Pattern Red
                            field.classList.add('nerv-pattern-red');
                            setTimeout(function() { field.classList.remove('nerv-pattern-red'); }, 820);
                        }
                    }
                });
            });
            observer.observe(formWrap, { subtree: true, characterData: true, childList: true });
        }

        // blur handler for Sync OK
        $(document).on('blur', '.form-input', function() {
            var $input = $(this);
            // check validity via jQuery validate if available
            var valid = false;
            if ($.fn.validate) {
                var form = $input.closest('form');
                if (form.length && form.data('validator')) {
                    valid = form.validate().element($input[0]);
                }
            } else {
                valid = this.checkValidity ? this.checkValidity() : true;
            }
            if (valid) {
                $input.addClass('nerv-sync-ok');
                setTimeout(function() { $input.removeClass('nerv-sync-ok'); }, 520);
            }
        });
    }

    // ── 6. MAGI Search Scanner ───────────────────────────────
    function searchScan() {
        if (!motionOk()) return;

        var $searchInput = $('#searchInput');
        if (!$searchInput.length) return;

        var $searchBar = $searchInput.closest('.search-bar');
        if (!$searchBar.length) $searchBar = $searchInput.parent();

        var scanTimer   = null;
        var $statusLabel = null;

        $searchInput.on('input', function() {
            // add scanning class
            $searchBar.addClass('magi-scanning');

            // inject status label if not present
            if (!$statusLabel || !$statusLabel.parent().length) {
                $statusLabel = $('<div class="magi-status-label">MAGI PROCESSING...</div>');
                $searchBar.after($statusLabel);
            }

            // reset debounce timer
            clearTimeout(scanTimer);
            scanTimer = setTimeout(function() {
                $searchBar.removeClass('magi-scanning');
                if ($statusLabel) {
                    $statusLabel.remove();
                    $statusLabel = null;
                }
            }, 1000);
        });
    }

    // ── 7. Brand Logo Periodic Glitch ────────────────────────
    function glitch() {
        if (!motionOk()) return;

        function doGlitch() {
            var $brand = $('.brand');
            $brand.addClass('brand-glitch');
            setTimeout(function() { $brand.removeClass('brand-glitch'); }, 320);
        }

        // schedule first glitch randomly between 8–12s, then repeat
        function schedule() {
            setTimeout(function() {
                doGlitch();
                schedule();
            }, 8000 + Math.random() * 4000);
        }
        schedule();
    }

    // ── 8. Synchronization Complete flash on form submit ─────
    function syncFlash() {
        if (!motionOk()) return;

        // inject overlay
        if (!document.getElementById('nerv-sync-complete')) {
            var el = document.createElement('div');
            el.id = 'nerv-sync-complete';
            el.innerHTML = '<div class="nerv-complete-text">◈ SYNCHRONIZATION COMPLETE ◈</div>';
            document.body.appendChild(el);
        }

        $(document).on('submit', '.edit-form-wrap form', function(e) {
            var $form = $(this);
            if ($form.data('nerv-submitting')) return; // second pass — allow through

            // skip invalid forms (let jQuery validate handle them)
            if ($.fn.validate && $form.data('validator')) {
                if (!$form.validate().form()) return;
            }

            e.preventDefault();

            var $overlay = $('#nerv-sync-complete');
            $overlay.addClass('active');

            setTimeout(function() {
                $overlay.removeClass('active');
                $form.data('nerv-submitting', true);
                $form.submit();
            }, 600);
        });
    }

    // ── 9. SEELE Toast Easter Egg ────────────────────────────
    function easterEgg() {
        if (!motionOk()) return;

        var path = window.location.pathname;
        var isHome = (path === '/' || path === '/home' || path === '/Home' ||
                      path.toLowerCase() === '/home/index');
        if (!isHome) return;

        // inject toast
        var toast = document.createElement('div');
        toast.className = 'seele-toast';
        toast.id = 'seele-toast';
        toast.innerHTML = [
            '<span class="seele-toast-icon">◈</span>',
            '<span class="seele-toast-msg">THIRD IMPACT CONTINGENCY: NOMINAL</span>'
        ].join('');
        document.body.appendChild(toast);

        setTimeout(function() {
            toast.classList.add('show');
            setTimeout(function() {
                toast.classList.remove('show');
                setTimeout(function() {
                    if (toast.parentNode) toast.parentNode.removeChild(toast);
                }, 450);
            }, 4000);
        }, 3000);
    }

    // ── Public API ───────────────────────────────────────────
    return {
        boot:             boot,
        cardEntrance:     cardEntrance,
        atField:          atField,
        deleteModal:      deleteModal,
        validationHooks:  validationHooks,
        searchScan:       searchScan,
        glitch:           glitch,
        syncFlash:        syncFlash,
        easterEgg:        easterEgg,
        init: function() {
            this.boot();
            this.cardEntrance();
            this.atField();
            this.deleteModal();
            this.validationHooks();
            this.searchScan();
            this.glitch();
            this.syncFlash();
            this.easterEgg();
        }
    };
})();

$(function() { NervSystem.init(); });
