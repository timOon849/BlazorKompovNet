window.kompovDashboardCharts = {
    charts: {},

    render(canvas, config) {
        if (!window.Chart) {
            console.error("Chart.js is not loaded.");
            return;
        }

        const ctx = canvas.getContext("2d");
        const id = config.id;

        if (this.charts[id]) {
            this.charts[id].destroy();
        }

        const options = {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: config.legendPosition || "bottom"
                }
            }
        };

        if (config.type !== "pie" && config.type !== "doughnut") {
            options.scales = {
                y: {
                    beginAtZero: config.beginAtZero !== false,
                    ticks: {
                        precision: 0
                    }
                }
            };
        }

        this.charts[id] = new Chart(ctx, {
            type: config.type,
            data: {
                labels: config.labels,
                datasets: config.datasets
            },
            options
        });
    },

    dispose(id) {
        if (this.charts[id]) {
            this.charts[id].destroy();
            delete this.charts[id];
        }
    }
};
