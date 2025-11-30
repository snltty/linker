import { getSignInList, setSignInOrder } from "@/apis/signin";
import { injectGlobalData } from "@/provide";
import { computed, inject, provide, reactive, ref } from "vue";

const deviceSymbol = Symbol();
export const provideDevices = () => {
    //https://api.ipbase.com/v1/json/8.8.8.8
    const globalData = injectGlobalData();
    const machineId = computed(() => globalData.value.config.Client.Id);

    const ps = +(localStorage.getItem('ps') || '10');
    const count = +(localStorage.getItem('device-count') || '10');
    const devices = reactive({
        timer: 0,
        timer1: 0,
        page: {
            Request: {
                Page: 1, Size: ps, Name: '', Ids: [], Prop: '', Asc: true
            },
            Count: count,
            List: Array(count).fill().map(c=>{ return {}})
        },
        loadTimer:0,

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

        const dataFn = (hook)=>{
            return new Promise((resolve, reject) => { 
                hook.dataFn(devices.page.List).then(changed=>{
                    hook.changed = hook.changed ||changed;
                    resolve();
                });
            });
        }
        const fn = async ()=>{
            clearTimeout(devices.timer1);

            const refreshs = Object.values(hooks).filter(c=>c.refresh);
            refreshs.forEach(hook=>{ 
                hook.refresh = false;
                hook.refreshFn(devices.page.List);
            });

            const chaneds = Object.values(hooks).filter(c=>c.changed);
            chaneds.forEach(hook=>{ hook.changed=false });
            if(chaneds.length > 0){
                for (let i = 0; i< devices.page.List.length; i++) {
                    const json = {_index:i};
                    for(let j = 0; j < chaneds.length; j++) {
                        const hook = chaneds[j];
                        hook.processFn(devices.page.List[i],json);
                    }
                    Object.assign(devices.page.List[i], json);
                }
            }
            await Promise.all(Object.values(hooks).map(hook=>dataFn(hook)));
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
                console.log(res);
                for (let j in res.List) {
                    Object.assign(res.List[j], {
                        showDel: machineId.value != res.List[j].MachineId && res.List[j].Connected == false,
                        showAccess: machineId.value != res.List[j].MachineId && res.List[j].Connected,
                        showReboot: res.List[j].Connected,
                        isSelf: machineId.value == res.List[j].MachineId,
                        animationDelay: j*50
                    });
                    if (res.List[j].isSelf) {
                        globalData.value.self = res.List[j];
                    }
                }
                devices.page.List = res.List;
                for(let name in hooks) {
                    hooks[name].changed = true;
                }

                localStorage.setItem('device-count',devices.page.Count);
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
        clearTimeout(devices.loadTimer);
        devices.loadTimer = setTimeout(_getSignList,300);
    }
    const handlePageSizeChange = (size) => {
        if (size) {
            devices.page.Request.Size = size;
            localStorage.setItem('ps', size);
        }
        clearTimeout(devices.loadTimer);
        devices.loadTimer = setTimeout(_getSignList,300);
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