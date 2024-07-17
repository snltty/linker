
import { getSForwardInfo, testLocalSForwardInfo } from '@/apis/sforward';
import { injectGlobalData } from '@/provide';
import { ref, provide, inject, computed } from 'vue';

const sforwardSymbol = Symbol();
export const provideSforward = () => {
    const globalData = injectGlobalData();
    const machineId = computed(() => globalData.value.config.Client.Id);
    const sforward = ref({
        timer: 0,
        showEdit: false,
        showCopy: false,
        list: [],
        testTimer: 0
    });
    provide(sforwardSymbol, sforward);
    const _getSForwardInfo = () => {
        if (globalData.value.api.connected) {
            getSForwardInfo().then((res) => {
                sforward.value.list = res;
                sforward.value.timer = setTimeout(_getSForwardInfo, 1040);
            }).catch(() => {
                sforward.value.timer = setTimeout(_getSForwardInfo, 1040);
            });
        } else {
            sforward.value.timer = setTimeout(_getSForwardInfo, 1040);
        }
    }
    const handleSForwardEdit = () => {
        sforward.value.showEdit = true;
    }
    const _testLocalSForwardInfo = () => {
        clearTimeout(sforward.value.testTimer)
        testLocalSForwardInfo().then((res) => {
            sforward.value.testTimer = setTimeout(_testLocalSForwardInfo, 5000);
        }).catch(() => {
            sforward.value.testTimer = setTimeout(_testLocalSForwardInfo, 5000);
        });
    }
    const clearSForwardTimeout = () => {
        clearTimeout(sforward.value.timer);
        clearTimeout(sforward.value.testTimer);
    }
    const getSForwardMachines = (name) => {
        const sfs = sforward.value.list
            .filter(c => (c.Name || '').indexOf(name) >= 0 || (c.Domain || '').indexOf(name) >= 0 || (c.RemotePort.toString()).indexOf(name) >= 0 || c.LocalEP.indexOf(name) >= 0);

        if (sfs.length > 0) {
            return [machineId.value];
        }
        return [];
    }
    return {
        sforward, _getSForwardInfo, handleSForwardEdit, _testLocalSForwardInfo, clearSForwardTimeout, getSForwardMachines
    }

}
export const useSforward = () => {
    return inject(sforwardSymbol);
}