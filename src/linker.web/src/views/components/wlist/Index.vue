<template>
    <a @click="state.showManager = true" href="javascript:;" class="mgr-1 a-line">{{$t('server.wlist')}}</a>
    <Manager v-if="state.showManager" v-model="state.showManager" :type="state.type" />
</template>

<script>
import { reactive } from 'vue';
import Manager from './Manager.vue'
export default {
    props:['type'],
    components:{Manager},
    setup (props) {

        const state = reactive({
            showManager:false,
            type:props.type
        });
        return {state}
    }
}
</script>

<style lang="stylus" scoped>

</style>