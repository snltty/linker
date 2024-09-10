import { subWebsocketState } from "@/apis/request";
import { inject, provide, ref } from "vue";

const globalDataSymbol = Symbol();

export const provideGlobalData = () => {
    const globalData = ref({
        //已连接
        api: { connected: false },
        height: 0,
        //配置信息
        config: {
            Common: {},
            Client: { Servers: [], Accesss: {} },
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
        signin: { Connected: false, Connecting: false, Version: 'v1.0.0.0' },
        bufferSize: ['1KB', '2KB', '4KB', '8KB', '16KB', '32KB', '64KB', '128KB', '256KB', '512KB', '1024KB'],
        updater: {}, //更新信息
        self: {}, //本机
        hasAccess(name) {
            if (this.config.Client.Accesss[name] == undefined) {
                return false;
            }
            const value = this.config.Client.Accesss[name].Value || -1;
            const access = this.config.Client.Access || -1;
            return access >= 0 && (access == 0 || ((access & value) >>> 0) == value);
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