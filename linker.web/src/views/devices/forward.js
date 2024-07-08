import { getForwardInfo, testListenForwardInfo, testTargetForwardInfo } from "@/apis/forward";
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
    });
    provide(forwardSymbol, forward);
    const _getForwardInfo = () => {
        if (globalData.value.connected) {
            getForwardInfo().then((res) => {
                forward.value.list = res;
                forward.value.timer = setTimeout(_getForwardInfo, 1000);
            }).catch(() => {
                forward.value.timer = setTimeout(_getForwardInfo, 1000);
            });
        } else {
            forward.value.timer = setTimeout(_getForwardInfo, 1000);
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
    const _testListenForwardInfo = () => {
        clearTimeout(forward.value.testTimer)
        testListenForwardInfo(forward.value.current).then((res) => {
            forward.value.testTimer = setTimeout(_testListenForwardInfo, 5000);
        }).catch(() => {
            forward.value.testTimer = setTimeout(_testListenForwardInfo, 5000);
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
        forward, _getForwardInfo, handleForwardEdit, _testTargetForwardInfo, _testListenForwardInfo, clearForwardTimeout, getForwardMachines
    }
}
export const useForward = () => {
    return inject(forwardSymbol);
}