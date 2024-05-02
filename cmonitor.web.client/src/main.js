import { createApp } from 'vue'
import App from './App.vue'
import router from './router'

// import VConsole from 'vconsole';
// new VConsole();

const app = createApp(App);

import './assets/style.css'

import ElementPlus from 'element-plus';
import 'element-plus/dist/index.css'
import 'element-plus/theme-chalk/display.css'
import 'element-plus/theme-chalk/dark/css-vars.css'

import {
    ArrowDown,
} from '@element-plus/icons-vue'
app.component(ArrowDown.name, ArrowDown);


app.use(ElementPlus, { size: 'default' }).use(router).mount('#app');
