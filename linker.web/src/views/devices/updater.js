import { getUpdater } from "@/apis/updater";
import { injectGlobalData } from "@/provide";
import { inject, provide, ref } from "vue";

const updaterSymbol = Symbol();
export const provideUpdater = () => {
    const globalData = injectGlobalData();
    const updater = ref({
        timer: 0,
        list: {},
        hashcode: 0,
        current: { Version: '', Msg: [], DateTime: '', Status: 0, Length: 0, Current: 0 }
    });
    provide(updaterSymbol, updater);
    const _getUpdater = () => {
        if (globalData.value.api.connected) {
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
                    }
                    updater.value.list = res.List;
                }

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