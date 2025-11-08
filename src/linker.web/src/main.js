import { createApp } from 'vue'
import App from './App.vue'
const app = createApp(App);

import i18n from './lang';
app.use(i18n);

import router from './router'
app.use(router);

import AccessShow from './views/components/accesss/AccessShow.vue';
app.component('AccessShow', AccessShow);
import AccessBoolean from './views/components/accesss/AccessBoolean.vue';
app.component('AccessBoolean', AccessBoolean);
import PhoneShow from './views/components/global/PhoneShow.vue';
app.component('PhoneShow', PhoneShow);
import PcShow from './views/components/global/PcShow.vue';
app.component('PcShow', PcShow);

import './assets/style.css'
import ElementPlus from 'element-plus';
import 'element-plus/dist/index.css'
import 'element-plus/theme-chalk/display.css'
import 'element-plus/theme-chalk/dark/css-vars.css'
app.use(ElementPlus, { size: 'default' });

app.mount('#app');


app.directive('trim', {
    mounted(el, binding) {
        const inputEl = el.querySelector('input');
        if (!inputEl) {
            console.error('v-trim 指令只能用于包含 input 的元素');
            return;
        }
        const trimHandler =  () => {
            const trimmedValue = inputEl.value.trim();
            if (trimmedValue !== inputEl.value) {
                inputEl.value = trimmedValue;
                const event = new Event('input', { bubbles: true });
                inputEl.dispatchEvent(event);
            }
        };
        inputEl.addEventListener('blur',trimHandler);
        el._trimHandler = trimHandler;
    },
    unmounted(el){
        const trimHandler = el._trimHandler;
        if (trimHandler) {
            const inputEl = el.querySelector('input');
            if(inputEl){
                inputEl.removeEventListener('blur', trimHandler);
            }
            delete el._trimHandler;
        }
    }
});

const ignoreErrors = [
  "ResizeObserver loop completed with undelivered notifications",
  "ResizeObserver loop limit exceeded"
];
window.addEventListener('error', e => {
    let errorMsg = e.message;
    ignoreErrors.forEach(m => {
        if (errorMsg.startsWith(m)) {
        console.error(errorMsg);
        if (e.error) {
            console.error(e.error.stack);
        }
        const resizeObserverErrDiv = document.getElementById(
            'webpack-dev-server-client-overlay-div'
        );
        const resizeObserverErr = document.getElementById(
            'webpack-dev-server-client-overlay'
        );
        if (resizeObserverErr) {
            resizeObserverErr.setAttribute('style', 'display: none');
        }
        if (resizeObserverErrDiv) {
            resizeObserverErrDiv.setAttribute('style', 'display: none');
        }
        }
    });
});