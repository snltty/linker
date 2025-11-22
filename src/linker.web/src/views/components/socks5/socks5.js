import { inject, provide, ref } from "vue"
import { getSocks5Info, refreshSocks5 } from "@/apis/socks5";

const socks5Symbol = Symbol();
export const provideSocks5 = () => {
    const socks5 = ref({
        show: true,
        timer: 0,
        showEdit: false,
        current: null,
        list: null,
        hashcode: 0,
    });
    provide(socks5Symbol, socks5);

    const socks5DataFn = () => {
        return new Promise((resolve, reject) => { 
            getSocks5Info(socks5.value.hashcode.toString()).then((res) => {
                socks5.value.hashcode = res.HashCode;
                if (res.List) {
                    for (let j in res.List) {
                        Object.assign(res.List[j], {
                            running: res.List[j].Status == 2,
                            loading: res.List[j].Status == 1
                        });
                    }
                    socks5.value.list = res.List;
                    resolve(true);
                    return;
                }
                resolve(false);
            }).catch((e) => {
                resolve(false);
            });
        });
    }
    const socks5ProcessFn = (device,json) => {
        if(!socks5.value.list) return;
        Object.assign(json,{
            hook_socks5: socks5.value.list[device.MachineId] || '',
            hook_socks5_load:true
        });
    }
    const socks5RefreshFn = () => {
        refreshSocks5();
    }
    const getSocks5Machines = (name) => {
        return Object.values(socks5.value.list)
            .filter(c => c.Port.toString().indexOf(name) >= 0 || (c.Lans.filter(d => d.IP.indexOf(name) >= 0).length > 0))
            .map(c => c.MachineId);
    }
    const sortSocks5 = (asc) => {
        const sort = Object.values(socks5.value.list).sort((a, b) => a.Port - b.Port);
        return sort.map(c => c.MachineId);
    }


    return {
        socks5, socks5DataFn,socks5ProcessFn, socks5RefreshFn, getSocks5Machines, sortSocks5
    }
}

export const useSocks5 = () => {
    return inject(socks5Symbol);
}