import { relayOperating } from "@/apis/relay";
import { getTunnelInfo, tunnelRefresh,tunnelOperating } from "@/apis/tunnel";
import { injectGlobalData } from "@/provide";
import { inject, provide, ref } from "vue";

const tunnelSymbol = Symbol();
export const provideTunnel = () => {
    const globalData = injectGlobalData();
    const tunnel = ref({
      
        timer1: 0,
        p2pOperatings:{},
        timer2: 0,
        relayOperatings:{},

        showEdit: false,
        current: null,
        timer: 0,
        list: {},
        hashcode: 0,
        showMap: false
    });
    provide(tunnelSymbol, tunnel);
    const _getTunnelInfo = () => {
        clearTimeout(tunnel.value.timer);
        getTunnelInfo(tunnel.value.hashcode.toString()).then((res) => {
            tunnel.value.hashcode = res.HashCode;
            if (res.List) {
                tunnel.value.list = res.List;
            }
            tunnel.value.timer = setTimeout(_getTunnelInfo, 1060);
        }).catch(() => {
            tunnel.value.timer = setTimeout(_getTunnelInfo, 1060);
        });
    }
    const getTunnelOperating = () => { 
        clearTimeout(tunnel.value.timer1);
        tunnelOperating().then((res) => {
            tunnel.p2pOperatings = res;
            tunnel.value.timer1 = setTimeout(getTunnelOperating, 1080);
        }).catch(() => {
            tunnel.value.timer1 = setTimeout(getTunnelOperating, 1080);
        });
    }
    const getRelayOperating = () => { 
        clearTimeout(tunnel.value.timer2);
        relayOperating().then((res) => {
            tunnel.relayOperatings = res;
            tunnel.value.timer2 = setTimeout(getRelayOperating, 1040);
        }).catch(() => {
            tunnel.value.timer2 = setTimeout(getRelayOperating, 1040);
        });
    }

    const handleTunnelEdit = (_tunnel) => {
        tunnel.value.current = _tunnel;
        tunnel.value.showEdit = true;
    }
    const handleTunnelRefresh = () => {
        tunnelRefresh();
    }
    const clearTunnelTimeout = () => {
        clearTimeout(tunnel.value.timer);
        clearTimeout(tunnel.value.timer1);
        clearTimeout(tunnel.value.timer2);
    }
    const sortTunnel = (asc) => {
        return Object.values(tunnel.value.list).sort((a, b) => {
            return a.RouteLevel + a.RouteLevelPlus - b.RouteLevel + b.RouteLevelPlus;
        }).map(c => c.MachineId);
    }
    return {
        tunnel, _getTunnelInfo,getTunnelOperating,getRelayOperating, handleTunnelEdit, handleTunnelRefresh, clearTunnelTimeout, sortTunnel
    }
}
export const useTunnel = () => {
    return inject(tunnelSymbol);
}