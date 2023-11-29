import { injectGlobalData } from '@/views/provide';
import { screenClip, screenUpdateFull, screenUpdateRegion } from '../../../../apis/screen'
import { subNotifyMsg } from '@/apis/request';
export default {
    field() {
        return {
            Screen: {
                displays: [],

                regionImgs: [], //局部图
                fullImg: null, //全图
                fullUpdated: false, //第一次进来先获取一次全图
                width: 0, height: 0, //系统宽高
                prevCanvas: null,
                prevCtx: null,

                draw(canvas, ctx) {
                    this.drawFps(canvas, ctx);
                    this.drawRectangle(canvas, ctx);
                    this.drawTouch(canvas, ctx);
                },

                lastInput: 0, //最后活动时间 ms
                captureTime: 0, //截图花费时间 ms
                fps: { value: 0, temp: 0 }, //帧数累计
                drawFps(canvas, ctx) {
                    ctx.lineWidth = 5;
                    ctx.font = 'bold 60px Arial';
                    ctx.fillStyle = 'rgba(0,0,0,0.5)';
                    ctx.fillText(`FPS : ${this.fps.value} 、${this.captureTime}ms、LT : ${this.lastInput}ms`, 50, 70);
                    ctx.lineWidth = 2;
                    ctx.strokeStyle = '#fff';
                    ctx.strokeText(`FPS : ${this.fps.value} 、${this.captureTime}ms 、LT : ${this.lastInput}ms`, 50, 70);
                },

                rectangles: [],
                drawRectangle(canvas, ctx) {
                    const rectangles = this.rectangles || [];
                    if (rectangles.length > 0 && this.touch.scale == 1) {
                        ctx.lineWidth = 5;
                        ctx.strokeStyle = 'rgba(255,0,0,1)';
                        for (let i = 0; i < rectangles.length; i++) {

                            const rectangle = rectangles[i];
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
                    clip: { x: 0, y: 0, w: 0, h: 0 },
                    //原点（缩放点，位置不变，四周扩散）
                    origin: { x: 0, y: 0, x1: 0, y1: 0 },
                    updated: false,
                    //缩放比例
                    scale: 1,
                    type: 0
                },
                drawTouch(canvas, ctx) {
                    if (this.touch.type == 2) {

                        ctx.fillStyle = 'yellow';
                        ctx.strokeStyle = 'yellow';
                        ctx.arc(this.touch.origin.x, this.touch.origin.y, 50, 0, Math.PI * 2);
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

                    this.touch.scale = 1;

                    this.touch.clip.x = 0;
                    this.touch.clip.y = 0;
                    this.touch.clip.w = 0;
                    this.touch.clip.h = 0;

                    this.touch.type = 0;

                    this.touch.updated = true;
                    this.fullUpdated = false;
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
                getDist(x1, y1, x2, y2) {
                    const distX = Math.abs(x1 - x2);
                    const distY = Math.abs(y1 - y2);
                    return Math.sqrt(distX * distX + distY * distY);
                },
                transPosition() {
                    this.touch.origin.x1 = this.touch.clip.x + parseInt(this.touch.origin.x / this.width * (this.touch.clip.w || this.width));
                    this.touch.origin.y1 = this.touch.clip.y + parseInt(this.touch.origin.y / this.height * (this.touch.clip.h || this.height));
                },
                touchstart(event) {
                    if (event.touches.length == 2) {
                        this.touch.type = 2;

                        const { x1, y1, x2, y2 } = this.getScalePosition(event);
                        const dist = this.getDist(x1, y1, x2, y2);

                        this.touch.origin.x = parseInt((x1 + x2) / 2);
                        this.touch.origin.y = parseInt((y1 + y2) / 2);
                        this.transPosition();

                        this.touch.lastTouch.x1 = x1;
                        this.touch.lastTouch.y1 = y1;
                        this.touch.lastTouch.x2 = x2;
                        this.touch.lastTouch.y2 = y2;
                        this.touch.lastTouch.dist = dist;

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
                        const dist = this.getDist(x1, y1, x2, y2);

                        this.touch.scale += (dist - this.touch.lastTouch.dist) / 500;
                        this.touch.scale = Math.max(this.touch.scale, 1);

                        this.touch.lastTouch.x1 = x1;
                        this.touch.lastTouch.y1 = y1;
                        this.touch.lastTouch.x2 = x2;
                        this.touch.lastTouch.y2 = y2;
                        this.touch.lastTouch.dist = dist;

                        this.calcClip();
                        this.touch.updated = true;

                    } else if (event.touches.length == 1) {
                        if (this.touch.type != 1 || this.touch.scale == 1) {
                            return;
                        }

                        const { x1, y1 } = this.getPosition(event);

                        this.touch.clip.x -= (x1 - this.touch.lastTouch.x1) / this.touch.scale;
                        this.touch.clip.y -= (y1 - this.touch.lastTouch.y1) / this.touch.scale;

                        this.touch.lastTouch.x1 = x1;
                        this.touch.lastTouch.y1 = y1;

                        this.touch.updated = true;
                    }
                },
                calcClip() {
                    const width = this.width, height = this.height, scale = this.touch.scale, origin = this.touch.origin;
                    if (width == 0 || height == 0) {
                        return;
                    }

                    const clipWidth = (width * scale - width) / scale;
                    const clipHeight = (height * scale - height) / scale;

                    const x = clipWidth * (origin.x1 / width);
                    const y = clipHeight * (origin.y1 / height);

                    this.touch.clip.x = x;
                    this.touch.clip.y = y;
                    this.touch.clip.w = width - clipWidth;
                    this.touch.clip.h = height - clipHeight;
                }

            }
        };
    },

    globalData: null,
    init() {
        this.globalData = injectGlobalData();
        this.subMessage();
        this.reportInterval();
        this.fpsInterval();
        this.clipInterver();
        this.draw();
    },
    uninit() {
        clearTimeout(this.reportTimer);
        clearTimeout(this.clipTimer);
    },

    imgOnload(url, param) {
        return new Promise((resolve, reject) => {
            const img = new Image();
            img.param = param;
            img.src = url;
            img.onload = function () {
                resolve(img);
            };
        });
    },
    handleScreenFull(res, param) {
        const name = res.Name;
        if (this.globalData.value.reportNames.indexOf(name) == -1) return;
        let item = this.globalData.value.devices.filter(c => c.MachineName == name)[0];
        if (item) {
            item.lastUpdated = Date.now();
            item.Screen.fps.temp++;
            if (typeof res.Img == 'string') {
                this.imgOnload(`data:image/jpg;base64,${res.Img}`).then((img) => {
                    item.Screen.fullImg = img;
                });
            } else {
                createImageBitmap(res.Img).then((img) => {
                    item.Screen.fullImg = img;
                });
            }
        }
    },
    handleScreenRegion(res, param) {
        const name = res.Name;
        if (this.globalData.value.reportNames.indexOf(name) == -1) return;
        let item = this.globalData.value.devices.filter(c => c.MachineName == name)[0];
        if (item) {
            item.lastUpdated = Date.now();
            item.Screen.fps.temp++;
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
                    images.push(createImageBitmap(res.Img.slice(index, index + length), { x: x, y: y, w: w, h: h }));
                    index += length;
                }

                Promise.all(images).then((images) => {
                    item.Screen.regionImgs = images;
                });
            });
        }
    },
    handleScreenRectangles(res, param) {
        const name = res.Name;
        if (this.globalData.value.reportNames.indexOf(name) == -1) return;
        let item = this.globalData.value.devices.filter(c => c.MachineName == name)[0];
        if (item) {
            item.lastUpdated = Date.now();
            item.Screen.rectangles = res.Rectangles || [];
        }
    },
    subMessage() {
        subNotifyMsg('/notify/report/screen/full', (res, param) => this.handleScreenFull(res, param));
        subNotifyMsg('/notify/report/screen/region', (res, param) => this.handleScreenRegion(res, param));
        subNotifyMsg('/notify/report/screen/rectangles', (res, param) => this.handleScreenRectangles(res, param));
    },


    getCtx(item) {
        if (!item.canvas) {
            item.canvas = document.getElementById(`canvas-${item.MachineName}`);
            if (item.canvas) {
                try {
                    item.ctx = item.canvas.getContext('2d')
                } catch (e) {
                    item.canvas = null;
                }
                if (!item.infoCanvas) {
                    item.infoCanvas = document.createElement('canvas');
                    item.infoCanvas.width = item.canvas.width;
                    item.infoCanvas.height = item.canvas.height;
                    item.infoCtx = item.infoCanvas.getContext('2d');
                }
            }
        }
        if (!this.prevCanvas || !this.prevCtx) {
            this.prevCanvas = document.getElementById(`prev-canvas`);
            if (this.prevCanvas) {
                this.prevCtx = this.prevCanvas.getContext('2d')
            }
        }
    },
    drawRegionImgs(item) {
        const regions = item.Screen.regionImgs;
        for (let i = 0; i < regions.length; i++) {
            const { x, y, w, h } = regions[i].param;
            item.infoCtx.drawImage(regions[i], 0, 0, regions[i].width, regions[i].height, x, y, w, h);
        }
    },
    drawInfo(item) {
        this.drawRegionImgs(item);
        for (let j in item) {
            try {
                if (item[j] && item[j].draw) {
                    item[j].draw(item.infoCanvas, item.infoCtx);
                }
            } catch (e) {
                console.log(e);
            }
        }
    },
    draw() {
        const devices = this.globalData.value.devices.filter(c => this.globalData.value.reportNames.indexOf(c.MachineName) >= 0);
        const current = this.globalData.value.currentDevice;

        for (let i = 0; i < devices.length; i++) {
            const item = devices[i];

            this.getCtx(item);
            if (!item.canvas) {
                continue;
            }

            if (item.lastUpdated == item.lastUpdatedOld) {
                continue;
            }
            item.lastUpdatedOld = item.lastUpdated;

            const img = item.Screen.fullImg;
            if (img) {
                item.ctx.drawImage(img, 0, 0, img.width, img.height, 0, 0, item.canvas.width, item.canvas.height);
                if (this.prevCtx) {
                    if (current && current.MachineName == item.MachineName) {
                        this.prevCtx.drawImage(img, 0, 0, img.width, img.height, 0, 0, this.prevCanvas.width, this.prevCanvas.height);
                    }
                }
            }
            item.infoCtx.clearRect(0, 0, item.infoCanvas.width, item.infoCanvas.height);
            this.drawInfo(item);
            item.ctx.drawImage(item.infoCanvas, 0, 0, item.infoCanvas.width, item.infoCanvas.height, 0, 0, item.canvas.width, item.canvas.height);
        }
        requestAnimationFrame(() => {
            this.draw();
        });
    },

    fpsInterval() {
        this.globalData.value.devices.forEach(item => {
            item.Screen.fps.value = item.Screen.fps.temp;
            item.lastUpdated = Date.now();
            item.Screen.fps.temp = 0;
        });
        setTimeout(() => {
            this.fpsInterval();
        }, 1000)
    },

    clipTimer: 0,
    clipInterver() {
        this.globalData.value.devices.forEach(item => {
            if (item.Screen.touch.updated) {
                screenClip(item.MachineName, {
                    x: parseInt(item.Screen.touch.clip.x),
                    y: parseInt(item.Screen.touch.clip.y),
                    w: parseInt(item.Screen.touch.clip.w),
                    h: parseInt(item.Screen.touch.clip.h),
                }).then(() => {
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
        const devices = this.globalData.value.devices;
        let reportType = 2;
        devices.filter(c => names.indexOf(c.MachineName) >= 0).forEach(item => {
            reportType &&= (Number(item.Screen.fullUpdated) + 1);
            item.Screen.fullUpdated = true;
        });
        return screenUpdateFull(names, reportType);
    },
    updateRegion() {
        const names = this.globalData.value.reportNames;
        return screenUpdateRegion(names);
    },
    reportInterval(times = 0) {
        if (this.reported) {
            this.reported = false;
            const fn = this.updateFull();
            fn.then(() => {
                this.reported = true;
                this.reportTimer = setTimeout(() => {
                    this.reportInterval(++times);
                }, 300);
            }).catch((e) => {
                console.log(e);
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

        item.lastUpdated = Date.now();
        item.Screen.lastInput = report.Screen.LT || 0;
        item.Screen.captureTime = report.Screen.CT || 0;
        item.Screen.width = report.Screen.W || 0;
        item.Screen.height = report.Screen.H || 0;
        //item.Screen.displays = report.Screen.Displays || [];

        //console.log(item.Screen.displays);
    }
}