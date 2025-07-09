<template>
    <div class="app-wrap flex flex-column flex-nowrap" id="app-wrap">
        <div class="head">
            <Head></Head>
        </div>
        <div class="body flex-1 relative" ref="wrap">
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
import Head from '@/views/full/Head.vue'
import Status from '@/views/full/status/Index.vue'
import Install from '@/views/full/install/Index.vue'
import { injectGlobalData } from '@/provide';
import { nextTick, onMounted, onUnmounted, ref } from 'vue';
import { useRouter } from 'vue-router';
export default {
    name: 'Index',
    components: {Head, Status, Install},
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
            resizeTable();

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
        width:100%;
        height:100%;
        position:absolute;
        left:0;
        top:0;
        right:auto;
        bottom:auto;
        height:100%;
        width:100%;
        transform:none;
        max-width:100%;
        border:0;
    }
}
.app-wrap{
    box-sizing:border-box;
    background-color:#fff;
    border:1px solid #d0d7de;
    width:81rem;
    max-width : 80%;
    height:90%;
    position:absolute;
    left:50%;
    top:50%;
    transform:translateX(-50%) translateY(-50%);
}
html.dark .app-wrap{
    background-color:#141414;
    border-color:#575c61;
}
</style>
