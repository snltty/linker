<template>
   <div class="point" @click="handleShow">
        <template v-if="state.connection && state.connection.Connected">
            <template v-if="state.connection.Type == 0">
                <span class="point p2p" title="打洞直连"></span>
            </template>
            <template v-else-if="state.connection.Type == 1">
                <span class="point relay" title="中继连接"></span>
            </template>
            <template v-else-if="state.connection.Type == 2">
                <span class="point node" title="节点连接"></span>
            </template>
        </template>
        <template v-else>
            <span class="point" title="未连接"></span>
        </template>
   </div>
</template>

<script>
import { reactive, watch } from 'vue';
import { useConnections } from './connections';
export default {
    props: ['data','row'],
    setup (props) {

        const connections = useConnections();
        const state = reactive({
            connection:props.data
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

<style lang="stylus" scoped>
div.point{
    margin: -.2rem .3rem 0 -1.3rem;
    position:absolute;
}
span.point {
    width: .8rem;
    height: .8rem;
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

span.point.p2p {
    background-color: #01c901;
    border: 1px solid #049538;
}

span.point.relay {
    background-color: #e3e811;
    border: 1px solid #b3c410;
}

span.point.node {
    background-color: #09dda9;
    border: 1px solid #0cac90;
}

</style>