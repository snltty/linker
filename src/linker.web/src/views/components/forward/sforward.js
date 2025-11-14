import { ref, provide, inject } from 'vue';

const sforwardSymbol = Symbol();
export const provideSforward = () => {
    const sforward = ref({
        showEdit: false,
        showCopy: false,
        machineid: '',
        machineName: '',
    });
    provide(sforwardSymbol, sforward);

    return {
        sforward
    }

}
export const useSforward = () => {
    return inject(sforwardSymbol);
}