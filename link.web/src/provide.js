import { subWebsocketState } from "@/apis/request";
import { computed, inject, provide, ref } from "vue";

const globalDataSymbol = Symbol();

export const provideGlobalData = () => {
    const globalData = ref({
        //已连接
        connected: false,
        updateFlag: false,
        height: 0,
        config: { Common: {}, Client: {}, Running: {} },
        configed: false,
        signin: { Connected: false, Connecting: false },
        groupid: ''
    });
    subWebsocketState((state) => {
        globalData.value.connected = state;
    });

    provide(globalDataSymbol, globalData);
    return globalData;
}
export const injectGlobalData = () => {
    return inject(globalDataSymbol);
}