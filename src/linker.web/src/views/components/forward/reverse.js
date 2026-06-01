import { ref, provide, inject } from 'vue';

const reverseSymbol = Symbol();
export const provideReverse = () => {
    const reverse = ref({
        showEdit: false,
        showCopy: false,
        machineid: '',
        machineName: '',
    });
    provide(reverseSymbol, reverse);

    return {
        reverse
    }

}
export const useReverse = () => {
    return inject(reverseSymbol);
}