<template>
<el-table-column prop="MachineId" label="设备">
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
                <a href="javascript:;" @click="handleEdit(scope.row)" :class="{green:scope.row.Connected}">{{scope.row.MachineName }}</a>
                <strong v-if="scope.row.isSelf"> - (<el-icon><StarFilled /></el-icon> 本机) </strong>
            </p>
            <p class="flex">
                <span>{{ scope.row.IP }}</span>
                <span class="flex-1"></span>
                <a href="javascript:;" class="download" title="下载更新" @click="handleUpdate">
                    <template v-if="scope.row.Version != version && scope.row.Version == updater.Version">
                        <span title="与服务器版本不一致，建议更新">{{scope.row.Version}}<el-icon size="14"><Download /></el-icon></span>
                    </template>
                    <template v-else-if="scope.row.Version != updater.Version">
                        <span title="不是最新版本，建议更新">{{scope.row.Version}}<el-icon size="14"><Download /></el-icon></span>
                    </template>
                    <template v-else>
                        <span title="版本一致，但我无法阻止你喜欢更新" class="green">{{scope.row.Version}}</span>
                    </template>
                </a>
            </p>
        </div>
    </template>
</el-table-column>
</template>
<script>
import { injectGlobalData } from '@/provide';
import { computed, ref } from 'vue';
import {WarnTriangleFilled,StarFilled,Search,Download} from '@element-plus/icons-vue'
import { ElMessageBox } from 'element-plus';

export default {
    emits:['edit','refresh'],
    components:{WarnTriangleFilled,StarFilled,Search,Download},
    setup(props,{emit}) {

        const globalData = injectGlobalData();
        const version = computed(()=>globalData.value.signin.Version);
        const updater = computed(()=>globalData.value.updater);
        const name = ref('')

        const handleEdit = (row)=>{
            emit('edit',row)
        }
        const handleRefresh = ()=>{
            emit('refresh',name.value)
        }

        const handleUpdate = ()=>{
            ElMessageBox.confirm('将进入后台自动更新，更新完成后自动重启', '是否下载更新?', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }).then(() => {
            }).catch(() => {});
        }

        return {
            updater, handleEdit,handleRefresh,version,name,handleUpdate
        }
    }
}
</script>
<style lang="stylus" scoped>
a{
    color:#666;
    text-decoration: underline;
}
a.green{color:green}

a.download{
    margin-left:.6rem
    .el-icon{vertical-align:middle;color:red;font-weight:bold;}
}

.el-input{
    width:15rem;
    margin-right:.6rem
}
</style>