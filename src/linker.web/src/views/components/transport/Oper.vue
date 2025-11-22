<template>
    <template v-if="item.hook_counter">
        <AccessShow value="Transport">
            <el-col :span="12" class="skeleton-animation" :style="`animation-delay:${item.animationDelay}ms`">
                <a href="javascript:;" :class="{green:transportCounter>0}" @click="handleTransport"><img src="transport.svg" alt="transport"> ({{ transportCounter }})</a>
            </el-col>
        </AccessShow>
    </template>
    <template v-else-if="!item.hook_counter_load">
        <el-col :span="12">
            <el-skeleton animated >
                <template #template>
                    <el-skeleton-item variant="text" style="vertical-align: middle;width: 50%;"/>
                </template>
            </el-skeleton>
        </el-col>
    </template>
</template>

<script>
import { computed } from 'vue';
import { useTransport } from './transport';

export default {
    props: ['item'],
    setup (props) {
        
        const transportCounter = computed(()=>props.item.hook_counter.transport || 0);
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