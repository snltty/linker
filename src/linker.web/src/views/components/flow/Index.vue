<template>
    <div class="flow-wrap flex" v-if="config">
        <a href="javascript:;" class="a-line flex-1 t-c" @click="flow.map=true" :title="`${$t('flow.thisServer')}\r\n${$t('flow.online')}/${$t('flow.online.7day')}`">{{flow.overallOnline}}</a>
        <a href="javascript:;" class="a-line flex-1 t-c" @click="flow.allmap=true" :title="`${$t('flow.allServer')}\r\n${$t('flow.online')}/${$t('flow.online.7day')}/${$t('flow.server')}`">{{ flow.serverOnline }}</a>
        <a href="javascript:;" class="a-line flex-1 t-c" @click="flow.count = true" :title="`${$t('flow.thisServer')}\r\n${$t('flow.allsend')}`">↑{{flow.overallSendtSpeed}}</a>
        <a href="javascript:;" class="a-line flex-1 t-c" @click="flow.count = true" :title="`${$t('flow.thisServer')}\r\n${$t('flow.allrecv')}`">↓{{flow.overallReceiveSpeed}}</a>
    </div>
    <Flow :config="config" :title="$t('status.server')"></Flow>
</template>

<script>
import Flow from './Dialog.vue'
import { provideFlow } from './flow.js';
export default {
    props:['config'],
    components:{Flow},
    setup (props) {
        const {flow} = provideFlow();

        return {
            config:props.config,flow
        }
    }
}
</script>

<style lang="stylus" scoped>
.flow-wrap{
    width 40rem
    a{
         border-left:1px solid var(--table-border-color);
    }
}
</style>