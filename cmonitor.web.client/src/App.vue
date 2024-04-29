<template>
    <div>
        <div class="app-wrap flex flex-column flex-nowrap">
            <div class="head"><Head></Head></div>
            <div class="body flex-1">
                <router-view/>
            </div>
            <div class="status">
                <Status></Status>
            </div>
        </div>
    </div>
</template>
<script>
import Head from './components/Head.vue'
import Status from './components/status/Index.vue'
import { provideGlobalData } from './provide';
export default{
    components:{Head,Status},
    setup(props) {
        provideGlobalData();
    }
}
</script>
<style lang="stylus" scoped>
.app-wrap{
    box-sizing:border-box;
    background-color:#fff;
    border:1px solid #d0d7de;
    width:80rem;
    max-width : 80%;
    height:90%;
    position:absolute;
    left:50%;
    top:50%;
    transform:translateX(-50%) translateY(-50%);
}
</style>
