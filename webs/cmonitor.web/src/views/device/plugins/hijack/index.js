import { injectGlobalData } from "@/views/provide";
export default {
    pluginName: 'hijack',
    field() {
        return {
            Hijack: {
                Upload: 0,
                UploadText: '',
                Download: 0,
                DownloadText: '',
                Count: 0,
                RuleIds1: [],
                RuleIds2: [],
                DomainKill: false,
            }
        }
    },
    state: {
        hijack: {
            showRules: false,
            devices: [],
            showRuleSetting: false,
            showProcessSetting: false,
        }
    },
    timer: 0,
    speedCaches: {},
    sizeFormat(size) {
        let unites = ['B', 'KB', 'MB', 'GB', 'TB'];
        let unit = unites[0];
        while ((unit = unites.shift()) && size.toFixed(2) >= 1024) {
            size /= 1024;
        }
        return unit == 'B' ? [size, unit] : [parseInt(size), unit];
    },
    globalData: null,
    init() {
        this.globalData = injectGlobalData();;
        const speedCaches = this.speedCaches;
        const sizeFormat = this.sizeFormat;
        this.timer = setInterval(() => {
            this.globalData.value.devices.forEach(item => {

                let cache = speedCaches[item.MachineName] || { up: 0, down: 0 };

                if (isNaN(cache.up)) cache.up = 0;
                if (isNaN(cache.down)) cache.down = 0;

                item.Hijack.Upload = item.Hijack.Upload || 0;
                item.Hijack.Download = item.Hijack.Download || 0;

                let bytes = item.Hijack.Upload - cache.up;
                cache.up = item.Hijack.Upload;
                let format = sizeFormat(bytes);
                item.Hijack.UploadText = `${format[0]}${format[1]}/s`;

                bytes = item.Hijack.Download - cache.down;
                cache.down = item.Hijack.Download;
                format = sizeFormat(bytes);
                item.Hijack.DownloadText = `${format[0]}${format[1]}/s`;

                speedCaches[item.MachineName] = cache;

            });
        }, 1000);
    },
    update(item, report) {

        if (!report.Hijack) return;
        if (report.Hijack.length > 0) {
            item.Hijack.Upload = report.Hijack[0];
            item.Hijack.Download = report.Hijack[1];
            item.Hijack.Count = report.Hijack[2];
        } else {
            item.Hijack.Upload = report.Hijack.Upload;
            item.Hijack.Download = report.Hijack.Download;
            item.Hijack.Count = report.Hijack.Count;
            item.Hijack.RuleIds1 = report.Hijack.Ids1 || [];
            item.Hijack.RuleIds2 = report.Hijack.Ids2 || [];
        }

        item.Hijack.DomainKill = report.Hijack.DomainKill || false;
    }
}