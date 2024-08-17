import { getForwardInfo, testTargetForwardInfo } from "@/apis/forward";
import { injectGlobalData } from "@/provide";
import { inject, provide, ref } from "vue";

const forwardSymbol = Symbol();
export const provideForward = () => {
    const globalData = injectGlobalData();
    const forward = ref({
        timer: 0,
        showEdit: false,
        showCopy: false,
        current: null,
        list: {},
        testTimer: 0,
        testTargetTimer: 0,
        hashcode: 0,
        hashcode1: 0,
    });
    provide(forwardSymbol, forward);
    const _getForwardInfo = () => {
        if (globalData.value.api.connected) {
            getForwardInfo(forward.value.hashcode.toString()).then((res) => {
                forward.value.hashcode = res.HashCode;
                if (res.List) {
                    forward.value.list = res.List;
                }
                forward.value.timer = setTimeout(_getForwardInfo, 1020);
            }).catch(() => {
                forward.value.timer = setTimeout(_getForwardInfo, 1020);
            });
        } else {
            forward.value.timer = setTimeout(_getForwardInfo, 1020);
        }
    }
    const handleForwardEdit = (machineId, machineName) => {
        forward.value.current = machineId;
        forward.value.machineName = machineName;
        forward.value.showEdit = true;
    }
    const _testTargetForwardInfo = () => {
        clearTimeout(forward.value.testTargetTimer)
        testTargetForwardInfo(forward.value.current).then((res) => {
            forward.value.testTargetTimer = setTimeout(_testTargetForwardInfo, 5000);
        }).catch(() => {
            forward.value.testTargetTimer = setTimeout(_testTargetForwardInfo, 5000);
        });
    }
    const clearForwardTimeout = () => {
        clearTimeout(forward.value.timer);
        clearTimeout(forward.value.testTimer);
        clearTimeout(forward.value.testTargetTimer);
    }

    const getForwardMachines = (name) => {
        return Object.values(forward.value.list).reduce((arr, val) => {
            arr = arr.concat(val);
            return arr;
        }, [])
            .filter(c => (c.Name || '').indexOf(name) >= 0 || (c.BindIPAddress || '').indexOf(name) >= 0 || (c.Port.toString()).indexOf(name) >= 0 || (c.TargetEP || '').indexOf(name) >= 0)
            .map(c => c.MachineId);
    }

    return {
        forward, _getForwardInfo, handleForwardEdit, _testTargetForwardInfo, clearForwardTimeout, getForwardMachines
    }
}
export const useForward = () => {
    return inject(forwardSymbol);
}