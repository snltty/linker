<template>
    <template v-if="item.hook_counter">
        <AccessBoolean value="FirewallSelf,FirewallOther">
            <template #default="{values}">
                <template v-if="(values.FirewallSelf && item.isSelf) || (values.FirewallOther && !item.isSelf)">
                    <el-col :span="12" class="skeleton-animation" :style="`animation-delay:${item.animationDelay}ms`">
                        <a href="javascript:;" :class="{green:firewallCounter>0}" @click="handleFirewall"><img src="firewall.svg" alt="firewall"> ({{firewallCounter}})</a>
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
import { useFirewall } from './firewall';

export default {
    props: ['item'],
    setup (props) {
        const firewallCounter = computed(()=>props.item.hook_counter.firewall || 0);
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