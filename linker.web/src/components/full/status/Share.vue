<template>
    <div v-if="config" class="status-share-wrap">
        <a href="javascript:;" title="此设备的管理接口" @click="state.show = true">
            <el-icon size="16"><Share /></el-icon>
            导出配置
        </a>
        <el-dialog class="options-center" title="导出配置" destroy-on-close v-model="state.show" center  width="300" top="1vh">
            <div class="port-wrap t-c">
                导出配置，作为节点客户端运行，其仅有查看基本信息的能力，无法修改任何配置，如果使用docker，可以仅复制configs文件夹过去，docker映射配置文件夹即可
            </div>
            <template #footer>
                <el-button plain @click="state.show = false" :loading="state.loading">取消</el-button>
                <el-button type="success" plain @click="handleExport" :loading="state.loading">确定导出</el-button>
            </template>
        </el-dialog>
    </div>
</template>
<script>
import {  reactive } from 'vue';
import {Share} from '@element-plus/icons-vue'
import { exportConfig } from '@/apis/config';
import { ElMessage } from 'element-plus';
export default {
    components:{Share},
    props:['config'],
    setup(props) {
        const state = reactive({
            show: false,
            loading:false
        });
        const handleExport = ()=>{
            state.loading = true;
            exportConfig().then(()=>{
                state.loading = false;
                state.show = false;
                ElMessage.success('导出成功');

                const link = document.createElement('a');
                link.download = 'client-node-export.zip';
                link.href = '/client-node-export.zip';
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);

            }).catch(()=>{
                state.loading = false;
            });
        }

        return {config:props.config, state,handleExport};
    }
}
</script>
<style lang="stylus" scoped>
.status-share-wrap{
    padding-right:2rem;
    a{color:#333;}
    .el-icon{
        vertical-align:text-top;
    }
}

</style>