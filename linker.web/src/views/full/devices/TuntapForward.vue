<template>
    <div class="w-100">
        <div>
            <span class="yellow">此设备无法使用NAT转发，或只想使用端口转发</span>
            <span class="green" v-if="state.testing">、testing</span>
        </div>
        <div class="wrap">
            <el-table stripe  :data="state.forwards" border size="small" width="100%" height="300px" @cell-dblclick="handleCellClick">
                <el-table-column prop="ListenPort" label="源端口" width="60">
                    <template #default="scope">
                        <template v-if="scope.row.ListenPortEditing">
                            <el-input autofocus size="small" v-model="scope.row.ListenPort"
                                @blur="handleEditBlur(scope.row, 'ListenPort')"></el-input>
                        </template>
                        <template v-else>
                            {{ scope.row.ListenPort }}
                        </template>
                    </template>
                </el-table-column>
                <el-table-column prop="ConnectAddr" label="目标IP" width="120">
                    <template #default="scope">
                        <template v-if="scope.row.ConnectAddrEditing">
                            <el-input autofocus size="small" v-model="scope.row.ConnectAddr"
                                @blur="handleEditBlur(scope.row, 'ConnectAddr')"></el-input>
                        </template>
                        <template v-else>
                            {{ scope.row.ConnectAddr }}
                        </template>
                    </template>
                </el-table-column>
                <el-table-column prop="ConnectPort" label="目标端口" width="80">
                    <template #default="scope">
                        <template v-if="scope.row.ConnectPortEditing">
                            <el-input autofocus size="small" v-model="scope.row.ConnectPort"
                                @blur="handleEditBlur(scope.row, 'ConnectPort')"></el-input>
                        </template>
                        <template v-else>
                            {{ scope.row.ConnectPort }}
                        </template>
                    </template>
                </el-table-column>
                <el-table-column prop="Remark" label="备注">
                    <template #default="scope">
                        <template v-if="scope.row.RemarkEditing">
                            <el-input autofocus size="small" v-model="scope.row.Remark"
                                @blur="handleEditBlur(scope.row, 'Remark')"></el-input>
                        </template>
                        <template v-else>
                            <div class="remark">
                                {{ scope.row.Remark }}
                            </div>
                        </template>
                    </template>
                </el-table-column>
                <el-table-column prop="Oper" label="操作" width="110">
                    <template #default="scope">
                        <div>
                            <el-popconfirm title="删除不可逆，是否确认?" @confirm="handleDel(scope.$index)">
                                <template #reference>
                                    <el-button type="danger" size="small">
                                        <el-icon><Delete /></el-icon>
                                    </el-button>
                                </template>
                            </el-popconfirm>
                            <el-button type="primary" size="small" @click="handleAdd(scope.$index)">
                                <el-icon><Plus /></el-icon>
                            </el-button>
                        </div>
                    </template>
                </el-table-column>
            </el-table>
        </div>
    </div>
</template>
<script>
import {subscribeForwardTest } from '@/apis/tuntap';
import { reactive, onMounted, onUnmounted } from 'vue';
import { useTuntap } from './tuntap';
import { Delete, Plus, Warning, Refresh } from '@element-plus/icons-vue'
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { Delete, Plus, Warning, Refresh },
    setup(props) {

        const tuntap = useTuntap();
        const state = reactive({
            machineName: tuntap.value.current.device.MachineName,
            
            forwards: tuntap.value.current.Forwards.length == 0 ? [
                    { ListenAddr: '0.0.0.0', ListenPort: 0, ConnectAddr: '0.0.0.0', ConnectPort: 0, Remark: '' }
            ] : tuntap.value.current.Forwards.slice(0),
            timer: 0,
            testing: false
        });

        const handleCellClick = (row, column) => {
            handleEdit(row, column.property);
        }
        const handleEdit = (row, p) => {
            state.forwards.forEach(c => {
                c[`ListenAddrEditing`] = false;
                c[`ListenPortEditing`] = false;
                c[`ConnectAddrEditing`] = false;
                c[`ConnectPortEditing`] = false;
                c[`RemarkEditing`] = false;
            })
            row[`${p}Editing`] = true;
            row[`__editing`] = true;
        }
        const handleEditBlur = (row, p) => {
            row[`${p}Editing`] = false;
            row[`__editing`] = false;
        }

        const handleDel = (index)=>{
            if(state.forwards.length == 1){
                state.forwards[0] = { ListenAddr: '0.0.0.0', ListenPort: 0, ConnectAddr: '0.0.0.0', ConnectPort: 0, Remark: '' };
            }
            else{
                state.forwards.splice(index,1);
            }
        }
        const handleAdd = (index)=>{
            if(state.forwards.filter(c=>c.ConnectAddr == '0.0.0.0' || c.ConnectPort == 0 || c.ListenPort == 0).length > 0){
                return;
            }
            state.forwards.splice(index+1,0,{ ListenAddr: '0.0.0.0', ListenPort: 0, ConnectAddr: '0.0.0.0', ConnectPort: 0, Remark: '' });
        }
        const _subscribeForwardTest = () => {
            clearTimeout(state.timer);

            state.testing = true;
            subscribeForwardTest({
                MachineId: tuntap.value.current.MachineId,
                List: state.forwards.map(c => {
                    return {
                        ListenAddr: c.ListenAddr,
                        ListenPort: +c.ListenPort,
                        ConnectAddr: c.ConnectAddr,
                        ConnectPort: +c.ConnectPort
                    }
                })
            }).then((res) => {
                var list = res.List;
                for (let i = 0; i < list.length; i++) {
                    const item = list[i];
                    const key = `${item.ListenPort}->${item.ConnectAddr}:${item.ConnectPort}`;
                    const forwards = state.forwards.filter(c => `${c.ListenPort}->${c.ConnectAddr}:${c.ConnectPort}` == key);
                    for (let k = 0; k < forwards.length; k++) {
                        forwards[k].Error = item.Error;
                    }
                }

                state.testing = false;
                state.timer = setTimeout(_subscribeForwardTest, 3000);
            }).catch(() => {
                state.testing = false;
                state.timer = setTimeout(_subscribeForwardTest, 3000);
            });
        }

        onMounted(() => {
            _subscribeForwardTest();
        });
        onUnmounted(() => {
            clearTimeout(state.timer);
        });

        const getData = ()=>{
            return state.forwards.map(c=>{
                c.ListenPort = +c.ListenPort;
                c.ConnectPort = +c.ConnectPort;
                return c;
            });
        }

        return {
            state,handleCellClick,handleEditBlur,handleDel,handleAdd,getData
        }
    }
}
</script>
<style lang="stylus" scoped>
.wrap{
    padding-right:1rem;
}
.remark{
    white-space: nowrap; /* 文本不换行 */
    overflow: hidden; /* 超出的文本隐藏 */
    text-overflow: ellipsis; /* 超出的部分显示省略号 */
}

</style>