import { injectGlobalData } from "@/views/provide";

export default {
    field() {
        return {
            Light: {
                Value: 0
            },
        }
    },
    state: {
        light: {
            showLight: false,
            showLightSingle: false,
            items: []
        }
    },
    globalData: null,
    init() {
        this.globalData = injectGlobalData();
    },
    update(item, report) {
        if (report.Light) {
            item.Light.Value = Math.floor(+report.Light.Value);
            if (isNaN(item.Light.Value)) item.Light.Value = 0;
        }
    }
}