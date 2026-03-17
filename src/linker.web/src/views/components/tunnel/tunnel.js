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
        showUpnp: false,
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
            const _json = {
                [arr[0]]: {
                    [arr[1]]: {
                        [arr[2]]: operatings[key],
                        loading: (json[arr[0]] && json[arr[0]][arr[1]] && json[arr[0]][arr[1]].loading) || operatings[key]
                    }
                }
            };
           
            Object.assign(json,_json);
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
        if(json.hook_tunnel.Net){
            const arr = json.hook_tunnel.Net.Nat.split('-');
            const arr1 = arr[0].split('/');
            json.hook_tunnel.Net.nat_number = parseInt(arr[1] || '0');
            json.hook_tunnel.Net.Nat = `RFC 5780\n映射类型 : ${arr1[0] || 'Unknown'}\n过滤类型 : ${arr[0] || 'Unknown'}\n成功几率 : ${json.hook_tunnel.Net.nat_number}%`;
        }
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