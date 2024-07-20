import { getUpdater } from "@/apis/updater";
import { injectGlobalData } from "@/provide";
import { inject, provide, ref } from "vue";

const updaterSymbol = Symbol();
export const provideUpdater = () => {
    const globalData = injectGlobalData();
    const updater = ref({
        timer: 0,
        list: {},
        current: { Version: '', Msg: [], DateTime: '', Status: 0, Length: 0, Current: 0 }
    });
    provide(updaterSymbol, updater);
    const _getUpdater = () => {
        if (globalData.value.api.connected) {
            getUpdater().then((res) => {
                console.log(res);
                const self = Object.values(res).filter(c => !!c.Version)[0];
                if (self) {
                    updater.value.current.DateTime = self.DateTime;
                    updater.value.current.Version = self.Version;
                    updater.value.current.Status = self.Status;
                    updater.value.current.Length = self.Length;
                    updater.value.current.Current = self.Current;
                    updater.value.current.Msg = self.Msg;
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