import { wlistList } from "@/apis/wlist";
import { inject, provide, ref } from "vue"
const wlistSymbol = Symbol();
export const provideWlist = () => {
    const wlist = ref({
        device: {id:'',name:'',type:'',typeText:''},
        show:false,

        list: null,
    });
    provide(wlistSymbol, wlist);


    const wlistDataFn = (devices) => {
        return new Promise((resolve, reject) => {
            if(wlist.value.list !== null){
                resolve(false);
                return;
            }
            wlistList({Key:'Relay',Value:devices.map(c=>c.MachineId)}).then((res) => {
                wlist.value.list = res;
                resolve(true);
            }).catch(() => {
                 resolve(false);
            });
        })
    }
    const wlistRefreshFn = () => {
        wlist.value.list = null;
    }
    const wlistProcessFn = (device,json) => { 
        if(!wlist.value.list) return;
        Object.assign(json,{
            hook_wlist: wlist.value.list[`m_${device.MachineId}`] || wlist.value.list[`u_${device.Args.userid}`] || {},
            hook_wlist_load:true
        });
    }


    return {
        wlist,wlistDataFn,wlistProcessFn,wlistRefreshFn
    }
}

export const useWlist = () => {
    return inject(wlistSymbol);
}