<template>
<el-table-column prop="MachineId" :label="$t('home.device')" width="220">
    <template #header>
        <div class="flex">
            <span class="flex-1">{{$t('home.device')}}</span>
            <span> <el-input v-trim size="small" v-model="name" clearable @input="handleRefresh" ></el-input> </span>
            <span>
                <el-button size="small" @click="handleRefresh"><el-icon><Search /></el-icon></el-button>
            </span>
        </div>
    </template>
    <template #default="scope">
        <div>
            <p>
                <DeviceName :config="true" :item="scope.row"></DeviceName>
            </p>
            <p class="flex">
                <template v-if="scope.row.Connected">
                    <template v-if="scope.row.showip">
                        <span :title="$t('home.deviceWanIP')" class="ipaddress" @click="handleExternal(scope.row)"><span>😀{{ scope.row.IP }}</span></span>
                    </template>
                    <template v-else>
                        <span :title="$t('home.deviceWanIP')" class="ipaddress" @click="handleExternal(scope.row)"><span>😴㊙.㊙.㊙.㊙</span></span>
                    </template>
                    <span class="flex-1"></span>
                    <UpdaterBtn v-if="scope.row.showip == false" :config="true" :item="scope.row"></UpdaterBtn>
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
import { useI18n } from 'vue-i18n';

export default {
    emits:['refresh'],
    components:{Search,UpdaterBtn,DeviceName},
    setup(props,{emit}) {

        const t = useI18n();
        const name = ref(sessionStorage.getItem('search-name') || '');
        
        const handleExternal = (row)=>{
            row.showip=!row.showip;
        }
        const handleRefresh = ()=>{
            sessionStorage.setItem('search-name',name.value);
            emit('refresh',name.value)
        }

        return {
             handleRefresh,name,handleExternal
        }
    }
}
</script>
<style lang="stylus" scoped>

.ipaddress{
    span{vertical-align:middle}
}

.el-input{
    width:12rem;
    margin-right:.6rem
}

</style>