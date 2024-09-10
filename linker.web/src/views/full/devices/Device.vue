<template>
<el-table-column prop="MachineId" label="设备" width="240">
    <template #header>
        <div class="flex">
            <span class="flex-1">设备</span>
            <span> <el-input size="small" v-model="name" clearable @input="handleRefresh" placeholder="设备/虚拟网卡/端口转发" ></el-input> </span>
            <span>
                <el-button size="small" @click="handleRefresh"><el-icon><Search /></el-icon></el-button>
            </span>
        </div>
    </template>
    <template #default="scope">
        <div>
            <p>
                <DeviceName @edit="handleEdit" :config="true" :item="scope.row"></DeviceName>
            </p>
            <p class="flex">
                <template v-if="scope.row.showip">
                    <span title="此设备的外网IP" class="ipaddress" @click="handleExternal(scope.row)"><span>😀{{ scope.row.IP }}</span></span>
                </template>
                <template v-else>
                    <span title="此设备的外网IP" class="ipaddress" @click="handleExternal(scope.row)"><span>😴㊙.㊙.㊙.㊙</span></span>
                </template>
                <span class="flex-1"></span>
                <UpdaterBtn :config="true" :item="scope.row"></UpdaterBtn>
            </p>
        </div>
    </template>
</el-table-column>
</template>
<script>
import { computed, ref } from 'vue';
import {Search} from '@element-plus/icons-vue'
import UpdaterBtn from './UpdaterBtn.vue';
import DeviceName from './DeviceName.vue';
import { injectGlobalData } from '@/provide';

export default {
    emits:['edit','refresh'],
    components:{Search,UpdaterBtn,DeviceName},
    setup(props,{emit}) {

        const globalData = injectGlobalData();
        const hasExternal = computed(()=>globalData.value.hasAccess('ExternalShow')); 
        const name = ref(sessionStorage.getItem('search-name') || '');
        
        const handleExternal = (row)=>{
            if(!hasExternal.value) return;
            row.showip=!row.showip;
        }
        const handleEdit = (row)=>{
            emit('edit',row)
        }
        const handleRefresh = ()=>{
            sessionStorage.setItem('search-name',name.value);
            emit('refresh',name.value)
        }

        return {
             handleEdit,handleRefresh,name,handleExternal
        }
    }
}
</script>
<style lang="stylus" scoped>

.ipaddress{
    span{vertical-align:middle}
}

.el-input{
    width:15rem;
    margin-right:.6rem
}
</style>