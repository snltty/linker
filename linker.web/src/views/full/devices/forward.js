import { getForwardCountInfo, refreshForward, testTargetForwardInfo } from "@/apis/forward";
import { injectGlobalData } from "@/provide";
import { inject, provide, ref } from "vue";

const forwardSymbol = Symbol();
export const provideForward = () => {
    const globalData = injectGlobalData();
    const forward = ref({
        timer: 0,
        showEdit: false,
        machineId: null,
        list: {},
        hashcode: 0
    });
    provide(forwardSymbol, forward);

    const handleForwardRefresh = () => {
        refreshForward();
    }
    const _getForwardCountInfo = () => {
        getForwardCountInfo(forward.value.hashcode.toString()).then((res) => {
            forward.value.hashcode = res.HashCode;
            if (res.List) {
                forward.value.list = res.List;
            }
            forward.value.timer = setTimeout(_getForwardCountInfo, 1020);
        }).catch(() => {
            forward.value.timer = setTimeout(_getForwardCountInfo, 1020);
        });
    }
    const handleForwardEdit = (machineId) => {
        forward.value.machineId = machineId[0];
        forward.value.machineName = machineId[1];
        forward.value.showEdit = true;
    }
    const clearForwardTimeout = () => {
        clearTimeout(forward.value.timer);
    }
    return {
        forward, _getForwardCountInfo, handleForwardEdit, clearForwardTimeout, handleForwardRefresh
    }
}
export const useForward = () => {
    return inject(forwardSymbol);
}