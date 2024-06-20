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
import { computed } from 'vue'
import { provideGlobalData } from './provide'
export default {
    components: { Head, Device },
    setup() {

        const globalData = provideGlobalData();
        const showList = computed(() => !!globalData.value.username && globalData.value.connected);

        return {
            showList
        }
    }
}
</script>

<style lang="stylus" scoped>
// @media (min-width: 768px) {
// .main-wrap {
// background-color: #1c77ac !important;
// background-image: none !important;
// // background-image: url('../assets/bg.jpg') !important;
// }
// }
@media (min-width: 768px) {
    .main-wrap {
        max-width: 39rem;
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
    background-color: #333;
    background-image: url('../assets/bg4.jpg');
    background-size: cover;
    background-position: center top;

    .body {
        position: relative;
        // background-color: #fafafa;
    }
}
</style>
