import { relayOperating } from "@/apis/relay";
import { getTunnelInfo, tunnelRefresh,tunnelOperating } from "@/apis/tunnel";
import { inject, provide, ref } from "vue";

const tunnelSymbol = Symbol();
export const provideTunnel = () => {
    const tunnel = ref({
        
        timer: 0,
        list: null,
        hashcode: 0,

        operatings:null,

        timer1: 0,
        p2pOperatings:{},
        hashcode1: 0,

        timer2: 0,
        relayOperatings:{},
        hashcode2: 0,

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
                    tunnel.value.p2pOperatings = parseOperating(res.List);
                    resolve(true);
                    return;
                }
                resolve(false);
            }).catch(() => {
                resolve(false);
            });
        });
    }
    const getRelayOperating = () => { 
        return new Promise((resolve, reject) => {
            relayOperating(tunnel.value.hashcode2.toString()).then((res) => {
                tunnel.value.hashcode2 = res.HashCode;
                if (res.List)
                {
                    tunnel.value.relayOperatings = parseOperating(res.List);
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
            Promise.all([_getTunnelInfo(), getTunnelOperating(), getRelayOperating()]).then((res) => {

                const result = res.filter(c=>c == true).length > 0;
                if(result){
                    const p2p = tunnel.value.p2pOperatings;
                    const relay = tunnel.value.relayOperatings;
                    if(p2p && relay){
                        const keys = [...new Set(Object.keys(p2p).concat(Object.keys(relay)))];
                        const json = {};
                        for(let key of keys) {
                            json[key] = json[key] || {};
                            Object.assign(json[key],p2p[key]||{},relay[key]||{});
                        }
                        tunnel.value.operatings = json;
                    }
                }
                resolve(result);
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