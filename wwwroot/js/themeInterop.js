// Enhanced navigation: smooth-scroll 비활성화 (스크롤 위치는 그대로 유지)
document.addEventListener('blazor:navigating', function () {
    document.documentElement.style.scrollBehavior = 'auto';
    document.body.style.scrollBehavior = 'auto';
});

window.themeInterop = {
    blurActive: function () {
        // Blazor 하이드레이션 후 자동 포커스된 요소를 blur
        // setTimeout(0): Blazor의 포커스 복원이 끝난 뒤 실행 보장
        setTimeout(function () {
            if (document.activeElement && document.activeElement !== document.body)
                document.activeElement.blur();
        }, 0);
    },
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
    }
};
