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
import { onMounted } from 'vue';
import Head from './Head.vue';
import List from './List.vue';
import Status from '@/views/components/status/Index.vue'
import { injectGlobalData } from '@/provide';
import { useRouter } from 'vue-router';
export default {
    components:{Head,List,Status},
    setup () {
        document.addEventListener('contextmenu', function(event) {
            event.preventDefault();
        });

        const globalData = injectGlobalData();
        const router = useRouter();
        onMounted(()=>{

            if(globalData.value.hasAccess('NetManager') == false){
                router.push({name:'NoPermission'});
            }

        })

        return {}
    }
}
</script>
<style lang="stylus" scoped>
.net-wrap{
    box-sizing:border-box;
    background-color:#fafafa;
    border:1px solid #d0d7de;
    width:calc(100% - 40px);
    height:calc(100% - 40px);
    position:absolute;
    left:20px;
    top:20px;
}
</style>