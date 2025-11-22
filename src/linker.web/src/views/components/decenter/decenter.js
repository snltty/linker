import {getCounterInfo,refreshCounter } from "@/apis/decenter";
import { inject, provide, ref } from "vue";

const decenterSymbol = Symbol();
export const provideDecenter = () => {
    const decenter = ref({
        list: null,
        hashcode: 0
    });
    provide(decenterSymbol, decenter);

    const counterDataFn = () => {
        return new Promise((resolve, reject) => { 
            getCounterInfo(decenter.value.hashcode.toString()).then((res) => {
                decenter.value.hashcode = res.HashCode;
                if (res.List) {
                    decenter.value.list = res.List;
                    resolve(true);
                    return;
                }
                resolve(false);
            }).catch(() => {
                resolve(false);
            });
        });
        
    }
    const counterProcessFn = (device,json) => {
        if(!decenter.value.list) return;
        const _json = {};
        for (const key in decenter.value.list) {
            _json[key] = decenter.value.list[key][device.MachineId] || 0;
        }
        Object.assign(json,{
            hook_counter: _json,
            hook_counter_load:true
        });
    }
    const counterRefreshFn = () => {
        refreshCounter();
    }
    return {
        decenter, counterDataFn,counterProcessFn,counterRefreshFn
    }
}
export const useDecenter = () => {
    return inject(decenterSymbol);
}