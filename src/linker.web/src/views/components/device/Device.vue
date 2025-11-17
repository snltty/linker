<template>
<el-table-column prop="MachineId" :label="$t('home.device')" width="180">
    <template #header>
        <div class="flex">
            <span>{{$t('home.device')}}</span>
            <span class="flex-1"> <el-input v-trim size="small" v-model="name" clearable @input="handleRefresh" class="w-100" ></el-input> </span>
        </div>
    </template>
    <template #default="scope">
        <div>
            <p>
                <DeviceName :config="true" :item="scope.row"></DeviceName>
            </p>
            <p class="flex">
                <template v-if="scope.row.Connected">
                    <SystemInfo :item="scope.row"></SystemInfo>
                    <span class="flex-1"></span>
                    <WlistShow type="Relay" :item="scope.row"></WlistShow>
                    <UpdaterBtn :config="true" :item="scope.row"></UpdaterBtn>
                </template>
                <template v-else>
                    <span>{{ scope.row.LastSignIn }}</span>
                </template>
            </p>
        </div>
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
</style>