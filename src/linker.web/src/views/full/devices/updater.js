import { getUpdater, subscribeUpdater } from "@/apis/updater";
import { injectGlobalData } from "@/provide";
import { inject, provide, ref } from "vue";

const updaterSymbol = Symbol();
export const provideUpdater = () => {
    const globalData = injectGlobalData();
    const updater = ref({
        timer: 0,
        list: {},
        hashcode: 0,
        current: { Version: '', Msg: [], DateTime: '', Status: 0, Length: 0, Current: 0 },

        subscribeTimer: 0
    });
    provide(updaterSymbol, updater);
    const _getUpdater = () => {
        getUpdater(updater.value.hashcode.toString()).then((res) => {
            updater.value.hashcode = res.HashCode;
            if (res.List) {
                const self = Object.values(res.List).filter(c => !!c.Version)[0];
                if (self) {
                    Object.assign(updater.value.current, {
                        DateTime: self.DateTime,
                        Version: self.Version,
                        Status: self.Status,
                        Length: self.Length,
                        Current: self.Current,
                        Msg: self.Msg,
                    });
                    globalData.value.updater = updater.value.current;
                }
                updater.value.list = res.List;
            }

            updater.value.timer = setTimeout(_getUpdater, 800);
        }).catch(() => {
            updater.value.timer = setTimeout(_getUpdater, 800);
        });
    }
    const _subscribeUpdater = () => {
        subscribeUpdater().then(() => {
            updater.value.subscribeTimer = setTimeout(_subscribeUpdater, 5000);
        }).catch(() => {
            updater.value.subscribeTimer = setTimeout(_subscribeUpdater, 5000);
        });
    }


    const clearUpdaterTimeout = () => {
        clearTimeout(updater.value.timer);
        clearTimeout(updater.value.subscribeTimer);
    }


    return {
        updater, _getUpdater, _subscribeUpdater, clearUpdaterTimeout
    }
}
export const useUpdater = () => {
    return inject(updaterSymbol);
}