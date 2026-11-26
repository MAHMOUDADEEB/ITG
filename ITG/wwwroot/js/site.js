// =========================================================
// Theme toggle (light / dark), saved in the browser
// =========================================================
document.addEventListener('click', function (event) {
    const themeButton = event.target.closest('[data-theme-toggle]');
    if (!themeButton) return;

    const html = document.documentElement;
    const nextTheme = html.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
    html.setAttribute('data-bs-theme', nextTheme);

    try {
        localStorage.setItem('itg-theme', nextTheme);
    } catch (e) {
        // Storage can be blocked (private mode); the theme still works for this page
    }
});

// =========================================================
// Show / hide password
// =========================================================
document.addEventListener('click', function (event) {
    const toggle = event.target.closest('[data-password-toggle]');
    if (!toggle) return;

    const input = document.getElementById(toggle.dataset.passwordToggle);
    if (!input) return;

    const show = input.type === 'password';
    input.type = show ? 'text' : 'password';
    toggle.setAttribute('aria-pressed', String(show));
    toggle.querySelector('i').className = show ? 'bi bi-eye-slash-fill' : 'bi bi-eye-fill';
});

// =========================================================
// Selects that submit their form when changed (career path)
// =========================================================
document.addEventListener('change', function (event) {
    const select = event.target.closest('[data-auto-submit]');
    // Searchable drop-downs (Tom Select) submit from their own onChange
    if (!select || !select.form || select.tomselect) return;

    // A final choice (career path at a fork) is confirmed first
    if (select.dataset.confirmChange) {
        itgConfirm(select.dataset.confirmChange).then(function (ok) {
            if (ok) {
                select.form.submit();
            } else {
                select.selectedIndex = 0; // back to "Choose your path"
            }
        });
        return;
    }

    select.form.submit();
});

// =========================================================
// Date inputs that cannot be in the future
// =========================================================
document.querySelectorAll('[data-max-today]').forEach(function (input) {
    input.max = new Date().toISOString().split('T')[0];
});

// =========================================================
// Certificate upload dialog
// =========================================================
(function () {
    const form = document.querySelector('[data-upload-form]');
    if (!form) return;

    const modal = form.closest('.modal');
    const idInput = form.querySelector('[data-upload-cert-id]');
    const titleText = form.querySelector('[data-upload-cert-title]');
    const fileInput = form.querySelector('[data-upload-file]');
    const fileName = form.querySelector('[data-upload-file-name]');
    const errorText = form.querySelector('[data-upload-error]');

    const showError = function (message) {
        errorText.textContent = message || '';
    };

    // Fill the dialog with the certificate of the clicked button
    modal.addEventListener('show.bs.modal', function (event) {
        const button = event.relatedTarget;
        form.reset();
        idInput.value = button ? button.dataset.certId : '';
        titleText.textContent = button ? button.dataset.certTitle : '';
        fileName.textContent = '';
        showError('');
    });

    fileInput.addEventListener('change', function () {
        fileName.textContent = fileInput.files.length ? fileInput.files[0].name : '';
        showError('');
    });

    // Quick checks in the browser (the server checks again)
    form.addEventListener('submit', function (event) {
        const file = fileInput.files[0];
        const allowed = form.dataset.allowed.split(',');
        const extension = file ? '.' + file.name.split('.').pop().toLowerCase() : '';

        let message = '';
        if (!file) {
            message = form.dataset.msgRequired;
        } else if (!allowed.includes(extension)) {
            message = form.dataset.msgType;
        } else if (file.size > Number(form.dataset.maxBytes)) {
            message = form.dataset.msgSize;
        }

        if (message) {
            event.preventDefault();
            showError(message);
        }
    });
})();

// =========================================================
// Admin: confirm before deleting / promoting
// =========================================================
document.addEventListener('submit', function (event) {
    const form = event.target.closest('[data-confirm]');
    if (!form) return;

    // Second pass: the user already said yes
    if (form.dataset.confirmed === '1') {
        delete form.dataset.confirmed;
        return;
    }

    event.preventDefault();
    event.stopImmediatePropagation();
    const submitter = event.submitter;
    const isDanger = !!form.querySelector('.btn-itg--danger, .icon-action--danger') || /delete|remove|حذف/i.test(form.dataset.confirm);

    itgConfirm(form.dataset.confirm, { danger: isDanger }).then(function (ok) {
        if (!ok) return;
        form.dataset.confirmed = '1';
        if (form.requestSubmit) {
            form.requestSubmit(submitter && submitter.form === form ? submitter : undefined);
        } else {
            form.submit();
        }
    });
}, true);

// =========================================================
// Admin: one dialog for "Add" and "Edit".
// The Edit button carries the item as JSON in data-fill.
// =========================================================
document.querySelectorAll('[data-entity-modal]').forEach(function (modal) {
    const form = modal.querySelector('form');
    const title = modal.querySelector('.modal-title');
    const createTitle = title.textContent;
    const createOnly = modal.querySelectorAll('[data-create-only]');

    modal.addEventListener('show.bs.modal', function (event) {
        const button = event.relatedTarget;
        const data = button && button.dataset.fill ? JSON.parse(button.dataset.fill) : null;

        form.reset();
        // reset() does not clear hidden inputs, so the Id is reset by hand
        const idInput = form.querySelector('input[type="hidden"][name="Id"]');
        if (idInput) idInput.value = idInput.defaultValue;

        title.textContent = button && button.dataset.modalTitle ? button.dataset.modalTitle : createTitle;
        createOnly.forEach(function (el) { el.hidden = !!data; });

        // Keep searchable drop-downs in step with the form values
        const syncSearchable = function () {
            form.querySelectorAll('select').forEach(function (select) {
                if (!select.tomselect) return;
                const value = select.multiple
                    ? Array.from(select.selectedOptions).map(function (o) { return o.value; })
                    : select.value;
                select.tomselect.setValue(value, true);
            });
        };

        if (!data) {
            syncSearchable();
            return;
        }

        // Fill each field whose name matches a JSON key (case-insensitive)
        Object.keys(data).forEach(function (key) {
            const value = data[key];
            Array.from(form.elements)
                .filter(function (el) { return el.name && el.name.toLowerCase() === key.toLowerCase(); })
                .forEach(function (el) {
                    if (el.type === 'checkbox') {
                        el.checked = value === true;
                    } else if (el.multiple && Array.isArray(value)) {
                        // Multi-select: tick every option whose value is in the list
                        const wanted = value.map(String);
                        Array.from(el.options).forEach(function (o) { o.selected = wanted.indexOf(o.value) !== -1; });
                    } else if (el.type !== 'hidden' || el.name.toLowerCase() === 'id') {
                        el.value = value === null || value === undefined ? '' : value;
                    }
                });
        });
        syncSearchable();
    });
});

// =========================================================
// Admin: reject dialog (fills the upload id and certificate title)
// =========================================================
(function () {
    const form = document.querySelector('[data-reject-form]');
    if (!form) return;

    form.closest('.modal').addEventListener('show.bs.modal', function (event) {
        const button = event.relatedTarget;
        form.querySelector('[data-reject-id]').value = button ? button.dataset.rejectId : '';
        form.querySelector('[data-reject-title]').textContent = button ? button.dataset.rejectTitle : '';
        form.querySelector('textarea').value = '';
    });
})();

// =========================================================
// "Please wait" overlay while the AI works
// =========================================================
const loadingOverlay = {
    show: function (text) {
        const overlay = document.querySelector('[data-loading-overlay]');
        if (!overlay) return;
        overlay.querySelector('[data-loading-text]').textContent = text || '';
        overlay.hidden = false;
    }
};

// Forms with data-loading (or a button with data-loading-button) show the overlay
document.addEventListener('submit', function (event) {
    if (event.defaultPrevented) return;
    const form = event.target;
    const button = form.querySelector('[data-loading-button]');
    const text = form.dataset.loading || (button && button.dataset.loadingButton);
    if (text) loadingOverlay.show(text);
});

// Links with data-loading-link show the overlay while the next page loads
document.addEventListener('click', function (event) {
    const link = event.target.closest('[data-loading-link]');
    if (link) loadingOverlay.show(link.dataset.loadingLink);
});

// The browser "back" button can restore a page with the overlay still visible
window.addEventListener('pageshow', function () {
    const overlay = document.querySelector('[data-loading-overlay]');
    if (overlay) overlay.hidden = true;
});

// =========================================================
// Exam / assessment page: answered counter, timer, submit checks
// =========================================================
(function () {
    const form = document.querySelector('[data-exam-form]');
    if (!form) return;

    const counter = form.querySelector('[data-answered-count]');
    const total = form.querySelectorAll('.question-card').length;

    const answeredCount = function () {
        return form.querySelectorAll('[data-answer]:checked').length;
    };

    form.addEventListener('change', function (event) {
        if (!event.target.matches('[data-answer]')) return;

        // Highlight the chosen option
        event.target.closest('.option-list').querySelectorAll('.option-tile').forEach(function (tile) {
            tile.classList.toggle('is-selected', tile.contains(event.target));
        });
        if (counter) counter.textContent = answeredCount();
    });

    // Warn about unanswered questions before submitting
    form.addEventListener('submit', function (event) {
        const left = total - answeredCount();
        if (left <= 0) return;

        // Second pass: the user agreed to submit with empty answers
        if (form.dataset.confirmed === '1') {
            delete form.dataset.confirmed;
            return;
        }

        event.preventDefault();
        event.stopImmediatePropagation();
        const message = (form.dataset.msgUnanswered || '').replace('{0}', left);

        if (form.hasAttribute('data-require-all')) {
            itgConfirm(message, { alertOnly: true }).then(function () {
                const first = Array.from(form.querySelectorAll('.option-list'))
                    .find(function (list) { return !list.querySelector('[data-answer]:checked'); });
                if (first) first.closest('.question-card').scrollIntoView({ behavior: 'smooth', block: 'center' });
            });
        } else {
            itgConfirm(message).then(function (ok) {
                if (!ok) return;
                form.dataset.confirmed = '1';
                if (form.requestSubmit) form.requestSubmit(); else form.submit();
            });
        }
    });

    // Countdown; when the time is up the answers are sent automatically
    const timer = form.querySelector('[data-exam-timer]');
    if (!timer) return;

    const timerText = timer.querySelector('[data-timer-text]');

    // The server sends the seconds left (not a date), so time zones cannot break the timer.
    // If the value is missing or not valid, the timer is not started (the server still checks the time).
    const secondsLeft = parseInt(timer.dataset.secondsLeft, 10);
    if (!(secondsLeft > 0)) return;
    const expires = Date.now() + secondsLeft * 1000;

    const tick = function () {
        const seconds = Math.max(0, Math.floor((expires - Date.now()) / 1000));
        const minutes = Math.floor(seconds / 60);
        timerText.textContent = String(minutes).padStart(2, '0') + ':' + String(seconds % 60).padStart(2, '0');
        timer.classList.toggle('is-warning', seconds <= 60);

        if (seconds === 0) {
            clearInterval(interval);
            form.submit(); // submit() skips the confirm dialog
        }
    };

    const interval = setInterval(tick, 1000);
    tick();
})();

// =========================================================
// AI chat
// =========================================================
(function () {
    const chat = document.querySelector('[data-chat]');
    if (!chat) return;

    const form = chat.querySelector('[data-chat-form]');
    const input = chat.querySelector('[data-chat-text]');
    const log = chat.querySelector('[data-chat-log]');
    const hero = chat.querySelector('[data-chat-hero]');
    const token = chat.querySelector('input[name="__RequestVerificationToken"]').value;
    const history = [];
    let busy = false;

    // Safe formatting: text only, "- " lines become bullets, **text** becomes bold
    const appendFormatted = function (container, text) {
        let list = null;
        text.split('\n').forEach(function (line) {
            const trimmed = line.trim();
            if (!trimmed) { list = null; return; }

            let target;
            if (/^[-*•]\s+/.test(trimmed)) {
                if (!list) { list = document.createElement('ul'); container.appendChild(list); }
                target = document.createElement('li');
                list.appendChild(target);
            } else {
                list = null;
                target = document.createElement('p');
                container.appendChild(target);
            }

            trimmed.replace(/^[-*•]\s+/, '').split('**').forEach(function (part, index) {
                if (!part) return;
                if (index % 2 === 1) {
                    const strong = document.createElement('strong');
                    strong.textContent = part;
                    target.appendChild(strong);
                } else {
                    target.appendChild(document.createTextNode(part));
                }
            });
        });
    };

    const addBubble = function (role, text) {
        const bubble = document.createElement('div');
        bubble.className = 'chat-bubble chat-bubble--' + role;
        if (role === 'assistant') {
            appendFormatted(bubble, text);
        } else {
            bubble.textContent = text;
        }
        log.appendChild(bubble);
        bubble.scrollIntoView({ behavior: 'smooth', block: 'end' });
        return bubble;
    };

    const send = async function (text) {
        text = text.trim();
        if (!text || busy) return;

        busy = true;
        hero.hidden = true;
        chat.classList.add('has-messages');
        addBubble('user', text);
        history.push({ role: 'user', text: text });
        input.value = '';
        input.style.height = '';

        const typing = addBubble('typing', '');
        typing.innerHTML = '<span></span><span></span><span></span>';

        try {
            const response = await fetch(chat.dataset.sendUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
                body: JSON.stringify({ messages: history })
            });
            const data = await response.json();
            typing.remove();

            if (data.reply) {
                addBubble('assistant', data.reply);
                history.push({ role: 'assistant', text: data.reply });
            } else {
                addBubble('error', data.error || chat.dataset.error);
                history.pop(); // let the user try the same question again
            }
        } catch (e) {
            typing.remove();
            addBubble('error', chat.dataset.error);
            history.pop();
        } finally {
            busy = false;
            input.focus();
        }
    };

    form.addEventListener('submit', function (event) {
        event.preventDefault();
        send(input.value);
    });

    // Enter sends, Shift + Enter makes a new line
    input.addEventListener('keydown', function (event) {
        if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault();
            send(input.value);
        }
    });

    // The text box grows with the text (up to a limit)
    input.addEventListener('input', function () {
        input.style.height = '';
        input.style.height = Math.min(input.scrollHeight, 160) + 'px';
    });

    chat.querySelectorAll('[data-chat-suggestion]').forEach(function (button) {
        button.addEventListener('click', function () { send(button.textContent); });
    });
})();

// =========================================================
// Admin: search box that applies itself while typing (no Enter needed)
// =========================================================
(function () {
    const input = document.querySelector('[data-live-search]');
    if (!input || !input.form) return;

    const storageKey = 'itg-live-search';
    let timer = null;

    input.addEventListener('input', function () {
        clearTimeout(timer);
        // Wait until the user stops typing for a moment
        timer = setTimeout(function () {
            try { sessionStorage.setItem(storageKey, '1'); } catch (e) { /* ignore */ }
            input.form.submit();
        }, 450);
    });

    // After the page reloads with the results, put the cursor back in the box
    try {
        if (sessionStorage.getItem(storageKey) === '1') {
            sessionStorage.removeItem(storageKey);
            input.focus();
            const end = input.value.length;
            input.setSelectionRange(end, end);
        }
    } catch (e) { /* ignore */ }
})();

// =========================================================
// Design refresh: dialogs, toasts, sidebar, search, numbers, charts
// =========================================================

// Confirm dialog in the site style (replaces the browser's confirm box).
// Returns a promise: true = yes, false = cancel. alertOnly shows one "OK" button.
function itgConfirm(message, options) {
    options = options || {};
    const body = document.body;
    const okText = body.dataset.textOk || 'OK';
    const cancelText = body.dataset.textCancel || 'Cancel';

    // Pages without Bootstrap's JS fall back to the browser box
    if (!window.bootstrap || !window.bootstrap.Modal) {
        if (options.alertOnly) { window.alert(message); return Promise.resolve(true); }
        return Promise.resolve(window.confirm(message));
    }

    return new Promise(function (resolve) {
        const wrapper = document.createElement('div');
        wrapper.className = 'modal fade confirm-dialog' + (options.danger ? ' is-danger' : '');
        wrapper.tabIndex = -1;
        wrapper.setAttribute('aria-modal', 'true');
        wrapper.setAttribute('role', 'dialog');
        wrapper.innerHTML =
            '<div class="modal-dialog modal-dialog-centered"><div class="modal-content">' +
            '<div class="modal-body"><span class="confirm-icon"><i class="bi ' +
            (options.danger ? 'bi-trash3' : options.alertOnly ? 'bi-info-lg' : 'bi-question-lg') +
            '" aria-hidden="true"></i></span><p class="confirm-message"></p></div>' +
            '<div class="modal-footer">' +
            (options.alertOnly ? '' : '<button type="button" class="btn btn-itg-outline" data-answer="no"></button>') +
            '<button type="button" class="btn ' + (options.danger ? 'btn-itg btn-itg--danger' : 'btn-itg') + '" data-answer="yes"></button>' +
            '</div></div></div>';

        wrapper.querySelector('.confirm-message').textContent = message;
        wrapper.querySelector('[data-answer="yes"]').textContent = options.alertOnly ? (body.dataset.textAlert || 'OK') : okText;
        const noButton = wrapper.querySelector('[data-answer="no"]');
        if (noButton) noButton.textContent = cancelText;

        document.body.appendChild(wrapper);
        const modal = new bootstrap.Modal(wrapper);
        let answer = false;

        wrapper.addEventListener('click', function (event) {
            const button = event.target.closest('[data-answer]');
            if (!button) return;
            answer = button.dataset.answer === 'yes';
            modal.hide();
        });
        wrapper.addEventListener('shown.bs.modal', function () {
            wrapper.querySelector('[data-answer="yes"]').focus();
        });
        wrapper.addEventListener('hidden.bs.modal', function () {
            modal.dispose();
            wrapper.remove();
            resolve(options.alertOnly ? true : answer);
        });

        modal.show();
    });
}

// ---------- Toast messages: close by themselves after 6 seconds ----------
(function () {
    const close = function (toast) {
        if (!toast || toast.classList.contains('is-leaving')) return;
        toast.classList.add('is-leaving');
        setTimeout(function () { toast.remove(); }, 300);
    };

    document.querySelectorAll('[data-toast]').forEach(function (toast) {
        let timer = setTimeout(function () { close(toast); }, 6000);
        // Hovering keeps the message open
        toast.addEventListener('mouseenter', function () { clearTimeout(timer); toast.classList.add('is-paused'); });
        toast.addEventListener('mouseleave', function () { timer = setTimeout(function () { close(toast); }, 2500); });
    });

    document.addEventListener('click', function (event) {
        const button = event.target.closest('[data-toast-close]');
        if (button) close(button.closest('[data-toast]'));
    });
})();

// ---------- Sidebar: shrink to icons (desktop), remembered ----------
document.addEventListener('click', function (event) {
    if (!event.target.closest('[data-sidebar-toggle]')) return;

    const html = document.documentElement;
    const mini = html.getAttribute('data-sidebar') !== 'mini';
    const apply = function () {
        if (mini) html.setAttribute('data-sidebar', 'mini'); else html.removeAttribute('data-sidebar');
    };

    // Animate the change where the browser supports it
    if (document.startViewTransition) document.startViewTransition(apply); else apply();
    try { localStorage.setItem('itg-sidebar', mini ? 'mini' : 'full'); } catch (e) { /* ignore */ }
});

// ---------- Top bar shadow once the page is scrolled ----------
(function () {
    const update = function () {
        document.body.classList.toggle('is-scrolled', window.scrollY > 4);
    };
    window.addEventListener('scroll', update, { passive: true });
    update();
})();

// ---------- Search: "/" to focus, button on phones ----------
(function () {
    const input = document.querySelector('[data-search-input]');
    if (!input) return;

    const open = function () {
        document.body.classList.add('search-open');
        setTimeout(function () { input.focus(); }, 30);
    };
    const closeSearch = function () { document.body.classList.remove('search-open'); };

    document.addEventListener('click', function (event) {
        if (event.target.closest('[data-search-toggle]')) {
            document.body.classList.contains('search-open') ? closeSearch() : open();
        } else if (!event.target.closest('.topbar-search')) {
            closeSearch();
        }
    });

    document.addEventListener('keydown', function (event) {
        const typing = event.target.closest('input, textarea, select, [contenteditable="true"]');
        if (event.key === '/' && !typing && !event.ctrlKey && !event.metaKey) {
            event.preventDefault();
            open();
        } else if (event.key === 'Escape' && event.target === input) {
            input.blur();
            closeSearch();
        }
    });
})();

// ---------- Numbers count up when the page opens ----------
(function () {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

    const selectors = '[data-countup], .stat-value, .mini-stat-value, .record-stat-value, .eval-score, ' +
        '.exam-card-score strong, .result-ring strong, .progress-percent';

    document.querySelectorAll(selectors).forEach(function (el) {
        // Only the first piece of text is animated ("78" in "78/100")
        const node = Array.from(el.childNodes).find(function (n) {
            return n.nodeType === Node.TEXT_NODE && /\d/.test(n.textContent);
        });
        if (!node) return;

        const match = node.textContent.match(/^(\s*)(\d+)(.*)$/s);
        if (!match) return;

        const target = parseInt(match[2], 10);
        if (!(target > 0)) return;

        const duration = 900;
        const start = performance.now();
        const frame = function (now) {
            const t = Math.min(1, (now - start) / duration);
            const eased = 1 - Math.pow(1 - t, 3);
            node.textContent = match[1] + Math.round(target * eased) + match[3];
            if (t < 1) requestAnimationFrame(frame);
        };
        node.textContent = match[1] + '0' + match[3];
        requestAnimationFrame(frame);
    });
})();

// ---------- Searchable drop-downs (Tom Select) ----------
(function () {
    if (!window.TomSelect) return;
    const noResults = document.body.dataset.textNoResults || 'No results';

    document.querySelectorAll('select[data-searchable]').forEach(function (select) {
        const autoSubmit = select.hasAttribute('data-auto-submit');
        // Multi-select: show the hint (data-placeholder) inside the empty box
        if (select.multiple && select.dataset.placeholder) select.setAttribute('placeholder', select.dataset.placeholder);

        const control = new TomSelect(select, {
            create: false,
            maxOptions: null,
            allowEmptyOption: true,
            // The search box sits at the top of the open list; chips get an x to remove them
            plugins: select.multiple ? ['dropdown_input', 'remove_button'] : ['dropdown_input'],
            closeAfterSelect: !select.multiple,
            render: {
                no_results: function () {
                    const div = document.createElement('div');
                    div.className = 'no-results';
                    div.textContent = noResults;
                    return div;
                }
            },
            onChange: function () {
                if (autoSubmit && select.form) select.form.submit();
            }
        });

        const searchBox = control.dropdown && control.dropdown.querySelector('.dropdown-input');
        if (searchBox) {
            // Multi-select keeps its hint in the box, so its list gets a plain "Search" box
            searchBox.placeholder = (!select.multiple && select.dataset.placeholder) || document.body.dataset.textSearch || '';
        }

        // "required" is checked on the hidden original select, so show the message on the visible box
        select.addEventListener('invalid', function () { control.focus(); });
    });
})();

// ---------- Chart.js is loaded once, only on pages that have a chart ----------
const itgWithChart = (function () {
    const waiting = [];
    let loading = false;
    return function (callback) {
        if (window.Chart) { callback(); return; }
        waiting.push(callback);
        if (loading) return;
        loading = true;
        const script = document.createElement('script');
        script.src = document.body.dataset.chartSrc;
        script.onload = function () { waiting.splice(0).forEach(function (fn) { fn(); }); };
        document.head.appendChild(script);
    };
})();

// ---------- Skills radar chart (Chart.js, loaded only when needed) ----------
(function () {
    const canvases = document.querySelectorAll('[data-skill-radar]');
    if (!canvases.length) return;

    const draw = function () {
        canvases.forEach(function (canvas) {
            const dark = document.documentElement.getAttribute('data-bs-theme') === 'dark';
            const grid = dark ? 'rgba(148, 178, 214, 0.18)' : 'rgba(30, 115, 190, 0.14)';
            const text = dark ? '#b0b8c4' : '#555555';
            const rtl = document.documentElement.dir === 'rtl';

            // Latest result (blue) first so it is drawn on top of the previous one (gray, dashed)
            const datasets = [{
                label: canvas.dataset.latestLabel || '',
                data: JSON.parse(canvas.dataset.values),
                backgroundColor: 'rgba(30, 115, 190, 0.18)',
                borderColor: '#1e73be',
                borderWidth: 2,
                pointBackgroundColor: '#c9a227',
                pointBorderColor: dark ? '#172131' : '#ffffff',
                pointBorderWidth: 2,
                pointRadius: 4,
                pointHoverRadius: 6
            }];
            if (canvas.dataset.previous) {
                const gray = dark ? '#8c97a6' : '#9aa3ae';
                datasets.push({
                    label: canvas.dataset.previousLabel || '',
                    data: JSON.parse(canvas.dataset.previous),
                    backgroundColor: dark ? 'rgba(140, 151, 166, 0.08)' : 'rgba(154, 163, 174, 0.10)',
                    borderColor: gray,
                    borderWidth: 1.5,
                    borderDash: [5, 4],
                    pointBackgroundColor: gray,
                    pointBorderColor: dark ? '#172131' : '#ffffff',
                    pointBorderWidth: 1,
                    pointRadius: 3,
                    pointHoverRadius: 5
                });
            }

            if (canvas.chart) canvas.chart.destroy();
            canvas.chart = new Chart(canvas, {
                type: 'radar',
                data: {
                    // Long names go on two lines so they fit around the chart
                    labels: JSON.parse(canvas.dataset.labels).map(function (label) {
                        return label.length > 10 && label.indexOf(' ') > 0 ? label.split(' ') : label;
                    }),
                    datasets: datasets
                },
                options: {
                    maintainAspectRatio: false,
                    layout: { padding: 2 },
                    animation: { duration: 900, easing: 'easeOutCubic' },
                    plugins: {
                        legend: { display: false },
                        tooltip: { rtl: rtl, callbacks: { label: function (c) {
                            return ' ' + (c.dataset.label ? c.dataset.label + ': ' : '') + c.formattedValue + '%';
                        } } }
                    },
                    scales: {
                        r: {
                            min: 0,
                            max: 100,
                            ticks: { display: false, stepSize: 25 },
                            grid: { color: grid },
                            angleLines: { color: grid },
                            pointLabels: {
                                color: text,
                                padding: 6,
                                font: { size: 10.5, weight: '600', family: rtl ? 'Cairo, sans-serif' : 'Open Sans, sans-serif' }
                            }
                        }
                    }
                }
            });
        });
    };

    itgWithChart(draw);

    // Redraw with the right colors when the theme changes
    new MutationObserver(function () { if (window.Chart) draw(); })
        .observe(document.documentElement, { attributes: true, attributeFilter: ['data-bs-theme'] });
})();

// ---------- My Plan: tick tasks without reloading the page ----------
(function () {
    const form = document.getElementById('planTaskForm');
    if (!form) return;
    const token = form.querySelector('input[name="__RequestVerificationToken"]').value;
    const count = document.querySelector('[data-plan-count]');
    const percent = document.querySelector('[data-plan-percent]');
    const fill = document.querySelector('[data-plan-fill]');

    document.querySelectorAll('[data-plan-task]').forEach(function (box) {
        box.addEventListener('change', function () {
            box.disabled = true;
            const body = new FormData();
            body.append('planId', form.dataset.planId);
            body.append('week', box.dataset.week);
            body.append('task', box.dataset.task);
            body.append('done', box.checked ? 'true' : 'false');
            body.append('__RequestVerificationToken', token);

            fetch(form.dataset.url, { method: 'POST', body: body, credentials: 'same-origin' })
                .then(function (response) {
                    if (!response.ok) throw new Error(response.status);
                    return response.json();
                })
                .then(function (data) {
                    if (count) count.textContent = form.dataset.countText.replace('{0}', data.done).replace('{1}', data.total);
                    if (percent) percent.textContent = data.percent + '%';
                    if (fill) {
                        fill.style.width = data.percent + '%';
                        fill.parentElement.setAttribute('aria-valuenow', data.percent);
                    }
                    const week = box.closest('.plan-week');
                    if (week) {
                        const boxes = week.querySelectorAll('[data-plan-task]');
                        const allDone = Array.prototype.every.call(boxes, function (b) { return b.checked; });
                        week.classList.toggle('plan-week--done', allDone);
                    }
                })
                .catch(function () {
                    // Could not save: put the tick back as it was
                    box.checked = !box.checked;
                })
                .finally(function () { box.disabled = false; });
        });
    });
})();

// ---------- Simple charts (admin analytics): bar, hbar (horizontal) and doughnut ----------
(function () {
    const canvases = document.querySelectorAll('canvas[data-chart]');
    if (!canvases.length) return;

    const draw = function () {
        canvases.forEach(function (canvas) {
            const dark = document.documentElement.getAttribute('data-bs-theme') === 'dark';
            const grid = dark ? 'rgba(148, 178, 214, 0.16)' : 'rgba(30, 115, 190, 0.10)';
            const text = dark ? '#b0b8c4' : '#555555';
            const rtl = document.documentElement.dir === 'rtl';
            const font = { family: rtl ? 'Cairo, sans-serif' : 'Open Sans, sans-serif', size: 11.5, weight: '600' };

            const type = canvas.dataset.chart;
            const labels = JSON.parse(canvas.dataset.labels || '[]');
            const values = JSON.parse(canvas.dataset.values || '[]');
            const extra = canvas.dataset.extra ? JSON.parse(canvas.dataset.extra) : null;
            const suffix = canvas.dataset.suffix || '';
            let config;

            if (type === 'doughnut') {
                config = {
                    type: 'doughnut',
                    data: {
                        labels: labels,
                        datasets: [{
                            data: values,
                            backgroundColor: JSON.parse(canvas.dataset.colors || '[]'),
                            borderColor: dark ? '#1b2432' : '#ffffff',
                            borderWidth: 3,
                            hoverOffset: 6
                        }]
                    },
                    options: {
                        maintainAspectRatio: false,
                        cutout: '68%',
                        animation: { duration: 900, easing: 'easeOutCubic' },
                        plugins: { legend: { display: false }, tooltip: { rtl: rtl } }
                    }
                };
            } else {
                const horizontal = type === 'hbar';
                const valueTicks = { color: text, font: font, precision: 0 };
                if (suffix) valueTicks.callback = function (v) { return v + suffix; };
                const valueAxis = { beginAtZero: true, grid: { color: grid }, ticks: valueTicks, border: { display: false } };
                if (canvas.dataset.max) valueAxis.max = +canvas.dataset.max;
                else valueAxis.suggestedMax = Math.max.apply(null, values.concat([0])) + 1; // small counts keep some room on top
                const labelAxis = { grid: { display: false }, ticks: { color: text, font: font }, border: { display: false } };

                if (horizontal && rtl) {
                    valueAxis.reverse = true;
                    labelAxis.position = 'right';
                }

                config = {
                    type: 'bar',
                    data: {
                        labels: labels,
                        datasets: [{
                            data: values,
                            // Blue bars that end in the ITG yellow (like the progress bars)
                            backgroundColor: function (c) {
                                const area = c.chart.chartArea;
                                if (!area) return '#1e73be';
                                const g = horizontal
                                    ? c.chart.ctx.createLinearGradient(rtl ? area.right : area.left, 0, rtl ? area.left : area.right, 0)
                                    : c.chart.ctx.createLinearGradient(0, area.bottom, 0, area.top);
                                g.addColorStop(0, '#1e73be');
                                g.addColorStop(0.75, '#4aa1ea');
                                g.addColorStop(1, '#e9c440');
                                return g;
                            },
                            borderRadius: 6,
                            borderSkipped: false,
                            maxBarThickness: horizontal ? 20 : 34
                        }]
                    },
                    options: {
                        indexAxis: horizontal ? 'y' : 'x',
                        maintainAspectRatio: false,
                        animation: { duration: 900, easing: 'easeOutCubic' },
                        plugins: {
                            legend: { display: false },
                            tooltip: {
                                rtl: rtl,
                                callbacks: {
                                    label: function (c) { return ' ' + c.formattedValue + suffix; },
                                    afterLabel: function (c) { return extra ? ' ' + extra[c.dataIndex] : ''; }
                                }
                            }
                        },
                        scales: horizontal ? { x: valueAxis, y: labelAxis } : { x: labelAxis, y: valueAxis }
                    }
                };
            }

            if (canvas.chart) canvas.chart.destroy();
            canvas.chart = new Chart(canvas, config);
        });
    };

    itgWithChart(draw);

    // Redraw with the right colors when the theme changes
    new MutationObserver(function () { if (window.Chart) draw(); })
        .observe(document.documentElement, { attributes: true, attributeFilter: ['data-bs-theme'] });
})();
