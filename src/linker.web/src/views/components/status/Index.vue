<template>
    <div class="status-wrap flex">
        <div class="copy flex">
            <a href="javascript:;" class="memory" :title="$t('status.support')" @click="state.showPay = true">
            <!-- <a href="https://afdian.com/a/snltty" class="memory" :title="$t('status.support')" target="_blank"> -->
                <img src="@/assets/dianchi.svg" alt="memory" />
                <span>{{$t('status.support')}}</span>
            </a>
            <a href="javascript:;">©linker {{ self.Version }}</a>
            <a v-if="globalData.isPc" href="https://github.com/snltty/linker" target="_blank">Github</a>
            <a v-if="globalData.isPc" href="https://linker.snltty.com" target="_blank">{{$t('status.website')}}</a>
            <a v-if="globalData.isPc" href="https://linker-doc.snltty.com" target="_blank">{{$t('status.doc')}}</a>
        </div>
        <div class="flex-1"></div>
        <div class="export"><Export :config="config"></Export></div>
        <div class="api" v-if="globalData.isPc"><Api :config="config"></Api></div>
        <div class="server"><Server :config="config"></Server></div>

        <el-dialog v-model="state.showPay" :title="$t('status.support')" width="80%">
            <div class="pay">
                <p class="t-c">
                    <a href="https://afdian.com/a/snltty" class="memory a-line" :title="$t('status.support')" target="_blank">
                        <img src="@/assets/dianchi.svg" alt="memory" />
                        <span>{{$t('status.support')}}</span>
                    </a>
                </p>
                <p class="t-c">
                    OR
                </p>
                <p>
                    <img src="@/assets/pay.jpg" alt="pay" width="100%"/>
                </p>
            </div>
        </el-dialog>
    </div>
</template>
<script>
import { computed, reactive, ref } from 'vue';
import Api from './Api.vue'
import Server from './server/Index.vue'
import Export from './Export.vue'
import UpdaterBtn from '../updater/UpdaterBtn.vue';
import { injectGlobalData } from '@/provide';
export default {
    components:{Api,Server,Export,UpdaterBtn},
    props:['config'],
    setup(props) {
        const globalData = injectGlobalData();
        const self = computed(()=>globalData.value.self); 

        const state = reactive({
            showPay:false
        });
        return {
            globalData,state,config:props.config,self
        }
    }
}
</script>
<style lang="stylus" scoped>
html.dark .status-wrap{background-color:#242526;border-color:#575c61;}
html.dark .status-wrap .copy a{color:#ccc;}
.status-wrap{
    border-top:1px solid #ddd;
    background-color:#f5f5f5;
    height:3rem;
    line-height:3rem;
    font-size:1.2rem;
    color:#555;
    border-radius:0 0 .5rem .5rem;

    .copy{
        padding-left:.5rem;
        a{color:#555;margin-right:1rem}
    }

    a.memory{
        img{height:2rem;vertical-align:sub;margin-right:.1rem;}
        margin-right:.6rem;
    }
}
</style>