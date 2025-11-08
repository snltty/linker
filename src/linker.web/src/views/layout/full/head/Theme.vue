<template>
    <PcShow>
        <div class="image">
            <a href="javascript:;" @click="changeMode('light')" v-if="state.mode == 'dark'"><el-icon><Moon /></el-icon></a>
            <a href="javascript:;" @click="changeMode('dark')" v-else><el-icon><Sunny /></el-icon></a>
        </div>
    </PcShow>
</template>

<script>
import {Moon,Sunny} from '@element-plus/icons-vue'
import { onMounted, reactive } from 'vue';
export default {
    components:{Moon,Sunny},
    setup () {

        const isSystemDarkMode = window.matchMedia('(prefers-color-scheme: dark)').matches;
        const isSystemLightMode = window.matchMedia('(prefers-color-scheme: light)').matches;
        const cacheMode = localStorage.getItem('theme-mode') || (isSystemDarkMode?'dark':'light');
        const state = reactive({
            mode: cacheMode,
        });
        const changeMode = (mode)=>{
            state.mode = mode;
            localStorage.setItem('theme-mode', mode);
            setMode();
        }
        const setMode = ()=>{
            document.querySelector('html').setAttribute('class', state.mode);
        }

        onMounted(()=>{
            setMode();
        })

        return {state,changeMode}
    }
}
</script>

<style lang="stylus" scoped>
.el-icon{
    font-size:1.6rem;
    vertical-align:middle;
    color:#555;
}
.image{
    padding-right:1rem;
}
</style>