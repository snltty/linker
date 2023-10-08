import { createApp } from 'vue'
import App from './App.vue'
import router from './router'

const app = createApp(App);

import './assets/style.css'

import ElementPlus from 'element-plus';
import 'element-plus/dist/index.css'
import 'element-plus/theme-chalk/display.css'
import 'element-plus/theme-chalk/dark/css-vars.css'

import {
    ChromeFilled, Promotion, Grid, ArrowDown, Upload, Download, EditPen, Delete, Refresh, BellFilled, Microphone
    , Position, Message, Bell, Mute, SwitchButton, Lock, DataLine, CirclePlus, QuestionFilled, Monitor, Sunny, Warning, Umbrella
    , ScaleToOriginal, Close, Help
} from '@element-plus/icons-vue'
app.component(ChromeFilled.name, ChromeFilled);
app.component(Promotion.name, Promotion);
app.component(Grid.name, Grid);
app.component(ArrowDown.name, ArrowDown);
app.component(Upload.name, Upload);
app.component(Download.name, Download);
app.component(EditPen.name, EditPen);
app.component(Delete.name, Delete);
app.component(Refresh.name, Refresh);
app.component(BellFilled.name, BellFilled);
app.component(Microphone.name, Microphone);

app.component(Position.name, Position);
app.component(Message.name, Message);
app.component(Bell.name, Bell);
app.component(Mute.name, Mute);
app.component(SwitchButton.name, SwitchButton);
app.component(Lock.name, Lock);
app.component(DataLine.name, DataLine);
app.component(CirclePlus.name, CirclePlus);
app.component(QuestionFilled.name, QuestionFilled);
app.component(Monitor.name, Monitor);
app.component(Sunny.name, Sunny);
app.component(Warning.name, Warning);
app.component(Umbrella.name, Umbrella);
app.component(ScaleToOriginal.name, ScaleToOriginal);
app.component(Close.name, Close);
app.component(Help.name, Help);


app.use(ElementPlus, { size: 'default' }).use(router).mount('#app');
