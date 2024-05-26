<template>
    <el-table-column prop="tuntap" label="虚拟网卡" width="150">
        <template #header>
            <div class="flex">
                <span class="flex-1">虚拟网卡</span>
                <el-button size="small" @click="handleRuntapRefresh"><el-icon><Refresh /></el-icon></el-button>
            </div>
        </template>
        <template #default="scope">
            <div v-if="data[scope.row.MachineName]">
                <div class="flex">
                    <div class="flex-1">
                        <a href="javascript:;" class="a-line" @click="handleTuntapIP(data[scope.row.MachineName])">
                            <template v-if="data[scope.row.MachineName].running">
                                <strong class="green">{{ data[scope.row.MachineName].IP }}</strong>
                            </template>
                            <template v-else>
                                <span>{{ data[scope.row.MachineName].IP }}</span>
                            </template>
                        </a>
                    </div>
                    <el-switch v-model="data[scope.row.MachineName].running" :loading="data[scope.row.MachineName].loading" disabled @click="handleTuntap(data[scope.row.MachineName])"  size="small" inline-prompt active-text="O" inactive-text="F" > 
                    </el-switch>
                </div>
                <div>{{ data[scope.row.MachineName].LanIPs.join('、') }}</div>
            </div> 
        </template>
    </el-table-column>
</template>
<script>
import { stopTuntap, runTuntap } from '@/apis/tuntap';
import { ElMessage } from 'element-plus';
import { reactive } from 'vue';

export default {
    props: ['data'],
    emits: ['change','edit','refresh'],
    setup(props, { emit }) {
        const state = reactive({});
       
        const handleTuntap = (tuntap) => {
            const fn = tuntap.running ? stopTuntap (tuntap.MachineName) : runTuntap(tuntap.MachineName);
            fn.then(() => {
                ElMessage.success('操作成功！');
                emit('change');
            }).catch(() => {
                ElMessage.error('操作失败！');
            })
        }
        const handleTuntapIP = (tuntap) => {
            emit('edit',tuntap);
        }
        const handleRuntapRefresh = ()=>{
            emit('refresh');
        }
       
        return {
            data: props.data, state, handleTuntap, handleTuntapIP,handleRuntapRefresh
        }
    }
}
</script>
<style lang="stylus" scoped>
.green{color:green;}
.el-switch.is-disabled{opacity :1;}
</style>