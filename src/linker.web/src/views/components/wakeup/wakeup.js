import { inject, provide, ref } from "vue"
const wakeupSymbol = Symbol();
export const provideWakeup = () => {
    const wakeup = ref({
        device: {id:'',name:''},
        show:false,
    });
    provide(wakeupSymbol, wakeup);
    return {
        wakeup,
    }
}

export const useWakeup = () => {
    return inject(wakeupSymbol);
}