<template>
      <AccessShow value="ActionOther">
            <el-dropdown-item v-if="item.isSelf==false" @click="handleAction"><el-icon><Lock /></el-icon>{{$t('home.action')}}</el-dropdown-item>
        </AccessShow>
</template>

<script>
import { useAction } from './action';
import { Lock } from '@element-plus/icons-vue'
export default {
    props: ['item'],
    components:{ Lock },

    setup (props) {
        
        const action = useAction();
        const handleAction = ()=>{
            action.value.device.id = props.item.MachineId;
            action.value.device.name = props.item.MachineName;
            action.value.show = true;
        }

        return {handleAction}
    }
}
</script>

<style lang="stylus" scoped>

</style>