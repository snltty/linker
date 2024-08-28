<template>
    <div>
        <template v-if="tuntap.list[item.MachineId] && tuntap.list[item.MachineId].system">
            <span :title="tuntap.list[item.MachineId].SystemInfo">
                <img v-if="item.countryFlag" class="system" :src="item.countryFlag" />
                <img class="system":src="`/${tuntap.list[item.MachineId].system}.svg`" />
                <img v-if="tuntap.list[item.MachineId].systemDocker" class="system" src="/docker.svg" />
            </span>
        </template>
        <a href="javascript:;" @click="handleEdit" title="此客户端的设备名" :class="{green:item.Connected}">{{item.MachineName }}</a>
        <strong v-if="item.isSelf"> - (<el-icon><StarFilled /></el-icon> 本机) </strong>
    </div>
</template>

<script>
import { injectGlobalData } from '@/provide';
import { useTuntap } from './tuntap';
import {StarFilled} from '@element-plus/icons-vue'
import { computed } from 'vue';
export default {
    props:['item','config'],
    emits:['edit','refresh'],
    components:{StarFilled},
    setup (props,{emit}) {
        const tuntap = useTuntap();
        const globalData = injectGlobalData();
        const machineId = computed(() => globalData.value.config.Client.Id);
        const handleEdit = ()=>{
            if(!props.config && machineId.value != props.item.MachineId){
                return;
            }
            emit('edit',props.item)
        }


        return {
            item:props.item,tuntap,handleEdit
        }
    }
}
</script>

<style lang="stylus" scoped>
a{
    color:#666;
    text-decoration: underline;
    &.green{color:green;font-weight:bold;}
}

img.system{
    height:1.6rem;
    vertical-align: middle;
    margin-right:.4rem
}
</style>