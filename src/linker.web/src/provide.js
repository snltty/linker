import { subWebsocketState } from "@/apis/request";
import { computed, inject, provide, ref } from "vue";

const globalDataSymbol = Symbol();

export const provideGlobalData = () => {
    const globalData = ref({
        //已连接
        api: { connected: false },
        width: 0,
        height: 0,
        isPhone:computed(()=>globalData.value.width < 800),
        isPc:computed(()=>globalData.value.width >= 800),
        //配置信息
        config: {
            Common: {},
            Client: { Servers: [], Accesss: {},AccessBits:'' },
            Server: {},
            Running: {
                Relay: { Servers: [] },
                Tuntap: { IP: '', PrefixLength: 24 },
                Client: { Servers: [] },
                AutoSyncs: {}
            },
            configed: false
        },
        //登录信息
        signin: { Connected: false, Version: 'v1.0.0.0' },
        bufferSize: ['1KB', '2KB', '4KB', '8KB', '16KB', '32KB', '64KB', '128KB', '256KB', '512KB', '1024KB'],
        self: {}, //本机
        hasAccess(name) {
            if(this.config.Client.FullAccess) return true;
            if (this.config.Client.Accesss[name] == undefined) return false;
            return this.config.Client.AccessBits[this.config.Client.Accesss[name].Value] == '1';
        }
    });
    subWebsocketState((state) => {
        globalData.value.api.connected = state;
    });

    provide(globalDataSymbol, globalData);
    return globalData;
}
export const injectGlobalData = () => {
    return inject(globalDataSymbol);
}