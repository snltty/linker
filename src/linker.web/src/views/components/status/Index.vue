<template>
    <div class="status-wrap flex flex-center">
        <PcShow>
            <div class="api item flex flex-center"><Api :config="config"></Api></div>
        </PcShow>
        <div class="group item flex flex-center">
            <Groups :config="config"></Groups>
        </div>
        <div class="version item flex flex-center">
            <Version :config="config"></Version>
        </div>
        <div class="flex-1"></div>
        <PcShow>
            <AccessShow value="Flow">
                <div class="flow item flex-1">
                    <Flow v-if="config" :config="config"></Flow>
                </div>
            </AccessShow>
        </PcShow>
    </div>
</template>
<script>
import { computed, reactive } from 'vue';
import Api from '../ws/Index.vue'
import { injectGlobalData } from '@/provide';
import Groups from '../groups/Index.vue'
import Version from './Version.vue';
import Flow from '../flow/Index.vue';
export default {
    components:{Api,Groups,Version,Flow},
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
.status-wrap{
    border-top: 1px solid var(--header-border-color);
    background-color: var(--header-bg-color);
    height:3.4rem;
    line-height:3.4rem;
    font-size:1.2rem;
    color:#555;
    border-radius:0 0 .8rem .8rem;

    .item{
        border-left:1px solid var(--header-border-color);
        height 2rem;
        line-height:2rem;

        &:first-child{
            border:0;
        }
    }
    .api,.group,.version{padding:0 1rem;}
}
</style>