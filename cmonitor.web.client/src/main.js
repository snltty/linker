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
    ArrowDown, Top, Bottom, Delete, Plus, Select, Refresh
} from '@element-plus/icons-vue'
app.component(ArrowDown.name, ArrowDown);
app.component(Top.name, Top);
app.component(Bottom.name, Bottom);
app.component(Delete.name, Delete);
app.component(Plus.name, Plus);
app.component(Select.name, Select);
app.component(Refresh.name, Refresh);


app.use(ElementPlus, { size: 'default' }).use(router).mount('#app');
