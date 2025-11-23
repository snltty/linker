<template>
    <div class="net-wrap app-wrap">
        <div class="inner absolute flex flex-column flex-nowrap">
            <template v-if="configed">
                <div class="head">
                    <Head></Head>
                </div>
                <div class="body flex-1 relative" id="main-body">
                    <AccessBoolean value="NetManager,FullManager">
                        <template #default="{values}">
                            <template v-if="$route.path.indexOf('/net/')== 0 && values.NetManager == false">
                                <NoPermission></NoPermission>
                            </template>
                            <template v-else> <List></List></template>
                        </template>
                    </AccessBoolean>
                </div>
                <div class="status">
                    <Status :config="false"></Status>
                </div>
            </template>
            <template v-else>
                <el-skeleton animated class="h-100">
                    <template #template>
                        <div class="h-100 flex flex-column flex-nowrap">
                            <div style="padding: 1rem;"><el-skeleton-item style="height:4.5rem;"/></div>
                            <div id="main-body" style="padding:0 1rem 0rem 1rem;" class="flex-1"><el-skeleton-item style="height: 100%;" /></div>
                            <div style="padding: 1rem;"><el-skeleton-item style="height: 3rem;"/></div>
                        </div>
                    </template>
                </el-skeleton>
            </template>
        </div>
    </div>
</template>

<script>
import { injectGlobalData } from '@/provide';
import Head from './Head.vue';
import List from './List.vue';
import Status from '@/views/components/status/Index.vue'
import { computed } from 'vue';
import NoPermission from '../../NoPermission.vue';
export default {
    components:{Head,List,Status,NoPermission},
    setup () {
        document.addEventListener('contextmenu', function(event) {
            event.preventDefault();
        });
        const globalData = injectGlobalData();
        const configed = computed(()=>globalData.value.config.configed);

        return {configed}
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