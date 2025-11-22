<template>
    <template v-if="item.hook_counter">
        <AccessBoolean value="WakeupSelf,WakeupOther">
            <template #default="{values}">
                <template v-if="(values.WakeupSelf && item.isSelf) || (values.WakeupOther && !item.isSelf)">
                    <el-col :span="12" class="skeleton-animation" :style="`animation-delay:${item.animationDelay}ms`">
                        <a href="javascript:;" :class="{green:wakeupCounter>0}" @click="handleWakeup"><img src="wakeup.svg" alt="wakeup"> ({{ wakeupCounter }})</a>
                    </el-col>
                </template>
            </template>
        </AccessBoolean>
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
import { useWakeup } from './wakeup';

export default {
    props:['item'],
    setup (props) {

        const wakeupCounter = computed(()=>props.item.hook_counter.wakeup || 0);
        const wakeup = useWakeup();
        const handleWakeup = ()=>{
            wakeup.value.device.id = props.item.MachineId;
            wakeup.value.device.name = props.item.MachineName;
            wakeup.value.show = true;
        }
        return {wakeupCounter,handleWakeup}
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