<template>
    <el-dropdown-item @click="handleStopwatch"><el-icon><Platform /></el-icon>{{$t('home.messenger')}}</el-dropdown-item>
    <AccessShow value="Flow">
        <el-dropdown-item @click="handleFlow"><el-icon><Histogram /></el-icon>{{$t('home.flowStatis')}}</el-dropdown-item>
    </AccessShow>
</template>

<script>
import { Platform,Histogram } from '@element-plus/icons-vue';
import { useFlow } from './flow';
export default {
    props:['item'],
    components:{Platform,Histogram},
    setup (props) {
        
        const flow = useFlow();
        const handleStopwatch = ()=>{

            flow.value.device.id = props.item.MachineId;
            flow.value.device.name = props.item.MachineName;
            flow.value.showStopwatch = true;
        }
        
        const handleFlow = ()=>{
            flow.value.device.id = props.item.MachineId;
            flow.value.device.name = props.item.MachineName;
            flow.value.show = true;
        }

        return {handleStopwatch,handleFlow}
    }
}
</script>

<style lang="stylus" scoped>

</style>