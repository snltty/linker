<template>
    <template v-if="item.MachineName !== undefined">
        <AccessBoolean value="RenameSelf,RenameOther">
            <template #default="{values}">
                <a href="javascript:;" @click="handleEdit(item,values)" :title="item.IP" class="a-line">
                    <strong class="gateway" :class="{green:item.Connected}">{{item.MachineName || 'null' }}</strong>
                </a>
                <strong class="self gateway" v-if="item.isSelf">(<el-icon size="16"><StarFilled /></el-icon>)</strong>
                <strong class="hide gateway" v-if="item.isHide">(<el-icon size="16"><Hide /></el-icon>)</strong>
            </template>
        </AccessBoolean>
    </template>
    <template v-else>
        <el-skeleton animated >
            <template #template>
                <el-skeleton-item variant="text" style="vertical-align: middle;width: 50%;"/>
            </template>
        </el-skeleton>
    </template>
</template>

<script>
import { injectGlobalData } from '@/provide';
import {StarFilled,Hide} from '@element-plus/icons-vue'
import { computed } from 'vue';
import { ElMessage } from 'element-plus';
import { useDevice } from './devices';
export default {
    props:['item','config'],
    components:{StarFilled,Hide},
    setup (props) {
        
        const devices = useDevice();
        
        const globalData = injectGlobalData();
        const machineId = computed(() => globalData.value.config.Client.Id);
        const handleEdit = (item,access)=>{
            if(!item.MachineId) return;
            if(!props.config) return;
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
            handleEdit
        }
    }
}
</script>

<style lang="stylus" scoped>

.self{
    color:#d400ff;
    .el-icon{vertical-align: text-bottom;}
}
.hide{
    color:#000;
    .el-icon{vertical-align: text-bottom;}
}
strong{
    font-weight:bold;
}

</style>