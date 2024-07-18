import { getUpdater } from "@/apis/updater";
import { injectGlobalData } from "@/provide";
import { inject, provide, ref } from "vue";

const updaterSymbol = Symbol();
export const provideUpdater = () => {
    const globalData = injectGlobalData();
    const updater = ref({
        timer: 0,
        list: {},
        version: ''
    });
    provide(updaterSymbol, updater);
    const _getUpdater = () => {
        if (globalData.value.api.connected) {
            getUpdater().then((res) => {
                const self = Object.values(res).filter(c => !!c.Version)[0];
                if (self) {
                    updater.value.version = self.Version;
                }
                updater.value.list = res;
                updater.value.timer = setTimeout(_getUpdater, 800);
            }).catch(() => {
                updater.value.timer = setTimeout(_getUpdater, 800);
            });
        } else {
            updater.value.timer = setTimeout(_getUpdater, 800);
        }
    }

    const clearUpdaterTimeout = () => {
        clearTimeout(updater.value.timer);
    }


    return {
        updater, _getUpdater, clearUpdaterTimeout
    }
}
export const useUpdater = () => {
    return inject(updaterSymbol);
}