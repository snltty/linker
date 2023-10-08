import { subNotifyMsg } from '@/apis/request';
import { reportPing, reportUpdate } from '../../../../apis/report'
import { injectGlobalData } from '@/views/provide';
export default {
    field() {
        return {
            Report: {
                fps: 0,
                fpsTimes: 0,
                ping: 0
            },
        }
    },

    globalData: null,
    init() {
        this.globalData = injectGlobalData();
        this.reportInterval();
        this.reportPingInterval();
        subNotifyMsg('/notify/report/pong', (res) => {
            let item = this.globalData.value.devices.filter(c => c.MachineName == res.Name)[0];
            if (item) {
                item.Connected = true;
                item.Report.ping = res.Time;
            }
        });
        this.fpsInterval();
    },

    reportTimer: 0,
    reported: true,
    reportInterval() {
        if (this.reported) {
            this.reported = false;
            reportUpdate(this.globalData.value.reportNames).then(() => {
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
    reportPingTimer: 0,
    reportedPing: true,
    reportPingInterval() {
        if (this.reportedPing) {
            this.reportedPing = false;
            let start = Date.now();
            reportPing(this.globalData.value.reportNames).then(() => {
                this.reportedPing = true;
                let lastTime = 1000 - (Date.now() - start);
                if (lastTime < 10) {
                    lastTime = 10;
                }
                this.reportPingTimer = setTimeout(() => {
                    this.reportPingInterval();
                }, lastTime);
            }).catch(() => {
                this.reportedPing = true;
                this.reportPingTimer = setTimeout(() => {
                    this.reportPingInterval();
                }, 1000);
            });
        } else {
            this.reportPingTimer = setTimeout(() => {
                this.reportPingInterval();
            }, 1000);
        }
    },

    fpsInterval() {
        this.globalData.value.devices.forEach(item => {
            item.Report.fps = item.Report.fpsTimes;
            item.Report.fpsTimes = 0;
        });
        setTimeout(() => {
            this.fpsInterval();
        }, 1000)
    },
    update(item, report) {
        item.Report.fpsTimes++;
    }
}