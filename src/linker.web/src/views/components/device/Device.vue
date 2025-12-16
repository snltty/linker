<template>
<el-table-column prop="MachineId" :label="$t('home.device')" width="180">
    <template #header>
        <div class="flex">
            <span>{{$t('home.device')}}</span>
            <span class="flex-1"> <el-input v-trim size="small" v-model="name" clearable @input="handleRefresh" class="w-100" ></el-input> </span>
        </div>
    </template>
    <template #default="scope">
        <template v-if="scope.row">
            <p>
                <DeviceName :config="true" :item="scope.row"></DeviceName>
            </p>
            <p class="flex">
                <template v-if="scope.row.Connected">
                    <SystemInfo :item="scope.row"></SystemInfo>
                    <WlistShow type="Relay" :item="scope.row"></WlistShow>
                    <UpdaterBtn :config="true" :item="scope.row"></UpdaterBtn>
                </template>
                <template v-else-if="scope.row.LastSignIn">
                    <span>{{ scope.row.LastSignIn }}-{{ scope.row.Version }}</span>
                </template>
                <template v-else>
                    <el-skeleton animated >
                        <template #template>
                            <div class="flex">
                                <el-skeleton-item variant="text" class="el-skeleton-item" />
                                <el-skeleton-item variant="text" class="el-skeleton-item" />
                                <span class="flex-1"></span>
                                <el-skeleton-item variant="text" class="el-skeleton-item" />
                            </div>
                        </template>
                    </el-skeleton>
                </template>
            </p>
        </template>
        <div class="device-remark"></div>
    </template>
</el-table-column>
</template>
<script>
import { ref } from 'vue';
import {Search} from '@element-plus/icons-vue'
import UpdaterBtn from '../updater/UpdaterBtn.vue';
import DeviceName from './DeviceName.vue';
import SystemInfo from '../tuntap/SystemInfo.vue'; 
import WlistShow from '../wlist/Device.vue'


export default {
    emits:['refresh'],
    components:{Search,UpdaterBtn,DeviceName,SystemInfo,WlistShow},
    setup(props,{emit}) {

        const name = ref(sessionStorage.getItem('search-name') || '');
        
        const handleRefresh = ()=>{
            sessionStorage.setItem('search-name',name.value);
            emit('refresh',name.value)
        }

        return {
            name, handleRefresh
        }
    }
}
</script>
<style lang="stylus" scoped>
.el-input{
    width:12rem;
    margin-right:.6rem
}
.el-skeleton-item{
    vertical-align: middle;width: 20%;
}
</style>