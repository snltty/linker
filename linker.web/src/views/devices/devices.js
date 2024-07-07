import { getSignInList, signInDel } from "@/apis/signin";
import { injectGlobalData } from "@/provide";
import { computed, reactive } from "vue";

export const provideDevices = () => {

    const globalData = injectGlobalData();
    const machineId = computed(() => globalData.value.config.Client.Id);
    const devices = reactive({
        timer: 0,
        page: {
            Request: {
                Page: 1, Size: +(localStorage.getItem('ps') || '10'),
                GroupId: globalData.value.groupid, Name: '', Ids: []
            },
            Count: 0,
            List: []
        },

        showDeviceEdit: false,
        deviceInfo: null
    });
    const _getSignList = () => {
        devices.page.Request.GroupId = globalData.value.groupid;
        getSignInList(devices.page.Request).then((res) => {
            devices.page.Request = res.Request;
            devices.page.Count = res.Count;
            for (let j in res.List) {
                res.List[j].showTunnel = machineId.value != res.List[j].MachineId;
                res.List[j].showForward = machineId.value != res.List[j].MachineId;
                res.List[j].showSForward = machineId.value == res.List[j].MachineId;
                res.List[j].showDel = machineId.value != res.List[j].MachineId && res.List[j].Connected == false;
                res.List[j].isSelf = machineId.value == res.List[j].MachineId;
            }
            devices.page.List = res.List.sort((a, b) => b.Connected - a.Connected);
        }).catch((err) => { });
    }
    const _getSignList1 = () => {
        if (globalData.value.connected) {
            devices.page.Request.GroupId = globalData.value.groupid;
            getSignInList(devices.page.Request).then((res) => {
                for (let j in res.List) {
                    const item = devices.page.List.filter(c => c.MachineId == res.List[j].MachineId)[0];
                    if (item) {
                        item.Connected = res.List[j].Connected;
                        item.Version = res.List[j].Version;
                        item.LastSignIn = res.List[j].LastSignIn;
                        item.Args = res.List[j].Args;
                        item.showTunnel = machineId.value != res.List[j].MachineId;
                        item.showForward = machineId.value != res.List[j].MachineId;
                        item.showSForward = machineId.value == res.List[j].MachineId;
                        item.showDel = machineId.value != res.List[j].MachineId && res.List[j].Connected == false;
                        item.isSelf = machineId.value == res.List[j].MachineId;
                    }
                }
                devices.timer = setTimeout(_getSignList1, 5000);
            }).catch((err) => {
                devices.timer = setTimeout(_getSignList1, 5000);
            });
        } else {
            devices.timer = setTimeout(_getSignList1, 5000);
        }
    }

    const handleDeviceEdit = (row) => {
        devices.deviceInfo = row;
        devices.showDeviceEdit = true;
    }
    const handlePageChange = (page) => {
        if (page) {
            devices.page.Request.Page = page;
        }
        _getSignList();
    }
    const handlePageSizeChange = (size) => {
        if (size) {
            devices.page.Request.Size = size;
            localStorage.setItem('ps', size);
        }
        _getSignList();
    }
    const handleDel = (name) => {
        signInDel(name).then(() => {
            _getSignList();
        });
    }
    const clearDevicesTimeout = () => {
        clearTimeout(devices.timer);
        devices.timer = 0;
    }

    return {
        devices, machineId, _getSignList, _getSignList1, handleDeviceEdit, handlePageChange, handlePageSizeChange, handleDel, clearDevicesTimeout
    }
}