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
