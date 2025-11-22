import { getAccesss, refreshAccess } from "@/apis/access";
import { inject, provide, ref } from "vue";

const accessSymbol = Symbol();
export const provideAccess = () => {
    const access = ref({
        list: null,
        timer: 0,
        hashcode: 0
    });
    provide(accessSymbol, access);

    const accessRefreshFn = () => {
        refreshAccess();
    }
    const accessDataFn = () => {
        return new Promise((resolve, reject) => { 
            getAccesss(access.value.hashcode.toString()).then((res) => {
                access.value.hashcode = res.HashCode;
                if (res.List) {
                    access.value.list = res.List;
                    resolve(true);
                    return;
                }
                resolve(false);
            }).catch(() => {
                resolve(false);
            });
        });
    }
    const accessProcessFn = (device,json) => {
        if(!access.value.list) return;
        Object.assign(json,{
            hook_accesss: access.value.list[device.MachineId] || '',
            hook_accesss_load:true
        })
    }

    return {
        access, accessDataFn,accessProcessFn, accessRefreshFn
    }
}
export const useAccess = () => {
    return inject(accessSymbol);
}