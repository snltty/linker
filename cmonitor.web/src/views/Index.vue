<template>
    <div class="main-wrap flex flex-column flex-nowrap">
        <div class="head">

            <Head></Head>
        </div>
        <div class="body flex-1 scrollbar" v-if="showList">
            <Device></Device>
        </div>
    </div>
</template>

<script>
import Head from './Head.vue'
import Device from './device/Index.vue'
import { subWebsocketState } from '../apis/request'
import { computed } from 'vue'
import { provideGlobalData } from './provide'
export default {
    components: { Head, Device },
    setup() {

        const globalData = provideGlobalData();
        subWebsocketState((state) => {
            globalData.value.connected = state;
        })
        const showList = computed(() => !!globalData.value.username && globalData.value.connected);

        return {
            showList
        }
    }
}
</script>

<style lang="stylus" scoped>
@media (min-width: 768px) {
    .main-wrap {
        border: 2px solid #d0d7de;
        height: 90% !important;
        width: 390px !important;
    }
}

.main-wrap {
    width: 100%;
    position: absolute;
    left: 50%;
    top: 50%;
    transform: translateX(-50%) translateY(-50%);
    background-color: #fff;
    height: 100%;

    .body {
        position: relative;
        background-color: #fafafa;
    }
}
</style>
