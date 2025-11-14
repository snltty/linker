import { inject, provide, ref } from "vue"
const firewallSymbol = Symbol();
export const provideFirewall = () => {
    const firewall = ref({
        device: {id:'',name:''},
        show:false,
    });
    provide(firewallSymbol, firewall);
    return {
        firewall,
    }
}

export const useFirewall = () => {
    return inject(firewallSymbol);
}