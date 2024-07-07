import { getTunnelInfo, refreshTunnel } from "@/apis/tunnel";
import { injectGlobalData } from "@/provide";
import { ElMessage } from "element-plus";
import { inject, provide, ref } from "vue";

const tunnelSymbol = Symbol();
export const provideTunnel = () => {
    const globalData = injectGlobalData();
    const tunnel = ref({
        timer: 0,
        showEdit: false,
        current: null,
        list: {},
        hashcode: 0,
    });
    provide(tunnelSymbol, tunnel);
    const _getTunnelInfo = () => {
        if (globalData.value.connected) {
            getTunnelInfo(tunnel.value.hashcode.toString()).then((res) => {
                tunnel.value.hashcode = res.HashCode;
                if (res.List) {
                    tunnel.value.list = res.List;
                }
                tunnel.value.timer = setTimeout(_getTunnelInfo, 1000);
            }).catch(() => {
                tunnel.value.timer = setTimeout(_getTunnelInfo, 1000);
            });
        } else {
            tunnel.value.timer = setTimeout(_getTunnelInfo, 1000);
        }
    }
    const handleTunnelEdit = (_tunnel) => {
        tunnel.value.current = _tunnel;
        tunnel.value.showEdit = true;
    }
    const handleTunnelRefresh = () => {
        refreshTunnel();
        ElMessage.success({ message: '刷新成功', grouping: true });
    }
    const clearTunnelTimeout = () => {
        clearTimeout(tunnel.value.timer);
    }
    return {
        tunnel, _getTunnelInfo, handleTunnelEdit, handleTunnelRefresh, clearTunnelTimeout
    }
}
export const useTunnel = () => {
    return inject(tunnelSymbol);
}