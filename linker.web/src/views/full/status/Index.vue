<template>
    <div class="status-wrap flex">
        <div class="copy">
            <a href="javascript:;" class="memory" title="赞助一笔，让作者饱餐一顿" @click="state.showPay = true">
                <img src="@/assets/coin.svg" alt="memory" />
                <span>各位老板行行好</span>
            </a>
            <a href="https://github.com/snltty/linker" target="_blank">©linker {{ self.Version }}</a>
        </div>
        
        <div class="flex-1"></div>
        <div class="export"><Export :config="config"></Export></div>
        <div class="api"><Api :config="config"></Api></div>
        <div class="server"><Server :config="config"></Server></div>

        <el-dialog v-model="state.showPay" title="赞助linker" width="300" top="1vh">
            <div class="pay">
                <img src="@/assets/wechat.jpg" alt=""/>
                <img src="@/assets/alipay.jpg" alt=""/>
            </div>
        </el-dialog>
    </div>
</template>
<script>
import { computed, reactive } from 'vue';
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
            state,config:props.config,self
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
        a{color:#555;}
    }

    a.memory{
        img{height:3rem;vertical-align:bottom;margin-right:.1rem;}
        margin-right:.6rem;
    }
}
</style>