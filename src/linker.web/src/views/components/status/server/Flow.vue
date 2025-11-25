<template>
    <div class="flow-wrap" v-if="config">
        <p>{{$t('status.flowOnline')}} 
            <a href="javascript:;" @click="flow.map=true" :title="`${$t('status.flowThisServer')}\r\n${$t('status.flowOnline')}/${$t('status.flowOnline7Day')}`">{{flow.overallOnline}}</a>
            <a href="javascript:;" @click="flow.allmap=true" :title="`${$t('status.flowAllServer')}\r\n${$t('status.flowOnline')}/${$t('status.flowOnline7Day')}/${$t('status.flowServer')}`">{{ flow.serverOnline }}</a>
        </p>
        <p>{{$t('status.flowUpload')}} <a href="javascript:;"  @click="flow.count = true" :title="`${$t('status.flowThisServer')}\r\n${$t('status.flowAllSend')}`">{{flow.overallSendtSpeed}}</a></p>
        <p>{{$t('status.flowDownload')}} <a href="javascript:;"  @click="flow.count = true" :title="`${$t('status.flowThisServer')}\r\n${$t('status.flowAllReceive')}`">{{flow.overallReceiveSpeed}}</a></p>
    </div>
    <Flow :config="config" title="服务器"></Flow>
</template>

<script>
import Flow from '../../flow/Index.vue'
import { provideFlow } from '../../flow/flow';
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
html.dark .flow-wrap{
    background-color: #242526;
    border-color: #575c61;
}
.flow-wrap{
    padding:.4rem;
    font-weight:bold;position:absolute;right:0.5rem;bottom:80%;
    border:1px solid #ddd;
    background-color:#fff;
    z-index :9
    &>a,&>p{
        line-height:normal;
        white-space: nowrap;
        display:block;
    }
}
</style>