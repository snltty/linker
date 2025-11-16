import { getUpdater, subscribeUpdater } from "@/apis/updater";
import { injectGlobalData } from "@/provide";
import { inject, provide, ref } from "vue";

const updaterSymbol = Symbol();
export const provideUpdater = () => {
    const updater = ref({
        timer: 0,
        list: {},
        hashcode: 0,
        subscribeTimer: 0,

        device: {},
        show: false,
    });
    provide(updaterSymbol, updater);

    const updaterDataFn = () => {
        return new Promise((resolve, reject) => {
            getUpdater(updater.value.hashcode.toString()).then((res) => {
                updater.value.hashcode = res.HashCode;
                if (res.List) {
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
        Object.assign(json,{
            hook_updater: updater.value.list[device.MachineId] || {}
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