export default {
    field() {
        return {
            Volume: {
                Value: 0,
                Mute: false,
                MasterPeak: 0,
                draw(canvas, ctx) {
                    this.MasterPeak -= 1;
                    if (this.MasterPeak < 0) {
                        this.MasterPeak = 0;
                        return;
                    }
                    ctx.beginPath();
                    ctx.fillStyle = '#0f0';
                    ctx.fillRect(0, canvas.height - 10, this.MasterPeak / 100 * canvas.width, 10);
                    ctx.closePath();
                }
            },
        }
    },
    state: {
        volume: {
            showVolume: false,
            showVolumeSingle: false,
            items: []
        }
    },
    init() {
    },
    update(item, report) {
        item.Volume.Value = report.Volume.Value;
        item.Volume.Mute = report.Volume.Mute;
        if (report.Volume.MasterPeak > item.Volume.MasterPeak) {
            item.Volume.MasterPeak = report.Volume.MasterPeak;
        }
    }
}