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
        linux: ['debian', 'ubuntu', 'alpine', 'rocky', 'centos'],
        windows: ['windows'],
        android: ['android'],
        ios: ['ios'],
    }

    const _getTuntapInfo = () => {
        clearTimeout(tuntap.value.timer);
        if (globalData.value.api.connected) {
            getTuntapInfo(tuntap.value.hashcode.toString()).then((res) => {
                tuntap.value.hashcode = res.HashCode;
                if (res.List) {
                    for (let j in res.List) {

                        let system = 'system';
                        const systemStr = res.List[j].SystemInfo.toLowerCase();
                        for (let jj in systems) {
                            if (systemStr.indexOf(jj) >= 0) {
                                const items = systems[jj];
                                if (items.length == 1) {
                                    system = items[0];
                                } else {
                                    for (let i = 0; i < items.length; i++) {
                                        if (systemStr.indexOf(items[i]) >= 0) {
                                            system = items[i];
                                            break;
                                        }
                                    }
                                }
                                break;
                            }
                        }
                        Object.assign(res.List[j], {
                            running: res.List[j].Status == 2,
                            loading: res.List[j].Status == 1,
                            system: system,
                            systemDocker: systemStr.indexOf('docker') >= 0,
                        });
                    }
                    tuntap.value.list = res.List;
                }
                tuntap.value.timer = setTimeout(_getTuntapInfo, 1100);
            }).catch((e) => {
                tuntap.value.timer = setTimeout(_getTuntapInfo, 1100);
            });
        } else {
            tuntap.value.timer = setTimeout(_getTuntapInfo, 50);
        }
    }
    const handleTuntapEdit = (_tuntap) => {
        console.log(_tuntap);
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
    const sortTuntapIP = (asc) => {
        const sort = Object.values(tuntap.value.list).filter(c => c.IP).sort((a, b) => {
            const arrA = a.IP.split('.').map(c => Number(c));
            const arrB = b.IP.split('.').map(c => Number(c));

            for (let i = 0; i < arrA.length; i++) {
                if (arrA[i] != arrB[i]) {
                    return arrA[i] - arrB[i];
                }
            }
            return 0;
        });
        return sort.map(c => c.MachineId);
    }


    return {
        tuntap, _getTuntapInfo, handleTuntapEdit, handleTuntapRefresh, clearTuntapTimeout, getTuntapMachines, sortTuntapIP
    }
}

export const useTuntap = () => {
    return inject(tuntapSymbol);
}