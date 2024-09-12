<template>
    <div class="app-wrap flex flex-column flex-nowrap">
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
        width:calc(100% - 40px);
        height:calc(100% - 40px);
        position:absolute;
        left:20px;
        top:20px;
        right:0;
        bottom:0;
        transform:none;
        max-width:calc(100% - 40px);
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
</style>
