import { inject, provide, ref } from "vue";

const forwardSymbol = Symbol();
export const provideForward = () => {
    const forward = ref({
        show: true,
        showEdit: false,
        machineId: null
    });
    provide(forwardSymbol, forward);
    return {
        forward
    }
}
export const useForward = () => {
    return inject(forwardSymbol);
}