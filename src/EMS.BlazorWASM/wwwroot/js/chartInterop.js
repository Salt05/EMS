// ============================================================
// EMS Chart.js Interop — Blazor WASM ↔ Chart.js bridge
// ============================================================

window.emsCharts = {
    _instances: {},

    /**
     * Creates or re-creates a Chart.js chart inside the given canvas element.
     * @param {string} canvasId  - DOM id of the <canvas> element.
     * @param {string} type      - Chart type: 'bar', 'line', 'doughnut'.
     * @param {string} labelsJson - JSON-serialised string[] of labels.
     * @param {string} dataJson   - JSON-serialised number[] of data values.
     * @param {string} label      - Dataset label.
     * @param {string} colorsJson - JSON-serialised string[] of colours (optional).
     */
    create: function (canvasId, type, labelsJson, dataJson, label, colorsJson) {
        // Destroy previous instance if it exists
        if (this._instances[canvasId]) {
            this._instances[canvasId].destroy();
            delete this._instances[canvasId];
        }

        var canvas = document.getElementById(canvasId);
        if (!canvas) return;

        var labels = JSON.parse(labelsJson);
        var data = JSON.parse(dataJson);
        var colors = colorsJson ? JSON.parse(colorsJson) : null;

        var defaultPalette = [
            '#1A1F36', '#3B82F6', '#10B981', '#F59E0B', '#EF4444',
            '#8B5CF6', '#EC4899', '#06B6D4', '#84CC16', '#F97316'
        ];

        var bgColors = colors || (type === 'doughnut'
            ? defaultPalette.slice(0, data.length)
            : Array(data.length).fill('#1A1F36'));

        var config = {
            type: type,
            data: {
                labels: labels,
                datasets: [{
                    label: label || '',
                    data: data,
                    backgroundColor: bgColors,
                    borderColor: type === 'line' ? '#1A1F36' : bgColors,
                    borderWidth: type === 'line' ? 2 : 1,
                    borderRadius: type === 'bar' ? 6 : 0,
                    tension: type === 'line' ? 0.35 : 0,
                    fill: type === 'line',
                    pointBackgroundColor: '#1A1F36',
                    pointRadius: type === 'line' ? 4 : 0,
                    pointHoverRadius: type === 'line' ? 6 : 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: { duration: 600, easing: 'easeOutQuart' },
                plugins: {
                    legend: { display: type === 'doughnut', position: 'bottom' },
                    tooltip: {
                        backgroundColor: '#1A1F36',
                        titleFont: { family: 'Inter', size: 12 },
                        bodyFont: { family: 'Inter', size: 11 },
                        cornerRadius: 8,
                        padding: 10
                    }
                },
                scales: type === 'doughnut' ? {} : {
                    x: {
                        grid: { display: false },
                        ticks: { font: { family: 'Inter', size: 11 }, color: '#787774' }
                    },
                    y: {
                        beginAtZero: true,
                        grid: { color: 'rgba(227,232,238,0.5)' },
                        ticks: { font: { family: 'Inter', size: 11 }, color: '#787774' }
                    }
                }
            }
        };

        // For line charts, add gradient fill
        if (type === 'line') {
            var ctx = canvas.getContext('2d');
            var gradient = ctx.createLinearGradient(0, 0, 0, canvas.height);
            gradient.addColorStop(0, 'rgba(26, 31, 54, 0.12)');
            gradient.addColorStop(1, 'rgba(26, 31, 54, 0.01)');
            config.data.datasets[0].backgroundColor = gradient;
        }

        this._instances[canvasId] = new Chart(canvas, config);
    },

    /**
     * Destroys a chart instance by canvas id.
     */
    destroy: function (canvasId) {
        if (this._instances[canvasId]) {
            this._instances[canvasId].destroy();
            delete this._instances[canvasId];
        }
    },

    /**
     * Triggers a browser file download from a byte array (base64 encoded).
     */
    downloadFile: function (fileName, contentType, base64Content) {
        var binary = atob(base64Content);
        var array = new Uint8Array(binary.length);
        for (var i = 0; i < binary.length; i++) {
            array[i] = binary.charCodeAt(i);
        }
        var blob = new Blob([array], { type: contentType });
        var link = document.createElement('a');
        link.href = window.URL.createObjectURL(blob);
        link.download = fileName;
        link.click();
        window.URL.revokeObjectURL(link.href);
    }
};
