import { injectGlobalData } from "@/provide";
import { ElMessage } from "element-plus";
import { inject, provide, ref } from "vue"
import { getTuntapInfo, refreshTuntap } from "@/apis/tuntap";

const tuntapSymbol = Symbol();
export const provideTuntap = () => {
    const globalData = injectGlobalData();
    const tuntap = ref({
        timer: 0,
        showEdit: false,
        current: null,
        list: {},
        hashcode: 0
    });
    provide(tuntapSymbol, tuntap);

    const systems = {
        linux: ['debian', 'ubuntu', 'rocky', 'centos'],
        windows: ['windows'],
        android: ['android'],
        ios: ['ios'],
    }

    const _getTuntapInfo = () => {
        clearTimeout(tuntap.value.timer);
        if (globalData.value.api.connected) {
            getTuntapInfo(tuntap.value.hashcode.toString()).then((res) => {
                console.log(res);
                tuntap.value.hashcode = res.HashCode;
                if (res.List) {
                    for (let j in res.List) {
                        res.List[j].running = res.List[j].Status == 2;
                        res.List[j].loading = res.List[j].Status == 1;
                        res.List[j].system = 'system';

                        const systemStr = res.List[j].System.toLowerCase();
                        res.List[j].systemDocker = systemStr.indexOf('docker') >= 0;

                        for (let j in systems) {
                            if (systemStr.indexOf(j) >= 0) {
                                const items = systems[j];
                                if (items.length == 1) {
                                    res.List[j].system = items[0];
                                } else {
                                    for (let i = 0; i < items.length; i++) {
                                        if (systemStr.indexOf(items[i]) >= 0) {
                                            res.List[j].system = items[i];
                                            break;
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                    tuntap.value.list = res.List;
                }
                tuntap.value.timer = setTimeout(_getTuntapInfo, 200);
            }).catch(() => {
                tuntap.value.timer = setTimeout(_getTuntapInfo, 200);
            });
        } else {
            tuntap.value.timer = setTimeout(_getTuntapInfo, 1000);
        }
    }
    const handleTuntapEdit = (_tuntap) => {
        tuntap.value.current = _tuntap;
        tuntap.value.showEdit = true;

    }
    const handleTuntapRefresh = () => {
        refreshTuntap();
        ElMessage.success({ message: '刷新成功', grouping: true });
    }
    const clearTuntapTimeout = () => {
        clearTimeout(tuntap.value.timer);
        tuntap.value.timer = 0;
    }
    const getTuntapMachines = (name) => {
        return Object.values(tuntap.value.list)
            .filter(c => c.IP.indexOf(name) >= 0 || (c.LanIPs.filter(d => d.indexOf(name) >= 0).length > 0))
            .map(c => c.MachineId);
    }
    return {
        tuntap, _getTuntapInfo, handleTuntapEdit, handleTuntapRefresh, clearTuntapTimeout, getTuntapMachines
    }
}

export const useTuntap = () => {
    return inject(tuntapSymbol);
}