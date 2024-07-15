import { subWebsocketState } from "@/apis/request";
import { computed, inject, provide, ref } from "vue";

const globalDataSymbol = Symbol();

export const provideGlobalData = () => {
    const globalData = ref({
        //已连接
        connected: false,
        updateFlag: false,
        height: 0,
        config: { Common: {}, Client: {}, Server: {}, Running: {} },
        configed: false,
        signin: { Connected: false, Connecting: false, Version: 'v1.0.0.0' },
        bufferSize: ['1KB', '2KB', '4KB', '8KB', '16KB', '32KB', '64KB', '128KB', '256KB', '512KB', '1024KB'],
        updater: { Msg: '', Version: '' }
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