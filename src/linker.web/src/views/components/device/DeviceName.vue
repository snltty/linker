<template>
    <AccessBoolean value="RenameSelf,RenameOther">
        <template #default="{values}">
            <div>
                <a href="javascript:;" @click="handleEdit(values)" title="此客户端的设备名" class="a-line">
                    <strong class="gateway" :class="{green:item.Connected}">{{item.MachineName || 'null' }}</strong>
                </a>
                
                <strong class="self gateway" v-if="item.isSelf">(<el-icon size="16"><StarFilled /></el-icon>)</strong>
                <template v-if="tuntap.list[item.MachineId] && tuntap.list[item.MachineId].systems">
                    <template v-for="system in tuntap.list[item.MachineId].systems">
                        <span :title="tuntap.list[item.MachineId].SystemInfo">
                            <img class="system" :src="`./${system}.svg`" />
                        </span>
                    </template>
                </template>
            </div>
        </template>
    </AccessBoolean>
</template>

<script>
import { injectGlobalData } from '@/provide';
import { useTuntap } from '../tuntap/tuntap';
import {StarFilled} from '@element-plus/icons-vue'
import { computed } from 'vue';
import { ElMessage } from 'element-plus';
import { useDevice } from './devices';
export default {
    props:['item','config'],
    components:{StarFilled},
    setup (props) {
        
        const devices = useDevice();
        const tuntap = useTuntap();
        
        const globalData = injectGlobalData();
        const machineId = computed(() => globalData.value.config.Client.Id);
        const handleEdit = (access)=>{
            if(!props.config){
                return;
            }
            if(machineId.value === props.item.MachineId){
                if(!access.RenameSelf){
                    ElMessage.success('无权限');
                    return;
                }
            }else{
                if(!access.RenameOther){
                    ElMessage.success('无权限');
                    return;
                }
            }
            devices.deviceInfo = props.item;
            devices.showDeviceEdit = true;
        }


        return {
            item:computed(()=>props.item),
            tuntap,handleEdit
        }
    }
}
</script>

<style lang="stylus" scoped>
img.system{
    height:1.6rem;
    vertical-align: sub;
    margin-left:.4rem
}
.self{
    color:#d400ff;
    .el-icon{vertical-align: text-bottom;}
}

</style>