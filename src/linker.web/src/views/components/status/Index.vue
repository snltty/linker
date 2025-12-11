<template>
    <div class="status-wrap flex">
        <div class="copy flex">
            <Support></Support>
            <a href="javascript:;">©linker {{ self.Version }}</a>
            <PcShow>
                <a href="https://github.com/snltty/linker" target="_blank">Github</a>
                <a href="https://linker.snltty.com" target="_blank">{{$t('status.website')}}</a>
                <a href="https://linker-doc.snltty.com" target="_blank">{{$t('status.doc')}}</a>
            </PcShow>
        </div>
        <div class="flex-1"></div>
        <PcShow>
            <div class="api"><Api :config="config"></Api></div>
        </PcShow>
        <div class="server"><Server :config="config"></Server></div>
    </div>
</template>
<script>
import { computed, reactive } from 'vue';
import Api from './Api.vue'
import Server from './server/Index.vue'
import UpdaterBtn from '../updater/UpdaterBtn.vue';
import { injectGlobalData } from '@/provide';
import Support from './Support.vue';
export default {
    components:{Api,Server,UpdaterBtn,Support},
    props:['config'],
    setup(props) {
        const globalData = injectGlobalData();
        const self = computed(()=>globalData.value.self); 

        const state = reactive({
        });
        return {
            state,config:props.config,self
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

    
}
</style>