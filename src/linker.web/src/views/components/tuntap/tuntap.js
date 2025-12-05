import { inject, provide, ref } from "vue"
import { getTuntapInfo, refreshTuntap, subscribePing } from "@/apis/tuntap";

const tuntapSymbol = Symbol();
export const provideTuntap = () => {
    const tuntap = ref({
        timer: 0,
        showEdit: false,
        current: null,
        list: null,
        hashcode: 0,

        showLease: false,
        
        device: {id:'',name:''},
        showRoutes:false,
        showFirewall:false,
        showWakeup:false,
    });
    provide(tuntapSymbol, tuntap);

    const reg = /pve|ikuai|fnos|iphone|samsung|vivo|oppo|google|huawei|xiaomi|ios|android|windows|ubuntu|openwrt|armbian|archlinux|fedora|centos|rocky|alpine|debian|linux|docker/g;

    const tuntapDataFn = () => {
        return new Promise((resolve, reject) => { 
            getTuntapInfo(tuntap.value.hashcode.toString()).then((res) => {

                subscribePing();
                tuntap.value.hashcode = res.HashCode;
                if (res.List) {
                    for (let j in res.List) {
                        const systemStr = res.List[j].SystemInfo.toLowerCase();
                        const match =[...new Set(systemStr.match(reg))];
                        Object.assign(res.List[j], {
                            running: res.List[j].Status == 2,
                            loading: res.List[j].Status == 1,
                            systems: match,
                        });
                    }
                    tuntap.value.list = res.List;
                    resolve(true);
                    return;
                }
                resolve(false);
            }).catch((e) => {
                resolve(false);
            });
        });
    }  
    const tuntapRefreshFn = () => {
        refreshTuntap();
    }
    const tuntapProcessFn = (device,json) => { 
        if(! tuntap.value.list) return;
        Object.assign(json,{
            hook_tuntap: tuntap.value.list[device.MachineId],
            hook_tuntap_load:true
        });
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
        tuntap, tuntapDataFn, tuntapProcessFn, tuntapRefreshFn, getTuntapMachines, sortTuntapIP
    }
}

export const useTuntap = () => {
    return inject(tuntapSymbol);
}