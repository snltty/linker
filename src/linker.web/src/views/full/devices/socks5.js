import { inject, provide, ref } from "vue"
import { getSocks5Info, refreshSocks5 } from "@/apis/socks5";

const socks5Symbol = Symbol();
export const provideSocks5 = () => {
    const socks5 = ref({
        show: true,
        timer: 0,
        showEdit: false,
        current: null,
        list: {},
        hashcode: 0,
    });
    provide(socks5Symbol, socks5);

    const _getSocks5Info = () => {
        clearTimeout(socks5.value.timer);
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
            }
            socks5.value.timer = setTimeout(_getSocks5Info, 1100);
        }).catch((e) => {
            socks5.value.timer = setTimeout(_getSocks5Info, 1100);
        });
    }
    const handleSocks5Edit = (_socks5) => {
        socks5.value.current = _socks5;
        socks5.value.showEdit = true;

    }
    const handleSocks5Refresh = () => {
        refreshSocks5();
    }
    const clearSocks5Timeout = () => {
        clearTimeout(socks5.value.timer);
        socks5.value.timer = 0;
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
        socks5, _getSocks5Info, handleSocks5Edit, handleSocks5Refresh, clearSocks5Timeout, getSocks5Machines, sortSocks5
    }
}

export const useSocks5 = () => {
    return inject(socks5Symbol);
}