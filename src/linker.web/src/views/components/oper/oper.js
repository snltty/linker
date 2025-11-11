import { inject, provide, ref } from "vue"
const operSymbol = Symbol();
export const provideOper = () => {
    const oper = ref({
        device: {id:'',name:''},
        showFirewall:false,
        showWakeup:false,
        showTransport:false,
        showAction:false,
    });
    provide(operSymbol, oper);
    return {
        oper,
    }
}

export const useOper = () => {
    return inject(operSymbol);
}