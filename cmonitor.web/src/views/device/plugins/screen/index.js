import { injectGlobalData } from '@/views/provide';
import { screenClip, screenUpdateFull, screenUpdateRegion } from '../../../../apis/screen'
import { subNotifyMsg } from '@/apis/request';
export default {
    field() {
        return {
            Screen: {
                regions: [],
                img: null,
                fullUpdated: false,

                draw(canvas, ctx) {
                    this.drawFps(canvas, ctx);
                    this.drawRectangle(canvas, ctx);
                    this.drawTouch(canvas, ctx);
                },

                lastInput: 0,
                fps: 0,
                fpsTimes: 0,
                drawFps(canvas, ctx) {
                    ctx.lineWidth = 5;
                    ctx.font = 'bold 60px Arial';
                    ctx.fillStyle = 'rgba(0,0,0,0.5)';
                    ctx.fillText(`FPS : ${this.fps} 、LT : ${this.lastInput}ms`, 50, 70);
                    ctx.lineWidth = 2;
                    ctx.strokeStyle = '#fff';
                    ctx.strokeText(`FPS : ${this.fps} 、LT : ${this.lastInput}ms`, 50, 70);
                },

                rectangles: [],
                drawRectangle(canvas, ctx) {
                    if (this.rectangles.length > 0 && this.touch.scale == 1) {
                        ctx.lineWidth = 5;
                        ctx.strokeStyle = 'rgba(255,0,0,1)';
                        for (let i = 0; i < this.rectangles.length; i++) {

                            const rectangle = this.rectangles[i];
                            ctx.rect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
                            ctx.stroke();

                            ctx.font = 'bold 100px Arial';
                            ctx.fillStyle = 'rgba(255,0,0,1)';
                            ctx.fillText(i, rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2);
                        }
                    }
                },

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
                drawTouch(canvas, ctx) {
                    if (this.touch.type == 2) {
                        ctx.fillStyle = 'red';
                        ctx.strokeStyle = 'yellow';
                        ctx.rect(this.touch.origin.x - 50, this.touch.origin.y - 50, 100, 100);
                        ctx.fill();

                        ctx.lineWidth = 5;
                        ctx.strokeStyle = 'yellow';
                        ctx.moveTo(this.touch.lastTouch.x1, this.touch.lastTouch.y1);
                        ctx.lineTo(this.touch.lastTouch.x2, this.touch.lastTouch.y2);
                        ctx.stroke();
                    }
                },

                touchend(event) {
                    this.touch.type = 0;
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
                            this.touch.origin.x1 = this.touch.origin.x = parseInt((this.touch.lastTouch.x1 + this.touch.lastTouch.x2) / 2);
                            this.touch.origin.y1 = this.touch.origin.y = parseInt((this.touch.lastTouch.y1 + this.touch.lastTouch.y2) / 2);
                        };

                    } else if (event.touches.length == 1) {
                        if (this.touch.scale == 1) return;
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

                        this.touch.scale += parseInt((dist - this.touch.lastTouch.dist) / 500 * 100) / 100;
                        if (this.touch.scale <= 1) this.touch.scale = 1;

                        this.touch.lastTouch.dist = dist;

                        this.touch.lastTouch.x1 = x1;
                        this.touch.lastTouch.y1 = y1;
                        this.touch.lastTouch.x2 = x2;
                        this.touch.lastTouch.y2 = y2;

                        this.touch.updated = true;
                    } else if (event.touches.length == 1) {
                        if (this.touch.type != 1 || this.touch.scale == 1) return;
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

    globalData: null,
    reported: true,
    init() {
        this.globalData = injectGlobalData();
        this.reportInterval(0);
        this.subMessage();
        this.fpsInterval();
        this.clipInterver();
        this.draw();
    },

    subMessage() {
        const imgOnload = (url, param) => {
            return new Promise((resolve, reject) => {
                const img = new Image();
                img.param = param;
                img.src = url;
                img.onload = function () {
                    resolve(img);
                };
            });
        }
        subNotifyMsg('/notify/report/screen/full', (res, param) => {
            const name = res.Name;
            if (this.globalData.value.reportNames.indexOf(name) == -1) return;
            let item = this.globalData.value.devices.filter(c => c.MachineName == name)[0];
            if (item) {
                item.Screen.fpsTimes++;
                if (typeof res.Img == 'string') {
                    imgOnload(`data:image/jpg;base64,${res.Img}`).then((img) => {
                        item.Screen.img = img;
                    });
                } else {
                    imgOnload(URL.createObjectURL(res.Img)).then((img) => {
                        item.Screen.img = img;
                    });
                }
            }
        });

        subNotifyMsg('/notify/report/screen/region', (res, param) => {
            const name = res.Name;
            if (this.globalData.value.reportNames.indexOf(name) == -1) return;
            let item = this.globalData.value.devices.filter(c => c.MachineName == name)[0];
            if (item) {
                item.Screen.fpsTimes++;
                res.Img.arrayBuffer().then((arrayBuffer) => {
                    const dataView = new DataView(arrayBuffer);
                    let index = 0;
                    const images = [];
                    while (index < arrayBuffer.byteLength) {

                        const length = dataView.getUint32(index, true);
                        index += 4;
                        const x = dataView.getUint32(index, true);
                        index += 4;
                        const w = dataView.getUint32(index, true);
                        index += 4;
                        const y = dataView.getUint32(index, true);
                        index += 4;
                        const h = dataView.getUint32(index, true);
                        index += 4;
                        images.push(imgOnload(URL.createObjectURL(res.Img.slice(index, index + length)), { x: x, y: y, w: w, h: h }));
                        index += length;
                    }

                    Promise.all(images).then((images) => {
                        item.Screen.regions = images;
                    });
                });
            }
        });
        subNotifyMsg('/notify/report/screen/rectangles', (res, param) => {
            const name = res.Name;
            if (this.globalData.value.reportNames.indexOf(name) == -1) return;
            let item = this.globalData.value.devices.filter(c => c.MachineName == name)[0];
            if (item) {
                item.Screen.rectangles = res.Rectangles;

            }
        });
    },

    draw() {
        const devices = this.globalData.value.devices.filter(c => this.globalData.value.reportNames.indexOf(c.MachineName) >= 0);

        for (let i = 0; i < devices.length; i++) {
            const item = devices[i];
            if (!item.canvas) {
                item.canvas = document.getElementById(`canvas-${item.MachineName}`);
                if (item.canvas) {
                    try {
                        item.ctx = item.canvas.getContext('2d')
                    } catch (e) {
                        item.canvas = null;
                    }
                }
            }
            if (!item.canvas) continue;

            if (!item.infoCanvas) {
                item.infoCanvas = document.createElement('canvas');
                item.infoCanvas.width = item.canvas.width;
                item.infoCanvas.height = item.canvas.height;
                item.infoCtx = item.infoCanvas.getContext('2d');
            }
            if (item.ctx) {
                item.infoCtx.clearRect(0, 0, item.infoCanvas.width, item.infoCanvas.height);

                const img = item.Screen.img;
                if (img) {
                    //item.Screen.img = null;
                    item.ctx.drawImage(img, 0, 0, img.width, img.height, 0, 0, item.canvas.width, item.canvas.height);
                }

                const regions = item.Screen.regions;
                for (let i = 0; i < regions.length; i++) {
                    const { x, y, w, h } = regions[i].param;
                    item.infoCtx.drawImage(regions[i], 0, 0, regions[i].width, regions[i].height, x, y, w, h);
                }

                for (let j in item) {
                    try {
                        if (item[j] && item[j].draw) {

                            item[j].draw(item.infoCanvas, item.infoCtx);
                        }
                    } catch (e) {
                        console.log(e);
                    }
                }

                item.ctx.drawImage(item.infoCanvas, 0, 0, item.infoCanvas.width, item.infoCanvas.height, 0, 0, item.canvas.width, item.canvas.height);
            }
        }
        requestAnimationFrame(() => {
            this.draw();
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
    reportTimer: 0,
    updateFull() {
        const names = this.globalData.value.reportNames;
        let reportType = 1;
        this.globalData.value.devices.filter(c => names.indexOf(c.MachineName) >= 0).forEach(item => {
            reportType &&= Number(item.Screen.fullUpdated);
            item.Screen.fullUpdated = true;
        });
        return screenUpdateFull(names, reportType);
    },
    updateRegion() {
        const names = this.globalData.value.reportNames;
        return screenUpdateRegion(names);
    },
    reportInterval(times) {
        if (this.reported) {
            this.reported = false;
            // const fn = times < 2 ? this.updateFull() : this.updateRegion();
            const fn = this.updateFull();
            fn.then(() => {
                this.reported = true;
                this.reportTimer = setTimeout(() => {
                    this.reportInterval(++times);
                }, 300);
            }).catch(() => {
                this.reported = true;
                this.reportTimer = setTimeout(() => {
                    this.reportInterval(++times);
                }, 300);
            });
        } else {
            this.reportTimer = setTimeout(() => {
                this.reportInterval(++times);
            }, 300);
        }
    },

    update(item, report) {
        if (!report.Screen) return;

        item.Screen.lastInput = report.Screen.LT || 0;
    }
}