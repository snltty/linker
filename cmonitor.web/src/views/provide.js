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
        reportNames: [],
        pc: false,//window.innerWidth > 768
    });
    window.addEventListener('resize', () => {
        globalData.value.pc = false;// window.innerWidth > 768;
    });

    provide(globalDataSymbol, globalData);
    return globalData;
}
export const injectGlobalData = () => {
    return inject(globalDataSymbol);
}