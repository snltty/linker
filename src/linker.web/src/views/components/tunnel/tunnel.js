import { getTunnelInfo, tunnelRefresh,tunnelOperating } from "@/apis/tunnel";
import { inject, provide, ref } from "vue";

const tunnelSymbol = Symbol();
export const provideTunnel = () => {
    const tunnel = ref({
        
        list: null,
        hashcode: 0,

        operatings:{},
        hashcode1: 0,

        showEdit: false,
        current: null,
        
    });
    provide(tunnelSymbol, tunnel);
    const _getTunnelInfo = () => {
        return new Promise((resolve, reject) => {
            getTunnelInfo(tunnel.value.hashcode.toString()).then((res) => {
                tunnel.value.hashcode = res.HashCode;
                if (res.List) {
                    tunnel.value.list = res.List;
                    resolve(true);
                    return;
                }
                resolve(false);
            }).catch(() => {
                resolve(false);
            });
        });
    }

    const parseOperating = (operatings) => {
        const json = {};
        for(let key in operatings){
            let arr = key.split('@');
            json[arr[0]] = json[arr[0]] ||{};
            json[arr[0]][arr[1]] = operatings[key];
        }
        return json;
    }
    const getTunnelOperating = () => { 
        return new Promise((resolve, reject) => {
            tunnelOperating(tunnel.value.hashcode1.toString()).then((res) => {
                tunnel.value.hashcode1 = res.HashCode;
                if (res.List)
                {
                    tunnel.value.operatings = parseOperating(res.List);
                    resolve(true);
                    return;
                }
                resolve(false);
            }).catch(() => {
                resolve(false);
            });
        });
    }
    const tunnelDataFn = () => { 
        return new Promise((resolve, reject) => { 
            Promise.all([_getTunnelInfo(), getTunnelOperating()]).then((res) => {
                resolve(res.filter(c=>c == true).length > 0);
            }).catch(() => {
                resolve(false);
            });
        });
    }
    const tunnelRefreshFn = () => { 
        tunnelRefresh();
    }

    
    const tunnelProcessFn = (device,json) => { 
        if(!tunnel.value.list || !tunnel.value.operatings) return;
        
        Object.assign(json,{
            hook_tunnel: tunnel.value.list[device.MachineId],
            hook_tunnel_load:true,
            hook_operating: tunnel.value.operatings[device.MachineId],
            hook_operating_load: true,
        });
    }
    
    const sortTunnel = (asc) => {
        return Object.values(tunnel.value.list).sort((a, b) => {
            return a.RouteLevel + a.RouteLevelPlus - b.RouteLevel + b.RouteLevelPlus;
        }).map(c => c.MachineId);
    }
    return {
        tunnel, tunnelDataFn,tunnelProcessFn,tunnelRefreshFn, sortTunnel
    }
}
export const useTunnel = () => {
    return inject(tunnelSymbol);
}