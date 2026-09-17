window.themeInterop = {
    getSystemDark: function () {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    },
    getSaved: function (key) {
        return localStorage.getItem(key);
    },
    setSaved: function (key, value) {
        localStorage.setItem(key, value);
    },
    setDark: function (isDark) {
        if (isDark) document.body.classList.add('hb-dark');
        else        document.body.classList.remove('hb-dark');
    },
    scrollToId: function (id) {
        const el = document.getElementById(id);
        if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
    },
    triggerInput: function (id, dotNetRef) {
        const input = document.getElementById(id);
        if (!input) return;

        input.addEventListener('change', async function () {
            const file = input.files[0];
            input.value = '';

            if (!file) return;

            await dotNetRef.invokeMethodAsync('SetUploading', true);

            try {
                const formData = new FormData();
                formData.append('file', file);

                const response = await fetch('/api/receipts/upload', {
                    method: 'POST',
                    body: formData
                });

                if (response.ok) {
                    const url = await response.text();
                    await dotNetRef.invokeMethodAsync('NotifyUploaded', url);
                } else {
                    const msg = await response.text();
                    await dotNetRef.invokeMethodAsync('NotifyError', msg || '업로드에 실패했습니다.');
                }
            } catch {
                await dotNetRef.invokeMethodAsync('NotifyError', '업로드에 실패했습니다. 다시 시도해 주세요.');
            }
        }, { once: true });

        try { input.click(); } catch { /* 브라우저 보안 제한으로 다이얼로그 열기 실패 */ }
    }
};
