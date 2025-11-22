import { getUpdater, subscribeUpdater } from "@/apis/updater";
import { injectGlobalData } from "@/provide";
import { inject, provide, ref } from "vue";

const updaterSymbol = Symbol();
export const provideUpdater = () => {
    const globalData = injectGlobalData();
    const updater = ref({
        timer: 0,
        list: null,
        hashcode: 0,
        subscribeTimer: 0,

        current: { Version: '', Msg: [], DateTime: '', Status: 0, Length: 0, Current: 0 },

        device: {},
        show: false,
    });
    provide(updaterSymbol, updater);

    const updaterDataFn = () => {
        return new Promise((resolve, reject) => {
            getUpdater(updater.value.hashcode.toString()).then((res) => {
                updater.value.hashcode = res.HashCode;
                if (res.List) {
                    const self = Object.values(res.List).filter(c => !!c.Version)[0];
                    if (self) {
                        Object.assign(updater.value.current, {
                            Version: self.Version,
                            Status: self.Status,
                            Length: self.Length,
                            Current: self.Current
                        });
                        globalData.value.updater = updater.value.current;
                    }
                    updater.value.list = res.List;
                    resolve(true);
                    return;
                }
                resolve(false);
            }).catch(() => {
                resolve(false);
            });
        });
    }
    const updaterRefreshFn = () => {
    }
    const updaterProcessFn = (device,json) => { 
        if(!updater.value.list) return;
        Object.assign(json,{
            hook_updater: updater.value.list[device.MachineId] || {},
            hook_updater_load: true
        });
    }

    const updaterSubscribe = () => {
        subscribeUpdater().then(() => {
            updater.value.subscribeTimer = setTimeout(updaterSubscribe, 5000);
        }).catch(() => {
            updater.value.subscribeTimer = setTimeout(updaterSubscribe, 5000);
        });
    }

    const updaterClearTimeout = () => {
        clearTimeout(updater.value.subscribeTimer);
    }


    return {
        updater, updaterDataFn, updaterProcessFn,updaterRefreshFn,updaterSubscribe, updaterClearTimeout
    }
}
export const useUpdater = () => {
    return inject(updaterSymbol);
}