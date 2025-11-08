<template>
    <div class="app-wrap flex flex-column flex-nowrap" id="app-wrap">
        <div class="head">
            <Head></Head>
        </div>
        <div class="adv">
            <Adv></Adv>
        </div>
        <div class="body flex-1 relative" ref="wrap" id="main-body">
            <div class="home absolute">
                <router-view></router-view>
            </div>
        </div>
        <div class="status">
            <Status :config="true"></Status>
            <Install></Install>
        </div>
    </div>
</template>

<script>
import Head from './head/Index.vue'
import Status from '../../components/status/Index.vue'
import Install from './install/Index.vue'
import { injectGlobalData } from '@/provide';
import { nextTick, onMounted, onUnmounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import Adv from '../../components/adv/Index.vue'
export default {
    name: 'Index',
    components: {Head, Status, Install,Adv},
    setup(props) {
        const globalData = injectGlobalData();
        const router = useRouter();

        const wrap = ref(null);
        const resizeTable = () => {
            nextTick(() => {
                globalData.value.height = wrap.value.offsetHeight;
                globalData.value.width = window.innerWidth;
            });
        }
        onMounted(() => {
            if(globalData.value.hasAccess('FullManager') == false){
                router.push({name:'NoPermission'});
            }
            window.addEventListener('resize', resizeTable);
            nextTick(() => {window.dispatchEvent(new Event('resize'));});
        });
        onUnmounted(() => {
            window.removeEventListener('resize', resizeTable);
        });
        return { wrap };
    }
}
</script>
<style lang="stylus" scoped>
@media screen and (max-width: 1000px) {
    body .app-wrap{
        height:98%;
        width:98%;
    }
}

.app-wrap{
    box-sizing:border-box;
    background-color:#fff;
    border:1px solid #ccc;
    width:81rem;
    max-width : 98%;
    height:90%;
    position:absolute;
    left:50%;
    top:50%;
    transform:translateX(-50%) translateY(-50%);
    box-shadow: 0 8px 50px rgba(0,0,0, 0.15);
    border-radius: 0.5rem;
}
html.dark .app-wrap{
    background-color:#141414;
    border-color:#575c61;
    box-shadow: 0 8px 50px rgba(34, 197, 94, 0.1);
}
</style>
