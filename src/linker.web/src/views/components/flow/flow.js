import { inject, provide, ref } from "vue";

const flowSymbol = Symbol();
export const provideFlow = () => {
    const flow = ref({
        count:false,map:false,allmap:false,
        overallSendtSpeed: '0000.00KB',
        overallReceiveSpeed: '0000.00KB',
        overallOnline: '0/0',
        serverOnline: '',
        machineId:'',
        device:{id:'',name:''},
        showStopwatch:false,
        show:false,
    });
    provide(flowSymbol, flow);
    return {
        flow
    }
}
export const useFlow = () => {
    return inject(flowSymbol);
}