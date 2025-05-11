import { inject, provide, ref } from "vue"
import { getTuntapInfo, refreshTuntap, subscribePing } from "@/apis/tuntap";

const tuntapSymbol = Symbol();
export const provideTuntap = () => {
    const tuntap = ref({
        show: true,
        timer: 0,
        showEdit: false,
        current: null,
        list: {},
        hashcode: 0,

        showLease: false,
        
        device: {id:'',name:''},
        showRoutes:false,
    });
    provide(tuntapSymbol, tuntap);

    const reg = /iphone|samsung|vivo|oppo|google|huawei|xiaomi|ios|android|windows|ubuntu|openwrt|armbian|archlinux|fedora|centos|rocky|alpine|debian|linux|docker/g;

    const _getTuntapInfo = () => {
        clearTimeout(tuntap.value.timer);
        getTuntapInfo(tuntap.value.hashcode.toString()).then((res) => {
            tuntap.value.hashcode = res.HashCode;
            if (res.List) {
                for (let j in res.List) {
                    const systemStr = res.List[j].SystemInfo.toLowerCase();

                    const match =[...new Set(systemStr.match(reg))];
                    Object.assign(res.List[j], {
                        running: res.List[j].Status == 2,
                        loading: res.List[j].Status == 1,
                        systems: match
                    });
                }
                tuntap.value.list = res.List;
            }
            tuntap.value.timer = setTimeout(_getTuntapInfo, 1100);
            subscribePing();
        }).catch((e) => {
            tuntap.value.timer = setTimeout(_getTuntapInfo, 1100);
        });
    }
    const handleTuntapEdit = (_tuntap) => {
        tuntap.value.current = _tuntap;
        tuntap.value.showEdit = true;

    }
    const handleTuntapRefresh = () => {
        refreshTuntap();
    }
    const clearTuntapTimeout = () => {
        clearTimeout(tuntap.value.timer);
        tuntap.value.timer = 0;
    }
    const getTuntapMachines = (name) => {
        return Object.values(tuntap.value.list)
            .filter(c => c.IP.indexOf(name) >= 0 || (c.Lans.filter(d => d.IP.indexOf(name) >= 0).length > 0))
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