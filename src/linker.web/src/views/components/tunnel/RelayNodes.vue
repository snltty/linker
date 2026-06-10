<template>
      <el-dialog append-to=".app-wrap" v-model="state.show" :title="$t('relay.title')" width="80rem" top="2vh">
        <div>
            <el-table :data="state.nodes" size="small" border height="600">
                <el-table-column property="Name" :label="$t('relay.name')">
                    <template #default="scope">
                        <div>
                            <a :href="scope.row.Url" class="a-line blue" target="_blank">{{ scope.row.Name }}</a>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column property="ConnectionsRatio" :label="$t('relay.conn')" width="80">
                    <template #default="scope">
                        <span><strong>{{ scope.row.ConnectionsRatio }}</strong></span>
                    </template>
                </el-table-column>
                <el-table-column property="BandwidthEach" :label="$t('relay.speed')" width="140">
                    <template #default="scope">
                        <p>
                            <span>{{ scope.row.BandwidthRatio }}Mbps</span>
                            <span> / </span>
                            <span v-if="scope.row.BandwidthEach == 0">--</span>
                            <span v-else>{{ scope.row.BandwidthEach }}Mbps</span>
                        </p>
                    </template>
                </el-table-column>
                 <el-table-column property="DataEachMonth" :label="$t('relay.flow')" width="100">
                    <template #default="scope">
                        <span v-if="scope.row.DataEachMonth == 0">--</span>
                        <span v-else>
                            {{ (scope.row.DataRemain / 1024 / 1024 / 1024).toFixed(2) }}GB
                        </span>
                    </template>
                </el-table-column>
                <el-table-column property="Delay" :label="$t('relay.delay')" width="60">
                    <template #default="scope">
                        <span>{{ scope.row.Delay }}ms</span>
                    </template>
                </el-table-column>
                <el-table-column property="Public" :label="$t('relay.public')" width="55">
                    <template #default="scope">
                        <el-switch disabled v-model="scope.row.Public" size="small" />
                    </template>
                </el-table-column>
                <el-table-column property="Oper" :label="$t('relay.use')" width="130">
                    <template #default="scope">
                        <el-button size="small" v-if="(scope.row.Protocol & 1) == 1" @click="handleConnect(scope.row, 1)">TCP</el-button>
                        <el-button size="small" v-if="(scope.row.Protocol & 2) == 2" @click="handleConnect(scope.row, 2)">UDP</el-button>
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
    emits: ['update:modelValue','onrelay'],
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

        const handleConnect = (row,protocol)=>{
            emit('onrelay',[row,protocol])
        }
        return {state,handleConnect}
    }
}
</script>

<style lang="scss" scoped>

</style>