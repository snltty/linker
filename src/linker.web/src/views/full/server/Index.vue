<template>
    <div class="servers-wrap" >
        <SignInServers v-if="hasConfig"></SignInServers>
    </div>
</template>
<script>
import { computed,  reactive } from 'vue';
import { injectGlobalData } from '@/provide';
import SignInServers from './SignInServers.vue';
export default {
    components:{SignInServers},
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