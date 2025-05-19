<template>
    <div class="status-wrap flex">
        <div class="copy flex">
            <a href="javascript:;" class="memory" :title="$t('status.support')" @click="state.showPay = true">
                <img src="@/assets/coin.svg" alt="memory" />
                <span>{{$t('status.support')}}</span>
            </a>
            <a href="javascript:;">©linker {{ self.Version }}</a>
            <a v-if="globalData.isPc" href="https://github.com/snltty/linker" target="_blank">Github</a>
            <a v-if="globalData.isPc" href="https://linker.snltty.com" target="_blank">{{$t('status.website')}}</a>
            <a v-if="globalData.isPc" href="https://linker-doc.snltty.com" target="_blank">{{$t('status.doc')}}</a>
            <a v-if="globalData.isPc" href="https://v.netzo123.com" target="_blank">{{$t('status.cdkey')}}</a>
        </div>
        <div class="flex-1"></div>
        <div class="export"><Export :config="config"></Export></div>
        <div class="api" v-if="globalData.isPc"><Api :config="config"></Api></div>
        <div class="server"><Server :config="config"></Server></div>

        <el-dialog v-model="state.showPay" :title="$t('status.support')" width="400">
            <div class="pay">
                <img src="@/assets/qr.jpg" alt=""/>
            </div>
        </el-dialog>
    </div>
</template>
<script>
import { computed, reactive, ref } from 'vue';
import Api from './Api.vue'
import Server from './server/Index.vue'
import Export from './Export.vue'
import UpdaterBtn from '@/views/full/devices/UpdaterBtn.vue';
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
.status-wrap{
    border-top:1px solid #ddd;
    background-color:#f5f5f5;
    height:3rem;
    line-height:3rem;
    font-size:1.2rem;
    color:#555;

    .pay{
        font-size:xxx-large;
        img{width:100%;margin:0;}
    }
    

    .copy{
        padding-left:.5rem;
        a{color:#555;margin-right:1rem}
    }

    a.memory{
        img{height:3rem;vertical-align:bottom;margin-right:.1rem;}
        margin-right:.6rem;
    }
}
</style>