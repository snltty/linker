import { inject, provide, ref } from "vue"
const wakeupSymbol = Symbol();
export const provideTransport = () => {
    const transport = ref({
        device: {id:'',name:''},
        show:false,
    });
    provide(wakeupSymbol, transport);
    return {
        transport,
    }
}

export const useTransport = () => {
    return inject(wakeupSymbol);
}