<template>
      <el-dialog v-model="state.show" :title="$t('network.tunnel.pcp.title')" width="98%" top="2vh">
        <div>
            <el-table :data="state.nodes" size="small" border height="600">
                <el-table-column property="NodeName" :label="$t('network.tunnel.pcp.name')"></el-table-column>
                <el-table-column property="Bandwidth" :label="$t('network.tunnel.pcp.bandwidth')" width="120">
                    <template #default="scope">
                        <span>{{ scope.row.Bandwidth }}Mbps</span>
                    </template>
                </el-table-column>
                <el-table-column property="Enabled" :label="$t('network.tunnel.pcp.enabled')" width="120">
                    <template #default="scope">
                        <el-switch disabled v-model="scope.row.Enabled" size="small" />
                    </template>
                </el-table-column>
                <el-table-column property="Oper" :label="$t('network.tunnel.pcp.use')" width="80">
                    <template #default="scope">
                        <el-button v-if="scope.row.Enabled" size="small" @click="handleConnect(scope.row.NodeId)">PCP</el-button>
                    </template>
                </el-table-column>
            </el-table>
        </div>
    </el-dialog>
</template>

<script>
import { computed, reactive, watch } from 'vue';

export default {
    props: ['modelValue','nodes'],
    emits: ['update:modelValue','onpcp'],
    setup (props,{emit}) {
        const state = reactive({
            show: true,
            nodes:computed(()=>props.nodes)
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const handleConnect = (id)=>{
            emit('onpcp',[id])
        }
        return {state,handleConnect}
    }
}
</script>

<style lang="scss" scoped>

</style>