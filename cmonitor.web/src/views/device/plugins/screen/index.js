import { injectGlobalData } from '@/views/provide';
import { screenClip, screenUpdate } from '../../../../apis/screen'
import { subNotifyMsg } from '@/apis/request';
export default {
    field() {
        return {
            Screen: {
                fps: 0,
                fpsTimes: 0,
                img: null,
                LastInput: 0,
                touch: {
                    //上次位置
                    lastTouch: { x1: 0, y1: 0, x2: 0, y2: 0, dist: 0, },
                    //缩放比例
                    scale: 1,
                    //原点（缩放点，位置不变，四周扩散）
                    origin: { x: 0, y: 0, x1: 0, y1: 0, distX: 0, distY: 0 },
                    updated: false,
                    type: 0
                },
                touchend(event) {
                },
                reset() {
                    this.touch.origin.x = 0;
                    this.touch.origin.y = 0;
                    this.touch.origin.x1 = 0;
                    this.touch.origin.y1 = 0;
                    this.touch.origin.distX = 0;
                    this.touch.origin.distY = 0;

                    this.touch.scale = 1;
                    this.touch.updated = true;
                },
                getScalePosition(event) {
                    const bound = event.srcElement.getBoundingClientRect();
                    const left = bound.left;
                    const top = bound.top;
                    return {
                        x1: (event.touches[0].clientX - left) / event.srcElement.offsetWidth * event.srcElement.width,
                        x2: (event.touches[1].clientX - left) / event.srcElement.offsetWidth * event.srcElement.width,
                        y1: (event.touches[0].clientY - top) / event.srcElement.offsetHeight * event.srcElement.height,
                        y2: (event.touches[1].clientY - top) / event.srcElement.offsetHeight * event.srcElement.height,
                    };
                },
                getPosition(event) {
                    const bound = event.srcElement.getBoundingClientRect();
                    const left = bound.left;
                    const top = bound.top;
                    return {
                        x1: (event.touches[0].clientX - left) / event.srcElement.offsetWidth * event.srcElement.width,
                        y1: (event.touches[0].clientY - top) / event.srcElement.offsetHeight * event.srcElement.height,
                    };
                },
                touchstart(event) {
                    if (event.touches.length == 2) {
                        this.touch.type = 2;
                        const { x1, y1, x2, y2 } = this.getScalePosition(event);
                        this.touch.lastTouch.x1 = x1;
                        this.touch.lastTouch.y1 = y1;
                        this.touch.lastTouch.x2 = x2;
                        this.touch.lastTouch.y2 = y2;

                        const distX = Math.abs(this.touch.lastTouch.x1 - this.touch.lastTouch.x2);
                        const distY = Math.abs(this.touch.lastTouch.y1 - this.touch.lastTouch.y2);
                        const dist = Math.sqrt(distX * distX + distY * distY);
                        this.touch.lastTouch.dist = dist;
                        if (this.touch.origin.x == 0) {
                            this.touch.origin.x = parseInt((this.touch.lastTouch.x1 + this.touch.lastTouch.x2) / 2);
                            this.touch.origin.y = parseInt((this.touch.lastTouch.y1 + this.touch.lastTouch.y2) / 2);
                        };

                    } else if (event.touches.length == 1) {
                        this.touch.type = 1;
                        const { x1, y1 } = this.getPosition(event);
                        this.touch.lastTouch.x1 = x1;
                        this.touch.lastTouch.y1 = y1;
                    }
                },
                touchmove(event) {
                    if (event.touches.length == 2) {
                        if (this.touch.type != 2) return;
                        const { x1, y1, x2, y2 } = this.getScalePosition(event);

                        const distX = Math.abs(x1 - x2);
                        const distY = Math.abs(y1 - y2);
                        const dist = Math.sqrt(distX * distX + distY * distY);

                        this.touch.scale += parseInt((dist - this.touch.lastTouch.dist) / 1000 * 100) / 100;
                        if (this.touch.scale <= 1) this.touch.scale = 1;

                        this.touch.lastTouch.dist = dist;

                        this.touch.lastTouch.x1 = x1;
                        this.touch.lastTouch.y1 = y1;
                        this.touch.lastTouch.x2 = x2;
                        this.touch.lastTouch.y2 = y2;

                        this.touch.updated = true;
                    } else if (event.touches.length == 1) {
                        if (this.touch.type != 1) return;
                        const { x1, y1 } = this.getPosition(event);

                        const distX = x1 - this.touch.lastTouch.x1;
                        const distY = y1 - this.touch.lastTouch.y1;
                        this.touch.origin.distX = distX;
                        this.touch.origin.distY = distY;

                        this.touch.origin.x1 -= distX;
                        if (this.touch.origin.x1 <= 0) this.touch.origin.x1 = 0;
                        else if (this.touch.origin.x1 >= event.srcElement.width) this.touch.origin.x1 = event.srcElement.width;

                        this.touch.origin.x = parseInt(this.touch.origin.x1);

                        this.touch.origin.y1 -= distY;
                        if (this.touch.origin.y1 <= 0) this.touch.origin.y1 = 0;
                        else if (this.touch.origin.y1 >= event.srcElement.height) this.touch.origin.y1 = event.srcElement.height;

                        this.touch.origin.y = parseInt(this.touch.origin.y1);

                        this.touch.lastTouch.x1 = x1;
                        this.touch.lastTouch.y1 = y1;

                        this.touch.updated = true;
                    }
                },
            }
        };
    },
    reportTimer: 0,
    globalData: null,
    reported: true,
    init() {
        this.globalData = injectGlobalData();
        this.reportInterval();
        this.subMessage();
        this.fpsInterval();
        this.clipInterver();
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
                item.ctx.clearRect(0, 0, item.canvas.width, item.canvas.height);
                if (img) {
                    item.ctx.drawImage(img, 0, 0, img.width, img.height, 0, 0, item.canvas.width, item.canvas.height);
                }
                this.drawFps(item);

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
    drawFps(item) {
        item.ctx.lineWidth = 5;
        item.ctx.font = 'bold 60px Arial';
        item.ctx.fillStyle = 'rgba(0,0,0,0.5)';
        item.ctx.fillText(`FPS : ${item.Screen.fps} 、LT : ${item.Screen.LastInput}ms`, 50, 70);
        item.ctx.lineWidth = 2;
        item.ctx.strokeStyle = '#fff';
        item.ctx.strokeText(`FPS : ${item.Screen.fps} 、LT : ${item.Screen.LastInput}ms`, 50, 70);

        /*
        item.ctx.fillStyle = 'red';
        item.ctx.strokeStyle = 'yellow';
        item.ctx.rect(item.Screen.touch.origin.x, item.Screen.touch.origin.y, 100, 100);
        item.ctx.fill();

        item.ctx.lineWidth = 5;
        item.ctx.strokeStyle = 'yellow';
        item.ctx.moveTo(item.Screen.touch.lastTouch.x1, item.Screen.touch.lastTouch.y1);
        item.ctx.lineTo(item.Screen.touch.lastTouch.x2, item.Screen.touch.lastTouch.y2);
        item.ctx.stroke();
        */
    },

    subMessage() {
        subNotifyMsg('/notify/report/screen', (res, param) => {
            const name = res.Name;
            if (this.globalData.value.reportNames.indexOf(name) == -1) return;
            let item = this.globalData.value.devices.filter(c => c.MachineName == name)[0];
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

    clipTimer: 0,
    clipInterver() {

        this.globalData.value.devices.forEach(item => {
            if (item.Screen.touch.updated) {
                screenClip(item.MachineName, item.Screen.touch.origin.x, item.Screen.touch.origin.y, item.Screen.touch.scale).then(() => {
                    item.Screen.touch.updated = false;
                }).catch(() => {
                    item.Screen.touch.updated = false;
                });
            }
        });
        this.clipTimer = setTimeout(() => {
            this.clipInterver();
        }, 16);
    },

    reported: true,
    reportInterval() {
        if (this.reported) {
            this.reported = false;
            screenUpdate(this.globalData.value.reportNames).then(() => {
                this.reported = true;
                this.reportTimer = setTimeout(() => {
                    this.reportInterval();
                }, 300);
            }).catch(() => {
                this.reported = true;
                this.reportTimer = setTimeout(() => {
                    this.reportInterval();
                }, 300);
            });
        } else {
            this.reportTimer = setTimeout(() => {
                this.reportInterval();
            }, 300);
        }
    },

    update(item, report) {
        if (!report.Screen) return;

        item.Screen.LastInput = report.Screen.LT || 0;

    }
}