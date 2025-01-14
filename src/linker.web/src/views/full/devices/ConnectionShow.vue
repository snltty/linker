<template>
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
</template>

<script>
import { reactive, watch } from 'vue';

export default {
    props: ['data'],
    setup (props) {

        const state = reactive({
            connection:props.data
        });
        watch(()=>props.data,()=>{
            state.connection = props.data
        })
        
        return {state}
    }
}
</script>

<style lang="stylus" scoped>
span.point {
    width: .8rem;
    height: .8rem;
    border-radius: 50%;
    display: inline-block;
    vertical-align: middle;
    margin: -.2rem .3rem 0 -1.3rem;
    background-color: #eee;
    border: 1px solid #ddd;
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