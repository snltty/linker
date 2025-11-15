import {getCounterInfo,refreshCounter } from "@/apis/decenter";
import { inject, provide, ref } from "vue";

const decenterSymbol = Symbol();
export const provideDecenter = () => {
    const decenter = ref({
        list: {},
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
    const counterProcessFn = (device) => {
        Object.assign(device,{
            hook_counter: decenter.value.list[device.MachineId] || ''
        })
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