import { injectGlobalData } from '@/views/provide';
import { screenUpdate } from '../../../../apis/screen'
import { subNotifyMsg } from '@/apis/request';
export default {
    field() {
        return {
            Screen: {
                fps: 0,
                fpsTimes: 0,
                img: null,
                LastInput: 0,
                UserName: '',
                KeyBoard: '',
                KeyBoardOld: '',
                KeyBoardLength: 0,
            }
        };
    },
    reportTimer: 0,
    globalData: null,
    reported: true,
    init() {
        this.globalData = injectGlobalData();;
        this.reportInterval();
        this.subMessage();
        this.fpsInterval();
        this.draw();
    },

    draw() {
        const devices = this.globalData.value.devices.filter(c => this.globalData.value.reportNames.indexOf(c.MachineName) >= 0);

        for (let i = 0; i < devices.length; i++) {
            const item = devices[i];
            if (!item.canvas) {
                item.canvas = document.getElementById(`canvas-${item.MachineName}`);
                if (item.canvas) {
                    item.ctx = item.canvas.getContext('2d');
                }
            }
            if (item.ctx) {
                const img = item.Screen.img;
                if (img) {
                    item.ctx.clearRect(0, 0, item.canvas.width, item.canvas.height);
                    item.ctx.drawImage(img, 0, 0, img.width, img.height, 0, 0, item.canvas.width, item.canvas.height);
                    this.drawFps(item);
                    this.drawKeyboard(item);
                }
                for (let j in item) {
                    try {
                        if (item[j] && item[j].draw) {
                            item[j].draw(item.canvas, item.ctx);
                        }
                    } catch (e) {
                        console.log(e);
                    }
                }
            }
        }
        requestAnimationFrame(() => {
            this.draw();
        });
    },
    drawKeyboard(item) {
        if (item.Screen.KeyBoard && item.Screen.KeyBoardLength < 60) {
            item.Screen.KeyBoardLength++;
            item.ctx.lineWidth = 10;
            let top = (item.canvas.height - 100) / 2;
            let left = (item.canvas.width - 50 * item.Screen.KeyBoard.length) / 2;
            item.ctx.font = 'bold 100px Arial';
            item.ctx.fillStyle = 'rgba(0,0,0,0.5)';
            item.ctx.fillText(`${item.Screen.KeyBoard}`, left, top);
            item.ctx.lineWidth = 5;
            item.ctx.strokeStyle = 'rgba(255,255,255,0.7)';
            item.ctx.strokeText(`${item.Screen.KeyBoard}`, left, top);
        }
    },
    drawFps(item) {
        item.ctx.lineWidth = 5;
        item.ctx.font = 'bold 60px Arial';
        item.ctx.fillStyle = 'rgba(0,0,0,0.5)';
        item.ctx.fillText(`FPS : ${item.Screen.fps} 、LT : ${item.Screen.LastInput}ms`, 50, 70);
        item.ctx.lineWidth = 2;
        item.ctx.strokeStyle = '#fff';
        item.ctx.strokeText(`FPS : ${item.Screen.fps} 、LT : ${item.Screen.LastInput}ms`, 50, 70);
    },

    subMessage() {
        subNotifyMsg('/notify/report/screen', (res, param) => {
            if (this.globalData.value.reportNames.indexOf(res.Name) == -1) return;
            let item = this.globalData.value.devices.filter(c => c.MachineName == res.Name)[0];
            if (item) {
                item.Screen.fpsTimes++;
                const img = new Image();
                if (typeof res.Img == 'string') {
                    img.src = `data:image/jpg;base64,${res.Img}`;
                    img.onload = function () {
                        item.Screen.img = img;
                    };
                } else {
                    const reader = new FileReader();
                    reader.readAsDataURL(res.Img);
                    reader.onload = function (e) {
                        img.src = e.target.result;
                        img.onload = function () {
                            item.Screen.img = img;
                        };
                    };
                }
            }
        });
    },

    fpsInterval() {
        this.globalData.value.devices.forEach(item => {
            item.Screen.fps = item.Screen.fpsTimes;
            item.Screen.fpsTimes = 0;
        });
        setTimeout(() => {
            this.fpsInterval();
        }, 1000)
    },

    reported: true,
    reportInterval() {
        if (this.reported) {
            this.reported = false;
            screenUpdate(this.globalData.value.reportNames).then(() => {
                this.reported = true;
                this.reportTimer = setTimeout(() => {
                    this.reportInterval();
                }, 30);
            }).catch(() => {
                this.reported = true;
                this.reportTimer = setTimeout(() => {
                    this.reportInterval();
                }, 30);
            });
        } else {
            this.reportTimer = setTimeout(() => {
                this.reportInterval();
            }, 30);
        }
    },

    update(item, report) {
        if (report.Screen) {
            item.Screen.LastInput = report.Screen.LastInput || '0';
            item.Screen.UserName = report.Screen.UserName || '';
            if (report.Screen.KeyBoard != item.Screen.KeyBoard) {
                item.Screen.KeyBoard = report.Screen.KeyBoard || '';
                item.Screen.KeyBoardLength = 0;
            }
        }
    }
}