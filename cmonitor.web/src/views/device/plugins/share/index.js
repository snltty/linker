export default {
    pluginName: 'cmonitor.plugin.share.',
    field() {
        return {
            Share: {
                KeyBoard: { Index: 0, Value: '' },
                UserName: { Index: 1, Value: '' },
                Lock: { Index: 2, Value: { type: 'list', val: 'none', star1: 0, star2: 0, star3: 0, notify: false, remarked: false, typeText: '' } },

                draw(canvas, ctx) {
                    if (this.KeyBoard.Value) {
                        let top = (canvas.height - 100) / 2;
                        let left = (canvas.width - 50 * this.KeyBoard.Value.length) / 2;
                        ctx.font = 'bold 100px Arial';
                        ctx.fillStyle = 'rgba(0,0,0,0.5)';
                        ctx.fillText(`${this.KeyBoard.Value}`, left, top);
                        ctx.lineWidth = 5;
                        ctx.strokeStyle = 'rgba(255,255,255,0.7)';
                        ctx.strokeText(`${this.KeyBoard.Value}`, left, top);
                    }
                    if (this.Lock.Value.val == 'star' || this.Lock.Value.val == 'ask') {
                        let str = '';

                        str = str.padEnd(5, '★');
                        let top = 120;
                        let left = (canvas.width - 100 * 5) / 2;
                        ctx.beginPath();
                        ctx.lineWidth = 5;
                        ctx.font = 'bold 100px Arial';
                        ctx.strokeStyle = '#fff';
                        ctx.strokeText(str, left, top);
                        ctx.lineWidth = 7;
                        ctx.strokeStyle = 'rgba(0,0,0,0.5)';
                        ctx.strokeText(str, left, top);

                        str = '';
                        const value = this.Lock.Value;
                        const star = parseInt((value.star1 + value.star2 + value.star3) / 3);
                        str = str.padEnd(star, '☆');
                        ctx.lineWidth = 2;
                        ctx.strokeStyle = 'yellow';
                        ctx.strokeText(str, left, top);
                        ctx.closePath();
                    }
                }
            }
        };
    },
    init() {
    },
    update(item, report) {
        if (!report.Share) return;

        if (report.Share.UserName) {
            item.Share.UserName.Index = report.Share.UserName.Index;
            item.Share.UserName.Value = report.Share.UserName.Value;
        }
        if (report.Share.Lock) {
            item.Share.Lock.Index = report.Share.Lock.Index;
            if (report.Share.Lock.Value) {
                const json = JSON.parse(report.Share.Lock.Value);
                item.Share.Lock.Value.type = json.type;
                item.Share.Lock.Value.val = json.val;
                item.Share.Lock.Value.remarked = json.remarked;
                item.Share.Lock.Value.star1 = json.star1 || 0;
                item.Share.Lock.Value.star2 = json.star2 || 0;
                item.Share.Lock.Value.star3 = json.star3 || 0;
                item.Share.Lock.Value.star4 = json.star4 || 0;
                item.Share.Lock.Value.star5 = json.star5 || 0;
                item.Share.Lock.Value.typeText = json.typeText;
            }
        }
        if (report.Share.KeyBoard) {
            item.Share.KeyBoard.Index = report.Share.KeyBoard.Index;
            item.Share.KeyBoard.Value = report.Share.KeyBoard.Value;
        }
    }
}