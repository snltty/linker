<template>
    <div class="servers-wrap scrollbar" >
        <Config v-if="hasConfig"></Config>
    </div>
</template>
<script>
import { computed,  reactive } from 'vue';
import { injectGlobalData } from '@/provide';
import Config from './Config.vue';
export default {
    components:{Config},
    setup(props) {
        const globalData = injectGlobalData();
        const hasConfig = computed(()=>globalData.value.hasAccess('Config'))
        const state = reactive({});
        return {
            state,hasConfig
        }
    }
}
</script>
<style lang="stylus" scoped>
.servers-wrap{
    padding:1rem
    font-size:1.3rem;
    color:#555;
    a{color:#333;}
}
</style>