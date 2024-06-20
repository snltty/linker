import { inject, provide, ref } from "vue"

const pluginStateSymbol = Symbol();
export const providePluginState = (data) => {
    const state = ref(data);
    provide(pluginStateSymbol, state);
    return state;
}
export const injectPluginState = () => {
    return inject(pluginStateSymbol);
}