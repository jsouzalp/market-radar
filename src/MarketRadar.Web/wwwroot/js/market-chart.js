window.marketChart = (function () {
    let _chart = null;
    let _series = {};
    let _initialized = false;

    function init(elementId) {
        const container = document.getElementById(elementId);
        if (!container || !window.LightweightCharts) return;

        _chart = LightweightCharts.createChart(container, {
            width: container.clientWidth,
            height: 350,
            layout: { background: { color: '#1e1e2f' }, textColor: '#d1d5db' },
            grid:   { vertLines: { color: '#2d2d44' }, horzLines: { color: '#2d2d44' } },
            timeScale: { timeVisible: true, secondsVisible: false }
        });

        _series.price     = _chart.addLineSeries({ color: '#60a5fa', lineWidth: 2, title: 'Preço' });
        _series.trendLine = _chart.addLineSeries({ color: '#f59e0b', lineWidth: 1, lineStyle: 1, title: 'Tendência' });
        _series.ema9      = _chart.addLineSeries({ color: '#34d399', lineWidth: 1, title: 'EMA 9' });
        _series.ema21     = _chart.addLineSeries({ color: '#a78bfa', lineWidth: 1, title: 'EMA 21' });
        _series.ema50     = _chart.addLineSeries({ color: '#fb923c', lineWidth: 1, title: 'EMA 50' });

        _initialized = true;
    }

    function update(data) {
        if (!_initialized || !data) return;
        if (data.closePrices) _series.price.setData(data.closePrices);
        if (data.trendLine)   _series.trendLine.setData(data.trendLine);
        if (data.ema9)        _series.ema9.setData(data.ema9);
        if (data.ema21)       _series.ema21.setData(data.ema21);
        if (data.ema50)       _series.ema50.setData(data.ema50);
        if (data.markers)     _series.price.setMarkers(data.markers);
        _chart.timeScale().fitContent();
    }

    function playAlert(severity) {
        try {
            const ctx = new (window.AudioContext || window.webkitAudioContext)();
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();
            osc.connect(gain);
            gain.connect(ctx.destination);
            osc.frequency.value = severity === 'Critical' ? 880 : 660;
            gain.gain.setValueAtTime(0.3, ctx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.5);
            osc.start(ctx.currentTime);
            osc.stop(ctx.currentTime + 0.5);
        } catch (_) {}
    }

    return { init, update, playAlert };
})();
