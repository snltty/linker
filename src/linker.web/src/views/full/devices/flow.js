import { inject, provide, ref } from "vue";

const flowSymbol = Symbol();
export const provideFlow = () => {
    const flow = ref({
        device: {},
        show: false,
    });
    provide(flowSymbol, flow);
    return {
        flow
    }
}
export const useFlow = () => {
    return inject(flowSymbol);
}