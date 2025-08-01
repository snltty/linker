
import { getSForwardCountInfo, refreshSForward, testLocalSForwardInfo } from '@/apis/sforward';
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
        list: {},
        testTimer: 0,
        hashcode: 0,
        machineid: '',
        machineName: '',
    });
    provide(sforwardSymbol, sforward);

    const handleSForwardRefresh = () => {
        refreshSForward();
    }
    const _getSForwardCountInfo = () => {
        clearTimeout(sforward.value.timer);
        getSForwardCountInfo(sforward.value.hashcode.toString()).then((res) => {
            sforward.value.hashcode = res.HashCode;
            if (res.List) {
                sforward.value.list = res.List;
            }
            sforward.value.timer = setTimeout(_getSForwardCountInfo, 1020);
        }).catch(() => {
            sforward.value.timer = setTimeout(_getSForwardCountInfo, 1020);
        });
    }
    const handleSForwardEdit = (machineid) => {
        sforward.value.machineid = machineid[0];
        sforward.value.machineName = machineid[1];
        sforward.value.showEdit = true;
    }
    const clearSForwardTimeout = () => {
        clearTimeout(sforward.value.timer);
        clearTimeout(sforward.value.testTimer);
    }
    return {
        sforward, _getSForwardCountInfo, handleSForwardEdit, clearSForwardTimeout, handleSForwardRefresh
    }

}
export const useSforward = () => {
    return inject(sforwardSymbol);
}