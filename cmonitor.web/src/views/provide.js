import { subWebsocketState } from "@/apis/request";
import { computed, inject, provide, ref } from "vue";

const globalDataSymbol = Symbol();

export const provideGlobalData = () => {
    const globalData = ref({
        //当前登录用户
        username: '',
        //默认用户  
        publicUserName: 'snltty',
        //用户配置
        usernames: {},
        //已连接
        connected: false,
        //需要更新规则 用 watch
        updateRuleFlag: 0,
        //需要更新设备 用 watch
        updateDeviceFlag: 0,
        //所有设备
        allDevices: [],
        //当前用户设备
        devices: computed(() => {
            const user = globalData.value.usernames[globalData.value.username];
            if (user) {
                return globalData.value.allDevices.filter(c => user.Devices.indexOf(c.MachineName) >= 0);
            }
            return [];
        }),
        //当前设备
        currentDevice: { MachineName: '' },
        //需要报告数据的设备
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