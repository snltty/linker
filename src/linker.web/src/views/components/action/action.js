import { inject, provide, ref } from "vue"
const actionSymbol = Symbol();
export const provideAction = () => {
    const action = ref({
        device: {id:'',name:''},
        show:false,
    });
    provide(actionSymbol, action);
    return {
        action,
    }
}

export const useAction = () => {
    return inject(actionSymbol);
}