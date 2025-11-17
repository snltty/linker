<template>
    <AccessBoolean value="FirewallSelf,FirewallOther">
        <template #default="{values}">
            <template v-if="(values.FirewallSelf && item.isSelf) || (values.FirewallOther && !item.isSelf)">
                <el-col :span="12">
                    <a href="javascript:;" :class="{green:firewallCounter>0}" @click="handleFirewall"><img src="firewall.svg" alt="firewall"> ({{firewallCounter}})</a>
                </el-col>
            </template>
        </template>
    </AccessBoolean>
</template>

<script>
import { computed } from 'vue';
import { useFirewall } from './firewall';

export default {
    props: ['item'],
    setup (props) {
        
        const firewallCounter = computed(()=>(props.hook_counter || {firewall:0}).firewall || 0);
        const firewall = useFirewall();

        const handleFirewall = ()=>{
            firewall.value.device.id = props.item.MachineId;
            firewall.value.device.name = props.item.MachineName;
            firewall.value.show = true;
        }

        return {firewallCounter,handleFirewall}
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