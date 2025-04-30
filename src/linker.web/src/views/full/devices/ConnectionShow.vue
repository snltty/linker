<template>
   <div class="connect-point" @click="handleShow">
        <template v-if="state.connection && state.connection.Connected">
            <template v-if="state.connection.Type == 0">
                <span class="connect-point p2p" title="打洞直连" v-loading="state.connecting"></span>
            </template>
            <template v-else-if="state.connection.Type == 1">
                <span class="connect-point relay" title="中继连接" v-loading="state.connecting"></span>
            </template>
            <template v-else-if="state.connection.Type == 2">
                <span class="connect-point node" title="节点连接" v-loading="state.connecting"></span>
            </template>
        </template>
        <template v-else>
            <span class="connect-point" title="未连接" v-loading="state.connecting"></span>
        </template>
   </div>
</template>

<script>
import { computed, reactive, watch } from 'vue';
import { useConnections } from './connections';
import { useTunnel } from './tunnel';
export default {
    props: ['data','row'],
    setup (props) {

        const connections = useConnections();
        const tunnel = useTunnel();
        const state = reactive({
            connection:props.data,
            transitionId:props.transitionId,
            connecting:computed(()=>tunnel.value.p2pOperatings[props.row.MachineId] || tunnel.value.relayOperatings[props.row.MachineId])
        });
        watch(()=>props.data,()=>{
            state.connection = props.data
        });
        const handleShow = () => {
            connections.value.current = props.row.MachineId;
            connections.value.currentName = props.row.MachineName;
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

</style>