window.chartInterop = {
    _instances: {},

    // 십자선 플러그인 — line 차트 전용
    _crosshairPlugin: {
        id: 'crosshair',
        afterDraw(chart) {
            if (!chart.tooltip._active?.length) return;
            const ctx  = chart.ctx;
            const pt   = chart.tooltip._active[0];
            const x    = pt.element.x;
            const y    = pt.element.y;
            const { top, bottom, left, right } = chart.chartArea;

            ctx.save();
            ctx.lineWidth    = 1;
            ctx.strokeStyle  = 'rgba(108,99,255,0.45)';
            ctx.setLineDash([4, 4]);

            ctx.beginPath(); ctx.moveTo(x, top);  ctx.lineTo(x, bottom); ctx.stroke(); // 수직선
            ctx.beginPath(); ctx.moveTo(left, y); ctx.lineTo(right, y);  ctx.stroke(); // 수평선

            ctx.restore();
        }
    },

    draw: function (canvasId, type, labels, datasets, options) {
        var canvas = document.getElementById(canvasId);
        if (!canvas) return;

        if (this._instances[canvasId]) {
            this._instances[canvasId].destroy();
            delete this._instances[canvasId];
        }

        var config = {
            type: type,
            data: { labels: labels, datasets: datasets },
            options: options || this._defaults(type)
        };

        if (type === 'line') config.plugins = [this._crosshairPlugin];

        this._instances[canvasId] = new Chart(canvas.getContext('2d'), config);
    },

    destroy: function (canvasId) {
        if (this._instances[canvasId]) {
            this._instances[canvasId].destroy();
            delete this._instances[canvasId];
        }
    },

    _defaults: function (type) {
        var fontFamily = "'Roboto','Apple SD Gothic Neo','Malgun Gothic',sans-serif";

        var base = {
            responsive: true,
            maintainAspectRatio: true,
            animation: { duration: 700, easing: 'easeInOutQuart' },
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        padding: 14,
                        font: { size: 11, family: fontFamily },
                        boxWidth: 10, boxHeight: 10,
                        usePointStyle: true, pointStyle: 'circle'
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(15,20,30,0.90)',
                    titleFont: { size: 12, weight: '700', family: fontFamily },
                    bodyFont: { size: 12, family: fontFamily },
                    padding: 10, cornerRadius: 10,
                    callbacks: {
                        label: function (ctx) {
                            var val = ctx.parsed;
                            if (typeof val === 'object' && val.y !== undefined) val = val.y;
                            return ' ' + Number(val).toLocaleString() + '원';
                        }
                    }
                }
            }
        };

        if (type === 'doughnut') {
            base.cutout = '68%';
            base.plugins.legend.position = 'bottom';
        }

        if (type === 'line') {
            base.interaction = { mode: 'index', intersect: false };
            base.plugins.legend.display = false;
            base.plugins.tooltip = {
                mode: 'index',
                intersect: false,
                backgroundColor: 'rgba(12,8,40,0.93)',
                borderColor: 'rgba(108,99,255,0.45)',
                borderWidth: 1,
                padding: 12,
                cornerRadius: 12,
                displayColors: false,
                titleFont: { size: 13, weight: '700', family: fontFamily },
                bodyFont: { size: 12, family: fontFamily },
                titleColor: '#fff',
                bodyColor: 'rgba(255,255,255,0.75)',
                callbacks: {
                    title: function (ctx) {
                        return ctx[0].label + '일';
                    },
                    label: function (ctx) {
                        var idx  = ctx.dataIndex;
                        var data = ctx.dataset.data;
                        var cum  = data[idx] || 0;
                        var prev = idx > 0 ? (data[idx - 1] || 0) : 0;
                        var day  = cum - prev;
                        return [
                            '  누적  ' + Number(cum).toLocaleString() + '원',
                            '  하루  ' + Number(day).toLocaleString() + '원'
                        ];
                    }
                }
            };
            base.scales = {
                y: {
                    beginAtZero: true,
                    grid: { color: 'rgba(108,99,255,0.07)', drawBorder: false },
                    border: { display: false },
                    ticks: {
                        font: { size: 10, family: fontFamily },
                        maxTicksLimit: 5,
                        callback: function (v) {
                            if (v === 0) return '0';
                            if (v >= 10000) return (v / 10000).toFixed(0) + '만';
                            return v.toLocaleString();
                        }
                    }
                },
                x: {
                    grid: { display: false },
                    border: { display: false },
                    ticks: { font: { size: 10, family: fontFamily }, maxTicksLimit: 8 }
                }
            };
        }

        return base;
    }
};
