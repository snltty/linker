import { getSignInList, setSignInOrder } from "@/apis/signin";
import { injectGlobalData } from "@/provide";
import { computed, inject, provide, reactive, ref } from "vue";

const deviceSymbol = Symbol();
export const provideDevices = () => {
    //https://api.ipbase.com/v1/json/8.8.8.8
    const globalData = injectGlobalData();
    const machineId = computed(() => globalData.value.config.Client.Id);
    const devices = reactive({
        timer: 0,
        timer1: 0,
        page: {
            Request: {
                Page: 1, Size: +(localStorage.getItem('ps') || '10'), Name: '', Ids: [], Prop: '', Asc: true
            },
            Count: 0,
            List: []
        },

        showDeviceEdit: false,
        showAccessEdit: false,
        deviceInfo: null
    });

    provide(deviceSymbol, devices);

    const hooks = {};
    const deviceAddHook = (name,dataFn,processFn,refreshFn) => {
        hooks[name] = {dataFn,processFn,refreshFn,changed:true,refresh:true};
    }
    const deviceRefreshHook = (name) => {
        if(hooks[name]) {
            hooks[name].refresh = true;
            hooks[name].changed = true;
        }
    }
    const startHooks = () => { 
        const fn = async ()=>{
            clearTimeout(devices.timer1);
            for(let name in hooks) {
                const hook = hooks[name];
                if(hook.refresh) {
                    hook.refresh = false;
                    hook.refreshFn(devices.page.List);
                }
            }
            for(let name in hooks) {
                const hook = hooks[name];
                if(hook.changed) {
                    hook.changed = false;
                    for (let i = 0; i< devices.page.List.length; i++) {
                        const json = {}
                        hook.processFn(devices.page.List[i],json);
                        Object.assign(devices.page.List[i], json);
                    }
                }
            }
            for(let name in hooks) {
                const hook = hooks[name];
                hook.changed = await hook.dataFn(devices.page.List);
            }
            devices.timer1 = setTimeout(fn,1000);
        }
        fn();
    }
    startHooks();

    const deviceStartProcess = () => { 
        _getSignList().then(()=>{
            startHooks();
            _getSignList1();
        });
    }
    const _getSignList = () => {
        return new Promise((resolve, reject) => { 
            getSignInList(devices.page.Request).then((res) => {
                devices.page.Request = res.Request;
                devices.page.Count = res.Count;
                for (let j in res.List) {
                    Object.assign(res.List[j], {
                        showDel: machineId.value != res.List[j].MachineId && res.List[j].Connected == false,
                        showAccess: machineId.value != res.List[j].MachineId && res.List[j].Connected,
                        showReboot: res.List[j].Connected,
                        isSelf: machineId.value == res.List[j].MachineId
                    });
                    if (res.List[j].isSelf) {
                        globalData.value.self = res.List[j];
                    }
                }
                devices.page.List = res.List;
                for(let name in hooks) {
                    hooks[name].changed = true;
                }
                resolve()
            }).catch((err) => { resolve() });
        });
    }
    const _getSignList1 = () => {
        clearTimeout(devices.timer);
        getSignInList(devices.page.Request).then((res) => {
            for (let j in res.List) {
                const item = devices.page.List.filter(c => c.MachineId == res.List[j].MachineId)[0];
                if (item) {
                    Object.assign(item, {
                        Connected: res.List[j].Connected,
                        Version: res.List[j].Version,
                        LastSignIn: res.List[j].LastSignIn,
                        Args: res.List[j].Args,
                        showDel: machineId.value != res.List[j].MachineId && res.List[j].Connected == false,
                        showAccess: machineId.value != res.List[j].MachineId && res.List[j].Connected,
                        showReboot: res.List[j].Connected,
                        isSelf: machineId.value == res.List[j].MachineId,
                    });
                    if (item.isSelf) {
                        globalData.value.self = item;
                    }
                }
            }
            devices.timer = setTimeout(_getSignList1, 5000);
        }).catch((err) => {
            devices.timer = setTimeout(_getSignList1, 5000);
        });
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
    const deviceClearTimeout = () => {
        clearTimeout(devices.timer);
        clearTimeout(devices.timer1);
        devices.timer = 0;
        devices.timer1 = 0;
    }

    const setSort = (ids) => {
        return setSignInOrder(ids);
    }

    return {
        devices,deviceAddHook,deviceRefreshHook, deviceStartProcess, handlePageChange, handlePageSizeChange, deviceClearTimeout, setSort
    }
}
export const useDevice = () => {
    return inject(deviceSymbol);
}