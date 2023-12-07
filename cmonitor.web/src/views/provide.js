import { subWebsocketState } from "@/apis/request";
import { computed, inject, provide, ref } from "vue";

const globalDataSymbol = Symbol();

export const provideGlobalData = () => {
    const globalData = ref({
        username: '',
        publicUserName: 'snltty',
        usernames: {},
        connected: false,
        updateRuleFlag: 0,
        updateDeviceFlag: 0,
        allDevices: [],
        devices: computed(() => {
            const user = globalData.value.usernames[globalData.value.username];
            if (user) {
                return globalData.value.allDevices.filter(c => user.Devices.indexOf(c.MachineName) >= 0);
            }
            return [];
        }),
        currentDevice: { MachineName: '' },
        reportNames: []
    });
    subWebsocketState((state) => {
        globalData.value.connected = state;
    })

    provide(globalDataSymbol, globalData);
    return globalData;
}
export const injectGlobalData = () => {
    return inject(globalDataSymbol);
}