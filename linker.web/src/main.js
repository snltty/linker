import { createApp } from 'vue'
import App from './App.vue'
import router from './router'


const app = createApp(App);

import './assets/style.css'

import ElementPlus from 'element-plus';
import 'element-plus/dist/index.css'
import 'element-plus/theme-chalk/display.css'
import 'element-plus/theme-chalk/dark/css-vars.css'

app.use(ElementPlus, { size: 'default' }).use(router).mount('#app');
