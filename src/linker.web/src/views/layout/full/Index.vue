<template>
    <div class="app-wrap flex flex-column flex-nowrap" id="app-wrap">
        <template v-if="configed">
            <div class="head">
                <Head></Head>
            </div>
            <div class="adv">
                <Adv></Adv>
            </div>
            <div class="body flex-1 relative" id="main-body">
                <div class="home absolute">
                    <AccessBoolean value="NetManager,FullManager">
                        <template #default="{values}">
                            <template v-if="$route.path.indexOf('/full/')== 0 && values.FullManager == false">
                                <NoPermission></NoPermission>
                            </template>
                            <template v-else><router-view /></template>
                        </template>
                    </AccessBoolean>
                </div>
            </div>
            <div class="status">
                <Status :config="true"></Status>
                <Install></Install>
            </div>
        </template>
        <template v-else>
            <el-skeleton animated class="h-100">
                <template #template>
                    <div class="h-100 flex flex-column flex-nowrap">
                        <div style="padding:0 0 1rem 0rem;"><el-skeleton-item style="height:5rem;"/></div>
                        <div id="main-body" style="padding:0 1rem 0rem 1rem;" class="flex-1"><el-skeleton-item style="height: 100%;" /></div>
                        <div style="padding: 1rem 0 0 0;font-size:0"><el-skeleton-item style="height: 3rem;"/></div>
                    </div>
                </template>
            </el-skeleton>
        </template>
    </div>
</template>

<script>
import Head from './head/Index.vue'
import Status from '../../components/status/Index.vue'
import Install from './install/Index.vue'
import { computed} from 'vue';
import Adv from '../../components/adv/Index.vue'
import { injectGlobalData } from '@/provide';
import NoPermission from '../../NoPermission.vue';
export default {
    name: 'Index',
    components: {Head, Status, Install,Adv,NoPermission},
    setup() {

        const globalData = injectGlobalData();
        const configed = computed(()=>globalData.value.config.configed);
        return {configed };
    }
}
</script>
<style lang="stylus" scoped>
@media screen and (max-width: 1000px) {
    body .app-wrap{
        height:98%;
        width:98%;
    }
}

.app-wrap{
    box-sizing:border-box;
    background-color:#fff;
    border:1px solid #ccc;
    width:81rem;
    max-width : 98%;
    height:90%;
    position:absolute;
    left:50%;
    top:50%;
    transform:translateX(-50%) translateY(-50%);
    box-shadow: 0 8px 50px rgba(0,0,0, 0.15);
    border-radius: 0.5rem;
}
html.dark .app-wrap{
    background-color:#141414;
    border-color:#575c61;
    box-shadow: 0 8px 50px rgba(34, 197, 94, 0.1);
}
</style>
