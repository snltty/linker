<template>
    <div class="w-100">
        <div class="wrap">
            <el-table stripe  :data="state.lans" border size="small" width="100%" height="400px" @cell-dblclick="handleCellClick">
                <el-table-column prop="IP" label="路由IP" width="120">
                    <template #default="scope">
                        <template v-if="scope.row.IPEditing">
                            <el-input v-trim autofocus size="small" v-model="scope.row.IP"
                                @blur="handleEditBlur(scope.row, 'IP')"></el-input>
                        </template>
                        <template v-else>
                            <a href="javascript:;" class="a-line" @click="handleEdit(scope.row, 'IP')">
                                <strong v-if="scope.row.Error" :title="scope.row.Error" class="red">{{ scope.row.IP }}</strong>
                                <span v-else>{{ scope.row.IP }} <a href="javascript:;" @click.stop="scope.row.IP='0.0.0.0'"><el-icon><Delete /></el-icon></a></span>
                            </a>
                        </template>
                    </template>
                </el-table-column>
                <el-table-column prop="PrefixLength" label="路由掩码" width="80">
                    <template #default="scope">
                        <template v-if="scope.row.PrefixLengthEditing">
                            <el-input v-trim autofocus size="small" v-model="scope.row.PrefixLength"
                                @blur="handleEditBlur(scope.row, 'PrefixLength')"></el-input>
                        </template>
                        <template v-else>
                            <a href="javascript:;" class="a-line" @click="handleEdit(scope.row, 'PrefixLength')">
                                <strong v-if="scope.row.Error" :title="scope.row.Error" class="red">{{ scope.row.PrefixLength }}</strong>
                                <span v-else>{{ scope.row.PrefixLength }}</span>
                            </a>
                        </template>
                    </template>
                </el-table-column>
                <el-table-column prop="MapIP" label="目标IP" width="120">
                    <template #default="scope">
                        <template v-if="scope.row.MapIPEditing">
                            <el-input v-trim autofocus size="small" v-model="scope.row.MapIP"
                                @blur="handleEditBlur(scope.row, 'MapIP')"></el-input>
                        </template>
                        <template v-else>
                            <a href="javascript:;" class="a-line" @click="handleEdit(scope.row, 'MapIP')">
                                <strong v-if="scope.row.Error" :title="scope.row.Error" class="red">{{ scope.row.MapIP }}</strong>
                                <span v-else>{{ scope.row.MapIP }} <a href="javascript:;" @click.stop="scope.row.MapIP='0.0.0.0'"><el-icon><Delete /></el-icon></a></span>
                            </a>
                        </template>
                    </template>
                </el-table-column>
                <el-table-column prop="MapPrefixLength" label="目标掩码" width="80">
                    <template #default="scope">
                        <template v-if="scope.row.MapPrefixLengthEditing">
                            <el-input v-trim autofocus size="small" v-model="scope.row.MapPrefixLength"
                                @blur="handleEditBlur(scope.row, 'MapPrefixLength')"></el-input>
                        </template>
                        <template v-else>
                            <a href="javascript:;" class="a-line" @click="handleEdit(scope.row, 'MapPrefixLength')">
                                <strong v-if="scope.row.Error" :title="scope.row.Error" class="red">{{ scope.row.MapPrefixLength }}</strong>
                                <span v-else>{{ scope.row.MapPrefixLength }}</span>
                            </a>
                        </template>
                    </template>
                </el-table-column>
                <el-table-column prop="Remark" label="备注">
                    <template #default="scope">
                        <template v-if="scope.row.RemarkEditing">
                            <el-input v-trim autofocus size="small" v-model="scope.row.Remark"
                                @blur="handleEditBlur(scope.row, 'Remark')"></el-input>
                        </template>
                        <template v-else>
                            <a href="javascript:;" class="a-line" @click="handleEdit(scope.row, 'Remark')">
                                <span>{{ scope.row.Remark }}</span>
                            </a>
                        </template>
                    </template>
                </el-table-column>
                <el-table-column prop="Disabled" label="禁用">
                    <template #default="scope">
                        <el-checkbox v-model="scope.row.Disabled" label="禁用"/>
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
import { reactive } from 'vue';
import { Delete, Plus, Warning, Refresh } from '@element-plus/icons-vue'
import { useSocks5 } from './socks5';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { Delete, Plus, Warning, Refresh },
    setup(props) {

        const socks5 = useSocks5();
        const state = reactive({
            lans: socks5.value.current.Lans.slice(0).map(c=>{ c.MapIP = c.MapIP || '0.0.0.0'; c.MapPrefixLength=c.MapPrefixLength || 24; return c; })
        });
        if (state.lans.length == 0) {
            state.lans.push({ IP: '0.0.0.0', PrefixLength: 24,MapIP:'0.0.0.0',MapPrefixLength:24,Remark:'' });
        }

        const handleCellClick = (row, column) => {
            handleEdit(row, column.property);
        }
        const handleEdit = (row, p) => {
            state.lans.forEach(c => {
                c[`IPEditing`] = false;
                c[`PrefixLengthEditing`] = false;
                c[`MapIPEditing`] = false;
                c[`MapPrefixLengthEditing`] = false;
                 c[`RemarkEditing`] = false;
            })
            row[`${p}Editing`] = true;
            row[`__editing`] = true;
        }
        const handleEditBlur = (row, p) => {
            row[`${p}Editing`] = false;
            row[`__editing`] = false;
            try{row[p] = row[p].trim();}catch(w){}

            if(p == 'PrefixLength' || p == 'MapPrefixLength'){
                var value = +row[p];
                if (value > 32 || value < 0 || isNaN(value)) {
                    value = 24;
                }
                row[p] = value;
            }
        }
        const handleDel = (index) => {
            state.lans.splice(index, 1);
            if (state.lans.length == 0) {
                handleAdd(0);
            }
        }

        const handleAdd = (index) => {
            state.lans.splice(index + 1, 0, { IP: '0.0.0.0', PrefixLength: 24 ,MapIP:'0.0.0.0',MapPrefixLength:24,Remark:'' });
        }
        const getData = ()=>{
            return state.lans.map(c => { c.PrefixLength = +c.PrefixLength; return c; });
        }

        return {
            state,handleDel,handleAdd,getData,handleCellClick,handleEditBlur,handleEdit
        }
    }
}
</script>
<style lang="stylus" scoped>
</style>