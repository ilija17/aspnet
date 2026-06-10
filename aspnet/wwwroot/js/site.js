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
   ✨ PerformativeUI — AI-native interaction layer
   Aurora is pure CSS; this module handles sparkles, counters,
   token streams, word rolls, the AI concierge FAB, and the
   waitlist form. Everything is canned. Nothing calls a model.
   All motion respects prefers-reduced-motion.
   ============================================================ */
const PerformativeUI = (() => {

    function motionOk() {
        return !window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    }

    /* ── FloatingSparkles — one lightweight canvas, ~24 ✦ ───── */
    function sparkles() {
        if (!motionOk()) return;
        var canvas = document.getElementById('sparkle-canvas');
        if (!canvas) return;

        var ctx = canvas.getContext('2d');
        var dpr = Math.min(window.devicePixelRatio || 1, 2);
        var W, H;

        function resize() {
            W = window.innerWidth; H = window.innerHeight;
            canvas.width = W * dpr; canvas.height = H * dpr;
            canvas.style.width = W + 'px'; canvas.style.height = H + 'px';
            ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
        }
        resize();
        window.addEventListener('resize', resize);

        var COLORS = ['#c084fc', '#818cf8', '#22d3ee', '#f0abfc'];
        var COUNT = 24;
        var parts = [];
        for (var i = 0; i < COUNT; i++) {
            parts.push({
                x: Math.random() * window.innerWidth,
                y: Math.random() * window.innerHeight,
                s: 1 + Math.random() * 2.2,           // size
                vy: 0.1 + Math.random() * 0.25,        // drift up
                vx: (Math.random() - 0.5) * 0.15,
                tw: Math.random() * Math.PI * 2,       // twinkle phase
                c: COLORS[(Math.random() * COLORS.length) | 0]
            });
        }

        var last = 0;
        var FRAME = 1000 / 30; // throttle to ~30fps
        var hidden = false;
        document.addEventListener('visibilitychange', function () { hidden = document.hidden; });

        function draw(ts) {
            requestAnimationFrame(draw);
            if (hidden) return;
            if (ts - last < FRAME) return;
            last = ts;

            ctx.clearRect(0, 0, W, H);
            for (var i = 0; i < parts.length; i++) {
                var p = parts[i];
                p.y -= p.vy; p.x += p.vx; p.tw += 0.05;
                if (p.y < -10) { p.y = H + 10; p.x = Math.random() * W; }
                if (p.x < -10) p.x = W + 10;
                if (p.x > W + 10) p.x = -10;

                var a = 0.25 + Math.abs(Math.sin(p.tw)) * 0.55;
                ctx.globalAlpha = a;
                ctx.fillStyle = p.c;
                // 4-point star
                ctx.beginPath();
                ctx.moveTo(p.x, p.y - p.s * 2);
                ctx.quadraticCurveTo(p.x, p.y, p.x + p.s * 2, p.y);
                ctx.quadraticCurveTo(p.x, p.y, p.x, p.y + p.s * 2);
                ctx.quadraticCurveTo(p.x, p.y, p.x - p.s * 2, p.y);
                ctx.quadraticCurveTo(p.x, p.y, p.x, p.y - p.s * 2);
                ctx.fill();
            }
            ctx.globalAlpha = 1;
        }
        requestAnimationFrame(draw);
    }

    /* ── StatCounter — count up [data-count-to] when visible ── */
    function statCounters() {
        var els = document.querySelectorAll('[data-count-to]');
        if (!els.length) return;

        function run(el) {
            var target = parseFloat(el.getAttribute('data-count-to')) || 0;
            var prefix = el.getAttribute('data-count-prefix') || '';
            var suffix = el.getAttribute('data-count-suffix') || '';
            var decimals = parseInt(el.getAttribute('data-count-decimals') || '0', 10);

            if (!motionOk()) {
                el.textContent = prefix + target.toFixed(decimals) + suffix;
                return;
            }
            var dur = 1400, start = null;
            function step(ts) {
                if (!start) start = ts;
                var t = Math.min((ts - start) / dur, 1);
                var eased = 1 - Math.pow(1 - t, 3);
                el.textContent = prefix + (target * eased).toFixed(decimals) + suffix;
                if (t < 1) requestAnimationFrame(step);
            }
            requestAnimationFrame(step);
        }

        if (!('IntersectionObserver' in window)) {
            els.forEach(run);
            return;
        }
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (e) {
                if (!e.isIntersecting) return;
                run(e.target);
                io.unobserve(e.target);
            });
        }, { threshold: 0.4 });
        els.forEach(function (el) { io.observe(el); });
    }

    /* ── TokenStream — LLM-style token-by-token typing ──────── */
    function streamTokens(el, text, opts) {
        opts = opts || {};
        return new Promise(function (resolve) {
            if (!motionOk()) { el.textContent = text; resolve(); return; }

            el.textContent = '';
            var caret = document.createElement('span');
            caret.className = 'token-caret';
            el.after(caret);

            // split into pseudo-tokens (word fragments) like a real stream
            var tokens = text.match(/\S+\s*/g) || [text];
            var i = 0;
            function next() {
                if (i >= tokens.length) {
                    if (!opts.keepCaret) caret.remove();
                    resolve();
                    return;
                }
                el.textContent += tokens[i++];
                setTimeout(next, 30 + Math.random() * 90); // variable latency = authenticity
            }
            setTimeout(next, opts.delay || 300);
        });
    }

    function tokenStreams() {
        document.querySelectorAll('[data-token-stream]').forEach(function (el) {
            streamTokens(el, el.getAttribute('data-token-stream'), { delay: 500 });
        });
    }

    /* ── WordRoll — rotating hype words ─────────────────────── */
    function wordRolls() {
        document.querySelectorAll('.word-roll').forEach(function (el) {
            var words;
            try { words = JSON.parse(el.getAttribute('data-words') || '[]'); }
            catch (e) { words = []; }
            if (!words.length) return;
            var i = 0;
            el.textContent = words[0];
            if (!motionOk()) return;
            setInterval(function () {
                i = (i + 1) % words.length;
                el.textContent = words[i];
                el.style.animation = 'none';
                void el.offsetWidth;            // restart CSS animation
                el.style.animation = '';
            }, 2200);
        });
    }

    /* ── WaitlistForm — fake success, obviously ─────────────── */
    function waitlist() {
        var form = document.getElementById('waitlistForm');
        if (!form) return;
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            var ok = document.getElementById('waitlistSuccess');
            if (ok) {
                var n = (12000 + Math.floor(Math.random() * 400)).toLocaleString();
                ok.textContent = '✨ You are #' + n + ' on the waitlist. Your TAM has been notified.';
                ok.style.display = 'block';
            }
            form.querySelector('button[type="submit"]').disabled = true;
        });
    }

    /* ── MarkdownLite — hand-rolled, escape-first renderer ──── */
    // HTML is escaped before any markdown is parsed, so model output
    // can never inject markup. Supports: pipe tables, - / * lists,
    // **bold**, *italic*, `code`. Headings and hrs are stripped.
    function escapeHtml(s) {
        return s
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function inlineMd(s) {
        return s
            .replace(/`([^`]+)`/g, '<code>$1</code>')
            .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
            .replace(/\*([^*]+)\*/g, '<em>$1</em>');
    }

    function renderMarkdown(text) {
        var lines = escapeHtml(String(text)).split(/\r?\n/);
        var html = [];
        var i = 0;

        function isDivider(line) {
            // |---|:---:| style separator row (dashes, pipes, colons only)
            return /-/.test(line) && /^[\s|:\-]+$/.test(line);
        }

        function isTableStart(idx) {
            return lines[idx].indexOf('|') !== -1 &&
                idx + 1 < lines.length &&
                lines[idx + 1].indexOf('|') !== -1 &&
                isDivider(lines[idx + 1]);
        }

        function cells(line) {
            var t = line.trim();
            if (t.charAt(0) === '|') t = t.slice(1);
            if (t.charAt(t.length - 1) === '|') t = t.slice(0, -1);
            return t.split('|').map(function (c) { return inlineMd(c.trim()); });
        }

        while (i < lines.length) {
            var line = lines[i];

            if (!line.trim()) { i++; continue; }

            // GitHub-style pipe table: header row + dashed separator row
            if (isTableStart(i)) {
                var head = cells(line);
                i += 2;
                var rows = [];
                while (i < lines.length && lines[i].indexOf('|') !== -1) {
                    rows.push(cells(lines[i++]));
                }
                var t = '<div class="chat-md-table-wrap"><table class="chat-md-table"><thead><tr>';
                head.forEach(function (c) { t += '<th>' + c + '</th>'; });
                t += '</tr></thead><tbody>';
                rows.forEach(function (r) {
                    t += '<tr>';
                    r.forEach(function (c) { t += '<td>' + c + '</td>'; });
                    t += '</tr>';
                });
                t += '</tbody></table></div>';
                html.push(t);
                continue;
            }

            // Unordered list: lines starting with "- " or "* "
            if (/^\s*[-*]\s+\S/.test(line)) {
                var items = [];
                while (i < lines.length && /^\s*[-*]\s+\S/.test(lines[i])) {
                    items.push('<li>' + inlineMd(lines[i].replace(/^\s*[-*]\s+/, '').trim()) + '</li>');
                    i++;
                }
                html.push('<ul>' + items.join('') + '</ul>');
                continue;
            }

            // Horizontal rule — strip entirely
            if (/^\s*([-*_])\s*(\1\s*){2,}$/.test(line)) { i++; continue; }

            // Paragraph — heading markers demoted to plain text,
            // single newlines inside become <br>
            var para = [];
            while (i < lines.length && lines[i].trim() &&
                   !/^\s*[-*]\s+\S/.test(lines[i]) &&
                   !/^\s*([-*_])\s*(\1\s*){2,}$/.test(lines[i]) &&
                   !isTableStart(i)) {
                para.push(inlineMd(lines[i].replace(/^\s*#{1,6}\s+/, '').trim()));
                i++;
            }
            html.push('<p>' + para.join('<br>') + '</p>');
        }
        return html.join('');
    }

    /* ── ChatFAB — AI concierge, now with a real backend ────── */
    function chatFab() {
        var fab = document.getElementById('chatFab');
        var panel = document.getElementById('chatPanel');
        if (!fab || !panel) return;

        var body = panel.querySelector('.chat-body');
        var input = document.getElementById('chatInput');
        var send = document.getElementById('chatSend');
        var greeted = false;
        var busy = false;
        var history = [];               // {role, content} — the greeting doesn't count

        // Offline fallback: artisanal, hand-canned intelligence.
        var REPLIES = [
            "Great question. I've routed it to a swarm of 47 specialized sub-agents. Consensus: bullish.",
            "Based on my training on 10B hands of blackjack, the optimal move is to raise another round.",
            "I can't share that — it's gated behind our Series D data room. But the vibes are immaculate.",
            "Let me 10x that for you. Done. It is now 10x. (This claim is SOC-2 compliant in spirit.)",
            "Our agents have been agentic about this since 2026. Frontier-scale, fully multimodal, zero hallucinations*.",
            "I've added that to the roadmap, the deck, and the TAM slide. All three are now bigger."
        ];
        var replyIdx = 0;

        function aiMessage(text) {
            var msg = document.createElement('div');
            msg.className = 'chat-msg';
            var span = document.createElement('span');
            span.className = 'token-stream';
            msg.appendChild(span);
            body.appendChild(msg);
            body.scrollTop = body.scrollHeight;
            streamTokens(span, text, { delay: 250 }).then(function () {
                // Stream done — swap plain text for rendered markdown.
                // renderMarkdown escapes all HTML first, so this is safe.
                span.innerHTML = renderMarkdown(text);
                span.classList.add('chat-md');
                body.scrollTop = body.scrollHeight;
            });
        }

        function showTyping() {
            var msg = document.createElement('div');
            msg.className = 'chat-msg chat-msg-typing';
            for (var i = 0; i < 3; i++) {
                msg.appendChild(document.createElement('span')).className = 'chat-dot';
            }
            body.appendChild(msg);
            body.scrollTop = body.scrollHeight;
            return msg;
        }

        function setBusy(state) {
            busy = state;
            if (send) send.disabled = state;
            if (input) input.disabled = state;
        }

        fab.addEventListener('click', function () {
            panel.classList.toggle('open');
            if (panel.classList.contains('open') && !greeted) {
                greeted = true;
                aiMessage("Hi! I'm an AI concierge trained on 10B hands of blackjack. How can I 10x you today? ✨");
            }
        });

        function userSend() {
            var text = (input.value || '').trim();
            if (!text || busy) return;
            var msg = document.createElement('div');
            msg.className = 'chat-msg chat-msg-user';
            msg.textContent = text;
            body.appendChild(msg);
            input.value = '';
            body.scrollTop = body.scrollHeight;

            history.push({ role: 'user', content: text });
            setBusy(true);
            var typing = showTyping();

            fetch('/api/chat', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ messages: history })
            }).then(function (res) {
                if (!res.ok) throw new Error('upstream vibes degraded: ' + res.status);
                return res.json();
            }).then(function (data) {
                typing.remove();
                history.push({ role: 'assistant', content: data.reply });
                aiMessage(data.reply);
            }).catch(function () {
                // No API key, no problem — canned replies stay off the record.
                typing.remove();
                aiMessage(REPLIES[replyIdx++ % REPLIES.length]);
            }).finally(function () {
                setBusy(false);
                if (input) input.focus();
            });
        }

        if (send) send.addEventListener('click', userSend);
        if (input) input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); userSend(); }
        });
    }

    /* ── Card entrance — staggered rise on grids ────────────── */
    function cardEntrance() {
        if (!motionOk()) return;
        document.querySelectorAll('.entity-grid, .floor-grid').forEach(function (grid) {
            grid.classList.add('deal-in-ready');
        });
    }

    return {
        streamTokens: streamTokens,
        init: function () {
            sparkles();
            statCounters();
            tokenStreams();
            wordRolls();
            waitlist();
            chatFab();
            cardEntrance();
        }
    };
})();

$(function () { PerformativeUI.init(); });
