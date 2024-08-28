<template>
    <div class="net-wrap app-wrap">
        <div class="inner absolute flex flex-column flex-nowrap">
            <div class="head">
                <Head></Head>
            </div>
            <div class="body flex-1 relative">
                <List></List>
            </div>
            <div class="status">
                <Status :config="false"></Status>
            </div>
        </div>
    </div>
</template>

<script>
import { provideGlobalData } from '@/provide';
import Head from './Head.vue';
import List from './List.vue';
import Status from '@/components/full/status/Index.vue'
export default {
    components:{Head,List,Status},
    setup () {
        provideGlobalData();
        
        document.addEventListener('contextmenu', function(event) {
            event.preventDefault();
        });

        return {}
    }
}
</script>

<style lang="stylus" scoped>
.net-wrap{
    box-sizing:border-box;
    background-color:#fafafa;
    width:100%;
    max-width : 39rem;
    height:100%;
    position:absolute;
    left:50%;
    top:50%;
    transform:translateX(-50%) translateY(-50%);
}
</style>