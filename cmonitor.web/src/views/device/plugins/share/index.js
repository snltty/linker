export default {
    field() {
        return {
            Share: {
                KeyBoard: { Index: 0, Value: '' },
                UserName: { Index: 1, Value: '' },
                Lock: { Index: 2, Value: '' },
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
                }
            }
        };
    },
    init() {
    },
    update(item, report) {
        console.log(report.Share);
        if (report.Share) {
            if (report.Share.UserName) {
                item.Share.UserName.Index = report.Share.UserName.Index;
                item.Share.UserName.Value = report.Share.UserName.Value;
            }
            if (report.Share.Lock) {
                item.Share.Lock.Index = report.Share.Lock.Index;
                item.Share.Lock.Value = report.Share.Lock.Value;
            }
            if (report.Share.KeyBoard) {
                item.Share.KeyBoard.Index = report.Share.KeyBoard.Index;
                item.Share.KeyBoard.Value = report.Share.KeyBoard.Value;
            }
        }
    }
}