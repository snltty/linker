<template>
   <div class="connect-point" @click="handleShow">
        <span :class="`connect-point ${state.className}`" :title="state.title" v-loading="state.connecting"></span>
   </div>
</template>

<script>
import {  computed, reactive } from 'vue';
import { useConnections } from './connections';
export default {
    props: ['row','transactionId'],
    setup (props) {
        const connections = useConnections();
        const connection = computed(()=>props.row.hook_connection?props.row.hook_connection[props.transactionId] || {} : {});
        const state = reactive({
            transactionId:props.transactionId,
            connecting:computed(()=>props.row.hook_operating?props.row.hook_operating[props.transactionId]:false),
            className:computed(()=>connection.value.Connected?['p2p','relay','node'][connection.value.Type]:'default'),
            title:computed(()=>connection.value.Connected?['打洞直连','中继连接','节点连接'][connection.value.Type]:'未连接'),
        });
        const handleShow = () => {
            connections.value.device = props.row;
            connections.value.transactionId = props.transactionId;
            connections.value.showEdit = true;
        }
        
        return {state,handleShow}
    }
}
</script>

<style lang="stylus">
.connect-point {
    .el-loading-mask{background-color:transparent}
    .el-loading-spinner{width:100%;height:100%;margin:0;top:0;}
    .el-loading-spinner .circular{width: 100%; height:100%;vertical-align:top;}
    .el-loading-spinner .path{stroke-width: 6;stroke: #008000;}
}
</style>
<style lang="stylus" scoped>

div.connect-point{
    margin: -.2rem .3rem 0 -1.4rem;
    position:absolute;
    z-index 9
}
span.connect-point {
    width: .9rem;
    height: .9rem;
    border-radius: 50%;
    display: inline-block;
    vertical-align: middle;
    background-color: #eee;
    border: 1px solid #ddd;
    cursor :pointer;
    transition:.3s;
    &:hover {
        transform: scale(2);
    }
}

span.connect-point.p2p {
    background-color: #01c901;
    border: 1px solid #049538;
}

span.connect-point.relay {
    background-color: #e3e811;
    border: 1px solid #b3c410;
}

span.connect-point.node {
    background-color: #09dda9;
    border: 1px solid #0cac90;
}
html.dark span.connect-point.default{background-color: #666;border-color:#888}

</style>