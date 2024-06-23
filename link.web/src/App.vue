<template>
    <div>
        <div class="app-wrap flex flex-column flex-nowrap">
            <div class="head">

                <Head></Head>
            </div>
            <div class="body flex-1 relative" ref="wrap">
                <router-view />
            </div>
            <div class="status">
                <Status></Status>
                <Install></Install>
            </div>
        </div>
    </div>
</template>
<script>
import { nextTick, onMounted, onUnmounted, ref } from 'vue';
import Head from './components/Head.vue'
import Status from './components/status/Index.vue'
import Install from './components/install/Index.vue'
import { provideGlobalData } from './provide';
export default {
    components: { Head, Status, Install },
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
