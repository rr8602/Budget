window.pwaInterop = {
    canInstall: function () {
        return window._pwaInstallPrompt !== null;
    },
    install: async function () {
        if (!window._pwaInstallPrompt) return false;
        window._pwaInstallPrompt.prompt();
        const result = await window._pwaInstallPrompt.userChoice;
        window._pwaInstallPrompt = null;
        return result.outcome === 'accepted';
    }
};
