<template>
    <AccessShow value="Transport">
        <el-col :span="12" v-if="!item.isSelf">
            <a href="javascript:;" :class="{green:transportCounter>0}" @click="handleTransport"><img src="transport.svg" alt="transport"> ({{ transportCounter }})</a>
        </el-col>
    </AccessShow>
</template>

<script>
import { computed } from 'vue';
import { useDecenter } from '../decenter/decenter';
import { useTransport } from './transport';

export default {
    props: ['item'],
    setup (props) {
        
        const decenter = useDecenter()
        const transportCounter = computed(()=>(decenter.value.list.transport || {})[props.item.MachineId] || 0);
        const transport = useTransport();
         const handleTransport = (ow)=>{
            transport.value.device.id = props.item.MachineId;
            transport.value.device.name = props.item.MachineName;
            transport.value.show = true;
        }
        return {transportCounter,handleTransport}
    }
}
</script>

<style lang="stylus" scoped>
a{
    display:inline-block; line-height:1.6rem;
    img{
        vertical-align:bottom;
        height:1.4rem;
    }
    &.green{
        font-weight:bold;
    }
}
</style>