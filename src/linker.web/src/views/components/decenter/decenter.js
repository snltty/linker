import {getCounterInfo,refreshCounter } from "@/apis/decenter";
import { inject, provide, ref } from "vue";

const decenterSymbol = Symbol();
export const provideDecenter = () => {
    const decenter = ref({
        timer: 0,
        list: {},
        hashcode: 0
    });
    provide(decenterSymbol, decenter);

    const _getDecenterCounterInfo = () => {
        clearTimeout(decenter.value.timer);
        getCounterInfo(decenter.value.hashcode.toString()).then((res) => {
            decenter.value.hashcode = res.HashCode;
            if (res.List) {
                decenter.value.list = res.List;
            }
            decenter.value.timer = setTimeout(_getDecenterCounterInfo, 1020);
        }).catch(() => {
            decenter.value.timer = setTimeout(_getDecenterCounterInfo, 1020);
        });
    }
    const handleDecenterRefresh = () => {
        refreshCounter();
    }
    const clearDecenterCounterTimeout = () => {
        clearTimeout(decenter.value.timer);
    }
    return {
        decenter, _getDecenterCounterInfo,handleDecenterRefresh, clearDecenterCounterTimeout
    }
}
export const useDecenter = () => {
    return inject(decenterSymbol);
}