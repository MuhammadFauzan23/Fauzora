// Notifikasi TempData ditampilkan sebagai SweetAlert2 toast yang hilang otomatis.
document.querySelectorAll('[data-swal-toast]').forEach(function (toast) {
    if (!window.Swal) {
        var type = toast.dataset.toastType === 'success' ? 'success' : 'warning';
        toast.classList.add('alert', type === 'success' ? 'alert-success' : 'alert-warning', 'alert-dismissible', 'fade', 'show', 'mb-3');
        toast.setAttribute('role', 'alert');
        toast.textContent = toast.dataset.toastMessage || '';
        var close = document.createElement('button');
        close.type = 'button';
        close.className = 'btn-close';
        close.setAttribute('aria-label', 'Tutup');
        close.addEventListener('click', function () { toast.remove(); });
        toast.appendChild(close);
        return;
    }
    Swal.fire({
        toast: true,
        position: 'top-end',
        icon: toast.dataset.toastType || 'info',
        title: toast.dataset.toastMessage || '',
        showConfirmButton: false,
        timer: 3200,
        timerProgressBar: true,
        showCloseButton: true
    });
});

// Konfirmasi aksi penting sebelum form dikirim ke server.
document.querySelectorAll('form[data-swal-confirm]').forEach(function (form) {
    form.addEventListener('submit', async function (event) {
        if (form.dataset.swalConfirmed === 'true') return;
        event.preventDefault();

        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        var submitter = event.submitter;
        var sourceElements = Array.from(form.querySelectorAll('[data-rich-text-editor]'));
        await Promise.all(sourceElements.map(function (element) {
            return element.__fauzoraEditorReady || Promise.resolve();
        }));

        if (window.Swal) {
            var result = await Swal.fire({
                title: form.dataset.swalTitle || 'Konfirmasi tindakan',
                text: form.dataset.swalText || 'Apakah Anda yakin ingin melanjutkan?',
                icon: form.dataset.swalIcon || 'question',
                showCancelButton: true,
                confirmButtonText: form.dataset.swalConfirmText || 'Ya, lanjutkan',
                cancelButtonText: 'Tidak',
                reverseButtons: true,
                focusCancel: true,
                buttonsStyling: true,
                customClass: {
                    confirmButton: 'btn btn-primary swal-confirm-button',
                    cancelButton: 'btn btn-outline-secondary swal-cancel-button'
                }
            });
            if (!result.isConfirmed) return;
        }

        sourceElements.forEach(function (element) {
            if (element.__fauzoraEditor) element.value = element.__fauzoraEditor.getData();
        });
        form.dataset.swalConfirmed = 'true';
        if (submitter) form.requestSubmit(submitter);
        else form.requestSubmit();
    });
});

// Form ulasan: tombol aktif setelah customer memilih rating dan mengonfirmasi produk diterima.
document.querySelectorAll('[data-review-form]').forEach(function (form) {
    var ratingInputs = form.querySelectorAll('input[name="Form.Rating"]');
    var confirmation = form.querySelector('[data-review-confirm]');
    var submit = form.querySelector('[data-review-submit]');
    var label = form.querySelector('[data-rating-label]');
    var ratingNames = { 1: 'Sangat kecewa', 2: 'Kurang puas', 3: 'Cukup puas', 4: 'Puas', 5: 'Sangat puas' };

    function syncReviewForm() {
        var selected = form.querySelector('input[name="Form.Rating"]:checked');
        var rating = selected ? selected.value : '0';
        if (label) label.textContent = rating === '0' ? 'Pilih bintang' : ratingNames[rating];
        if (submit) submit.disabled = !selected || !confirmation.checked;
    }

    ratingInputs.forEach(function (input) { input.addEventListener('change', syncReviewForm); });
    if (confirmation) confirmation.addEventListener('change', syncReviewForm);
    syncReviewForm();
});

// Filter ulasan berjalan di browser agar klik rating tidak reload dan tidak mengubah posisi scroll.
document.querySelectorAll('[data-review-filters]').forEach(function (filters) {
    var items = document.querySelectorAll('[data-review-item]');
    var empty = document.querySelector('[data-review-filter-empty]');
    var links = filters.querySelectorAll('[data-review-filter]');

    function applyFilter(value) {
        var visible = 0;
        items.forEach(function (item) {
            var show = value === 'all' || item.dataset.rating === value;
            item.hidden = !show;
            if (show) visible++;
        });
        links.forEach(function (link) { link.classList.toggle('active', link.dataset.reviewFilter === value); });
        if (empty) empty.hidden = visible > 0;
        filters.dataset.selectedRating = value;
    }

    links.forEach(function (link) {
        link.addEventListener('click', function (event) {
            event.preventDefault();
            var value = link.dataset.reviewFilter;
            applyFilter(value);
            var url = new URL(link.href, window.location.origin);
            if (value === 'all') url.searchParams.delete('rating');
            else url.searchParams.set('rating', value);
            window.history.replaceState({}, '', url);
        });
    });

    applyFilter(filters.dataset.selectedRating || 'all');
});

// Deskripsi produk dipadatkan agar halaman detail tetap ringkas dan bisa dibuka saat diperlukan.
document.querySelectorAll('[data-description-toggle]').forEach(function (toggle) {
    var content = toggle.previousElementSibling;
    if (!content) return;

    function syncDescription() {
        var isLong = content.scrollHeight > 150;
        var expanded = toggle.getAttribute('aria-expanded') === 'true';
        toggle.hidden = !isLong;
        if (!isLong) {
            content.classList.remove('is-collapsed');
            toggle.setAttribute('aria-expanded', 'true');
            return;
        }
        content.classList.toggle('is-collapsed', !expanded);
        toggle.textContent = expanded ? 'Sembunyikan' : 'Lihat selengkapnya';
    }

    toggle.addEventListener('click', function () {
        var expanded = toggle.getAttribute('aria-expanded') === 'true';
        content.classList.toggle('is-collapsed', expanded);
        toggle.setAttribute('aria-expanded', String(!expanded));
        toggle.textContent = expanded ? 'Lihat selengkapnya' : 'Sembunyikan';
    });
    window.addEventListener('resize', syncDescription);
    syncDescription();
});

// Upload produk menampilkan gambar lama yang dipertahankan dan preview file lokal.
(function () {
    var editor = document.querySelector('[data-product-upload]');
    if (!editor) return;
    var form = editor.closest('form');
    var input = editor.querySelector('[data-product-image-files]');
    var count = editor.querySelector('[data-product-image-count]');
    var newGrid = editor.querySelector('[data-new-image-grid]');
    var error = editor.querySelector('[data-product-image-error]');
    var maxImages = Number(editor.dataset.maxImages || 15);
    var maxFileBytes = 5 * 1024 * 1024;
    var maxTotalBytes = 50 * 1024 * 1024;
    var objectUrls = [];

    function retainedCount() {
        return editor.querySelectorAll('[data-retain-image]:checked').length;
    }

    function setFiles(files) {
        var transfer = new DataTransfer();
        files.forEach(function (file) { transfer.items.add(file); });
        input.files = transfer.files;
        renderNewFiles();
    }

    function renderNewFiles() {
        objectUrls.forEach(function (url) { URL.revokeObjectURL(url); });
        objectUrls = [];
        newGrid.replaceChildren();
        var files = Array.from(input.files || []);
        files.forEach(function (file, index) {
            var card = document.createElement('div');
            card.className = 'product-image-choice selected';
            var image = document.createElement('img');
            var url = URL.createObjectURL(file);
            objectUrls.push(url);
            image.src = url;
            image.alt = 'Preview gambar baru ' + (index + 1);
            var caption = document.createElement('span');
            caption.className = 'product-image-choice-caption';
            caption.textContent = 'Upload baru ' + (index + 1);
            var action = document.createElement('span');
            action.className = 'product-image-choice-action';
            var name = document.createElement('span');
            name.className = 'product-image-file-name';
            name.textContent = file.name;
            var remove = document.createElement('button');
            remove.type = 'button';
            remove.className = 'btn btn-sm btn-outline-danger';
            remove.textContent = 'Hapus pilihan';
            remove.addEventListener('click', function () {
                setFiles(files.filter(function (_, fileIndex) { return fileIndex !== index; }));
            });
            action.append(name, remove);
            card.append(image, caption, action);
            newGrid.appendChild(card);
        });
        updateCount();
    }

    function updateCount() {
        var current = retainedCount() + (input.files ? input.files.length : 0);
        count.textContent = current + ' / ' + maxImages + ' gambar';
        count.classList.toggle('is-over-limit', current > maxImages);
        editor.querySelectorAll('[data-retain-image]').forEach(function (checkbox) {
            var label = checkbox.closest('[data-existing-image]');
            label.classList.toggle('selected', checkbox.checked);
            label.classList.toggle('removed', !checkbox.checked);
            label.querySelector('[data-retain-label]').textContent = checkbox.checked ? 'Dipertahankan' : 'Akan dihapus';
        });
    }

    function validateSelection() {
        var files = Array.from(input.files || []);
        var bytes = files.reduce(function (total, file) { return total + file.size; }, 0);
        var tooLargeFile = files.some(function (file) { return file.size > maxFileBytes; });
        var message = '';
        if (retainedCount() + files.length > maxImages) message = 'Maksimal 15 gambar total. Hapus pilihan gambar atau upload lebih sedikit.';
        else if (tooLargeFile) message = 'Ukuran setiap file maksimal 5 MB.';
        else if (bytes > maxTotalBytes) message = 'Total file upload maksimal 50 MB.';
        error.textContent = message;
        error.hidden = !message;
        return !message;
    }

    input.addEventListener('change', function () {
        renderNewFiles();
        validateSelection();
    });
    editor.addEventListener('change', function (event) {
        if (event.target.matches('[data-retain-image]')) {
            updateCount();
            validateSelection();
        }
    });
    form.addEventListener('submit', function (event) {
        if (!validateSelection()) {
            event.preventDefault();
            error.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    });
    renderNewFiles();
    updateCount();
}());

// Wilayah Indonesia dimuat berurutan; pilihan turunan mengikuti provinsi/kota yang dipilih.
(function () {
    var levels = ['province', 'city', 'district', 'village'];
    var cache = new Map();

    function normalizeName(value) {
        return (value || '').trim().toLocaleLowerCase('id-ID').replace(/\s+/g, ' ');
    }

    function comparableName(value) {
        return normalizeName(value)
            .replace(/^(kota administrasi|kabupaten administrasi|kabupaten|kota)\s+/i, '')
            .replace(/\s+/g, ' ');
    }

    async function fetchRegions(level, parentCode) {
        var params = new URLSearchParams({ level: level });
        if (parentCode) params.set('parentCode', parentCode);
        var url = '/Region/List?' + params.toString();
        if (cache.has(url)) return cache.get(url);

        var response = await fetch(url, { headers: { Accept: 'application/json' } });
        if (!response.ok) throw new Error('Wilayah tidak dapat dimuat.');
        var items = await response.json();
        if (!Array.isArray(items)) throw new Error('Format data wilayah tidak valid.');
        cache.set(url, items);
        return items;
    }

    function findRegion(items, requestedName) {
        if (!requestedName) return null;
        var requested = normalizeName(requestedName);
        var exact = items.find(function (item) { return normalizeName(item.name) === requested; });
        if (exact) return exact;

        var comparable = comparableName(requestedName);
        var matches = items.filter(function (item) { return comparableName(item.name) === comparable; });
        return matches.length === 1 ? matches[0] : null;
    }

    document.querySelectorAll('[data-region-cascade]').forEach(function (section) {
        var root = section.querySelector('[data-region-root]');
        if (!root) return;

        var selects = {};
        var hiddenFields = {};
        levels.forEach(function (level) {
            selects[level] = root.querySelector('[data-region-select="' + level + '"]');
            hiddenFields[level] = root.querySelector('[data-region-value="' + level + '"]');
        });

        var status = root.querySelector('[data-region-status]');
        var retryButton = root.querySelector('[data-region-retry]');
        if (!levels.every(function (level) { return selects[level] && hiddenFields[level]; })) return;

        var requestVersion = 0;
        var $ = window.jQuery;
        var placeholders = {
            province: 'Pilih provinsi',
            city: 'Pilih kota/kabupaten',
            district: 'Pilih kecamatan',
            village: 'Pilih kelurahan/desa'
        };

        function setStatus(message, failed) {
            if (status) status.textContent = message;
            if (retryButton) retryButton.hidden = !failed;
        }

        if ($ && $.fn.select2) {
            levels.forEach(function (level) {
                $(selects[level]).select2({
                    width: '100%',
                    placeholder: placeholders[level],
                    allowClear: false,
                    language: {
                        noResults: function () { return 'Pilihan tidak ditemukan'; },
                        searching: function () { return 'Mencari…'; },
                        inputTooShort: function () { return 'Ketik untuk mencari'; }
                    }
                });
            });
        }

        function syncSelect2(select) {
            if ($ && $.fn.select2) $(select).trigger('change.select2');
        }

        function setDisabled(select, disabled) {
            select.disabled = disabled;
            if ($ && $.fn.select2) {
                $(select).prop('disabled', disabled).trigger('change.select2');
                $(select).next('.select2-container').toggleClass('select2-container--disabled', disabled);
            }
        }

        function setOptions(level, items, requestedName) {
            var select = selects[level];
            select.replaceChildren(new Option(placeholders[level], ''));
            items.forEach(function (item) {
                if (!item || !/^\d+(?:\.\d+)*$/.test(String(item.id)) || typeof item.name !== 'string') return;
                var option = new Option(item.name, String(item.id));
                option.dataset.regionName = item.name;
                select.add(option);
            });
            syncSelect2(select);

            var match = findRegion(items, requestedName);
            if (match) {
                select.value = String(match.id);
                hiddenFields[level].value = match.name;
                syncSelect2(select);
                return match;
            }
            select.value = '';
            syncSelect2(select);
            return null;
        }

        function clearFollowing(index) {
            for (var i = index + 1; i < levels.length; i++) {
                var level = levels[i];
                selects[level].replaceChildren(new Option(placeholders[level], ''));
                setDisabled(selects[level], true);
                hiddenFields[level].value = '';
                syncSelect2(selects[level]);
            }
        }

        async function loadChild(parentIndex, parentId, requestedName, version) {
            var childLevel = levels[parentIndex + 1];
            var childSelect = selects[childLevel];
            setDisabled(childSelect, true);
            childSelect.replaceChildren(new Option('Memuat pilihan…', ''));
            syncSelect2(childSelect);

            var items = await fetchRegions(childLevel, parentId);
            if (version !== requestVersion) return null;

            var selected = setOptions(childLevel, items, requestedName);
            setDisabled(childSelect, false);
            syncSelect2(childSelect);
            return selected;
        }

        async function initialize() {
            var version = ++requestVersion;
            var savedNames = {};
            levels.forEach(function (level) { savedNames[level] = hiddenFields[level].value; });
            levels.slice(1).forEach(function (level) {
                setDisabled(selects[level], true);
            });
            setStatus('Memuat data wilayah Indonesia…', false);

            try {
                var provinces = await fetchRegions('province');
                if (version !== requestVersion) return;
                setDisabled(selects.province, false);
                var selectedProvince = setOptions('province', provinces, savedNames.province);
                if (!selectedProvince) {
                    setStatus('Pilih provinsi, lalu lanjutkan ke kota/kabupaten, kecamatan, dan kelurahan/desa.', false);
                    return;
                }

                var parent = selectedProvince;
                for (var i = 0; i < levels.length - 1; i++) {
                    var selectedChild = await loadChild(i, parent.id, savedNames[levels[i + 1]], version);
                    if (version !== requestVersion) return;
                    if (!selectedChild) {
                        setStatus('Pilih wilayah yang belum dikenali untuk melanjutkan urutan alamat.', false);
                        return;
                    }
                    parent = selectedChild;
                }

                setStatus('Wilayah lengkap. Data pilihan mengikuti daftar administrasi Indonesia.', false);
            } catch (_) {
                setDisabled(selects.province, false);
                setStatus('Daftar wilayah gagal dimuat. Periksa koneksi internet, lalu coba lagi.', true);
            }
        }

        levels.forEach(function (level, index) {
            var handleRegionChange = async function () {
                var version = ++requestVersion;
                var selectedOption = selects[level].selectedOptions[0];
                hiddenFields[level].value = selectedOption && selectedOption.value
                    ? selectedOption.dataset.regionName || selectedOption.textContent
                    : '';
                clearFollowing(index);

                if (!selectedOption || !selectedOption.value || index === levels.length - 1) {
                    setStatus(index === levels.length - 1 ? 'Kelurahan/desa dipilih.' : 'Pilih wilayah berikutnya secara berurutan.', false);
                    return;
                }

                try {
                    await loadChild(index, selectedOption.value, '', version);
                    if (version === requestVersion) setStatus('Lanjutkan memilih wilayah pada daftar berikutnya.', false);
                } catch (_) {
                    if (version === requestVersion) setStatus('Daftar wilayah gagal dimuat. Periksa koneksi internet, lalu coba lagi.', true);
                }
            };

            if ($ && $.fn.select2) {
                $(selects[level]).on('change.fauzoraRegion', handleRegionChange);
            } else {
                selects[level].addEventListener('change', handleRegionChange);
            }
        });

        if (retryButton) retryButton.addEventListener('click', initialize);
        initialize();
    });
}());

// Pilihan bahasa disimpan lokal agar reload tidak kembali ke Indonesia lalu menerjemahkan ulang.
(function () {
    var select = document.getElementById('lang-select');
    if (!select) return;

    var storageKey = 'fauzora-language';
    var savedLanguage = null;
    try { savedLanguage = localStorage.getItem(storageKey); } catch (_) { }

    var match = document.cookie.match(/(?:^|;\s*)googtrans=\/[^/]+\/([^;]+)/);
    var cookieLanguage = match ? decodeURIComponent(match[1]) : null;
    var supportedLanguages = ['id', 'en', 'ms', 'zh-CN', 'ja'];
    var current = supportedLanguages.indexOf(savedLanguage) >= 0
        ? savedLanguage
        : (supportedLanguages.indexOf(cookieLanguage) >= 0 ? cookieLanguage : 'id');

    if (current === 'id') {
        var expired = 'expires=Thu, 01 Jan 1970 00:00:00 GMT';
        document.cookie = 'googtrans=; ' + expired + '; path=/';
        document.cookie = 'googtrans=; ' + expired + '; path=/; domain=' + location.hostname;
    } else if (current !== cookieLanguage) {
        document.cookie = 'googtrans=/id/' + encodeURIComponent(current) + '; path=/';
    }
    try { localStorage.setItem(storageKey, current); } catch (_) { }
    select.value = current;

    if (current !== 'id') {
        window.googleTranslateElementInit = function () {
            new google.translate.TranslateElement({
                pageLanguage: 'id',
                includedLanguages: 'id,en,ms,zh-CN,ja',
                autoDisplay: false
            }, 'google_translate_element');
        };

        var script = document.createElement('script');
        script.src = 'https://translate.google.com/translate_a/element.js?cb=googleTranslateElementInit';
        document.head.appendChild(script);
    }

    select.addEventListener('change', function () {
        var nextLanguage = select.value;
        if (nextLanguage === current) return;

        var expired = 'expires=Thu, 01 Jan 1970 00:00:00 GMT';
        document.cookie = 'googtrans=; ' + expired + '; path=/';
        document.cookie = 'googtrans=; ' + expired + '; path=/; domain=' + location.hostname;
        try {
            localStorage.setItem(storageKey, nextLanguage);
        } catch (_) { }
        if (nextLanguage !== 'id') document.cookie = 'googtrans=/id/' + encodeURIComponent(nextLanguage) + '; path=/';
        location.reload();
    });
})();

// Galeri produk: thumbnail ikut slide, hover memperbesar, klik membuka lightbox.
document.querySelectorAll('[data-gallery]').forEach(function (gallery) {
    var thumbs = document.querySelectorAll('.thumb');
    var box = document.getElementById('lightbox');
    var lightboxEl = document.getElementById('lightboxCarousel');

    function trackPointer(el) {
        el.addEventListener('mousemove', function (e) {
            var r = el.getBoundingClientRect();
            el.style.setProperty('--zx', ((e.clientX - r.left) / r.width * 100) + '%');
            el.style.setProperty('--zy', ((e.clientY - r.top) / r.height * 100) + '%');
        });
    }

    gallery.addEventListener('slid.bs.carousel', function (e) {
        thumbs.forEach(function (t, i) { t.classList.toggle('active', i === e.to); });
    });

    if (!box || !lightboxEl) return;
    var items = lightboxEl.querySelectorAll('.carousel-item');
    var stages = lightboxEl.querySelectorAll('.lightbox-stage');

    function resetZoom() { stages.forEach(function (s) { s.classList.remove('zoomed'); }); }

    gallery.querySelectorAll('.zoomable').forEach(function (zoomable) {
        trackPointer(zoomable);
        zoomable.addEventListener('click', function () {
            var index = Number(zoomable.dataset.lightboxIndex) || 0;
            resetZoom();
            items.forEach(function (item, i) { item.classList.toggle('active', i === index); });
            bootstrap.Modal.getOrCreateInstance(box).show();
        });
    });

    stages.forEach(function (stage) {
        trackPointer(stage);
        stage.addEventListener('click', function () { stage.classList.toggle('zoomed'); });
    });
    lightboxEl.addEventListener('slid.bs.carousel', resetZoom);
    box.addEventListener('hidden.bs.modal', resetZoom);
});

// Tombol +/- jumlah dibatasi oleh atribut min dan max pada input.
document.querySelectorAll('[data-qty]').forEach(function (box) {
    var input = box.querySelector('input');

    function clamp() {
        var min = Number(input.min) || 1;
        var max = Number(input.max) || Infinity;
        var value = parseInt(input.value, 10);
        input.value = Math.min(max, Math.max(min, isNaN(value) ? min : value));
    }

    box.querySelectorAll('[data-step]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            input.value = (parseInt(input.value, 10) || 0) + Number(btn.dataset.step);
            clamp();
        });
    });
    input.addEventListener('change', clamp);
});

// Input uang: tampil dengan pemisah ribuan, nilai asli (angka murni) dikirim lewat input hidden.
document.querySelectorAll('[data-money]').forEach(function (display) {
    var hidden = document.getElementById(display.dataset.money);
    var hint = document.querySelector('[data-money-hint="' + display.dataset.money + '"]');
    var nf = new Intl.NumberFormat('id-ID');

    function sync() {
        var digits = display.value.replace(/\D/g, '');
        hidden.value = digits;
        display.value = digits ? nf.format(Number(digits)) : '';
        if (hint) hint.textContent = digits ? 'Rp ' + nf.format(Number(digits)) : '';
    }

    display.addEventListener('input', sync);
    sync();
});
