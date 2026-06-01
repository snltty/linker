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
                    const json = {};
                    for (const key in res.List) {
                        const machines = res.List[key];
                        for (const machineId in machines) {
                            json[machineId] = json[machineId] || {};
                            json[machineId][key] = machines[machineId];
                        }
                    }
                    decenter.value.list = json;
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
        const _decenter = decenter.value.list[device.MachineId];
        const decenter_sort = _decenter || {forward:0,reverse:0,firewall:0,transport:0,wakeup:0}
        Object.assign(json,{
            hook_counter:_decenter,
            hook_counter_forward_sort:`${decenter_sort.forward}-${decenter_sort.reverse}`,
            hook_counter_oper_sort:`${decenter_sort.firewall}-${decenter_sort.transport}-${decenter_sort.wakeup}`,
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