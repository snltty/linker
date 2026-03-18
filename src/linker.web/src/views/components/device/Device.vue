<template>
<el-table-column prop="MachineId" :label="$t('home.device')" width="196">
    <template #header>
        <div class="flex">
            <span>{{$t('home.device')}}</span>
            <span class="flex-1"> <el-input v-trim size="small" v-model="name" clearable @input="handleRefresh" class="w-100" ></el-input> </span>
        </div>
    </template>
    <template #default="scope">
        <template v-if="scope.row">
            <div class="flex flex-nowrap">
                <div class="avatar">
                    <template v-if="scope.row.avatar">
                        <template v-if="scope.row.avatar_url">
                            <el-avatar shape="square" :size="30" :src="scope.row.avatar_url" />
                        </template>
                        <template v-else>
                            <el-avatar shape="square" :size="30" :style="scope.row.avatar_style" >{{scope.row.avatar_text}}</el-avatar>
                        </template>
                    </template>
                    <template v-else>
                        <el-avatar shape="square" :size="30" src="user.png"></el-avatar>
                    </template>
                </div>
                <div class="flex-1 name">
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
                            <span class="ellipsis" :title="`${scope.row.LastSignIn}-${scope.row.Version}`">{{ scope.row.LastSignIn }}-{{ scope.row.Version }}</span>
                        </template>
                        <template v-else>
                            <el-skeleton animated >
                                <template #template>
                                    <div class="flex">
                                        <el-skeleton-item variant="text" class="vam w-20-" />
                                        <el-skeleton-item variant="text" class="vam w-20-" />
                                        <span class="flex-1"></span>
                                        <el-skeleton-item variant="text" class="vam w-20-" />
                                    </div>
                                </template>
                            </el-skeleton>
                        </template>
                    </p>
                </div>
            </div>
        </template>
        <div class="device-remark"></div>
    </template>
</el-table-column>
</template>
<script>
import { ref } from 'vue';
import {Search,UserFilled} from '@element-plus/icons-vue'
import UpdaterBtn from '../updater/UpdaterBtn.vue';
import DeviceName from './DeviceName.vue';
import SystemInfo from '../tuntap/SystemInfo.vue'; 
import WlistShow from '../wlist/Device.vue'



export default {
    emits:['refresh'],
    components:{Search,UserFilled,UpdaterBtn,DeviceName,SystemInfo,WlistShow},
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
.avatar{
    
    padding-right:.5rem;
    display: flex;
    align-items: center;
    img{
        width:3rem;
    }
}
.name p{
    line-height:1.8rem;
}
</style>