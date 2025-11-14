<template>
    <AccessBoolean value="WakeupOther">
        <template #default="{values}">
            <template v-if="values.WakeupOther && !item.isSelf">
                <el-col :span="12">
                    <a href="javascript:;" :class="{green:wakeupCounter>0}" @click="handleWakeup"><img src="wakeup.svg" alt="wakeup"> ({{ wakeupCounter }})</a>
                </el-col>
            </template>
        </template>
    </AccessBoolean>
</template>

<script>
import { computed } from 'vue';
import { useDecenter } from '../decenter/decenter';
import { useWakeup } from './wakeup';

export default {
    props:['item'],
    setup (props) {

        const decenter = useDecenter()
        const wakeupCounter = computed(()=>(decenter.value.list.wakeup || {})[props.item.MachineId] || 0);
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