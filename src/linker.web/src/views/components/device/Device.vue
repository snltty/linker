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
                    <div>
                        <template v-if="tuntap.list[scope.row.MachineId] && tuntap.list[scope.row.MachineId].systems">
                            <template v-for="system in tuntap.list[scope.row.MachineId].systems">
                                <span :title="tuntap.list[scope.row.MachineId].SystemInfo">
                                    <img class="system" :src="`./${system}.svg`" />
                                </span>
                            </template>
                        </template>
                    </div>
                    <span title="20Mbps">
                        <a href="javascript:;"><img class="system" src="lightning.svg" /></a>
                    </span>
                    
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
import { useTuntap } from '../tuntap/tuntap';

export default {
    emits:['refresh'],
    components:{Search,UpdaterBtn,DeviceName},
    setup(props,{emit}) {

        const tuntap = useTuntap();
        const name = ref(sessionStorage.getItem('search-name') || '');
        
        const handleRefresh = ()=>{
            sessionStorage.setItem('search-name',name.value);
            emit('refresh',name.value)
        }

        return {
            tuntap,name, handleRefresh
        }
    }
}
</script>
<style lang="stylus" scoped>

.el-input{
    width:12rem;
    margin-right:.6rem
}
img.system{
    height:1.4rem;
    vertical-align: middle;
    margin-right:.1rem
    border:1px solid rgba(0,0,0,.1);
    border-radius:.2rem;
}
</style>