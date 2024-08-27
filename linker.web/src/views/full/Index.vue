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
import Head from '@/components/full/Head.vue'
import Status from '@/components/full/status/Index.vue'
import Install from '@/components/full/install/Index.vue'
import { provideGlobalData } from '@/provide';
import { nextTick, onMounted, onUnmounted, ref } from 'vue';
export default {
    name: 'Index',
    components: {Head, Status, Install},
    setup(props) {
        const globalData = provideGlobalData();

        const wrap = ref(null);
        const resizeTable = () => {
            nextTick(() => {
                globalData.value.height = wrap.value.offsetHeight;
            });
        }
        onMounted(() => {
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
