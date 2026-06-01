<template>
    <div class="w-100">
        <div class="wrap">
            <el-table stripe  :data="state.profiles" border size="small" width="100%" height="400px" @cell-dblclick="handleCellClick">
                <el-table-column prop="SourceSymbols" :label="$t('tuntap.fec.source')">
                    <template #default="scope">
                        <template v-if="scope.row.SourceSymbolsEditing">
                            <el-input v-trim autofocus size="small" v-model="scope.row.SourceSymbols"
                                @blur="handleEditBlur(scope.row, 'SourceSymbols')"></el-input>
                        </template>
                        <template v-else>
                            <a href="javascript:;" class="a-line" @click="handleEdit(scope.row, 'SourceSymbols')">
                                <span>{{ scope.row.SourceSymbols }} <a href="javascript:;" @click.stop="scope.row.SourceSymbols=0"><el-icon><Delete /></el-icon></a></span>
                            </a>
                        </template>
                    </template>
                </el-table-column>
                <el-table-column prop="RepairSymbols" :label="$t('tuntap.fec.repair')">
                    <template #default="scope">
                        <template v-if="scope.row.RepairSymbolsEditing">
                            <el-input v-trim autofocus size="small" v-model="scope.row.RepairSymbols"
                                @blur="handleEditBlur(scope.row, 'RepairSymbols')"></el-input>
                        </template>
                        <template v-else>
                            <a href="javascript:;" class="a-line" @click="handleEdit(scope.row, 'RepairSymbols')">
                                <span>{{ scope.row.RepairSymbols }} <a href="javascript:;" @click.stop="scope.row.RepairSymbols=0"><el-icon><Delete /></el-icon></a></span>
                            </a>
                        </template>
                    </template>
                </el-table-column>
                <el-table-column prop="Disabled" :label="$t('tuntap.fec.disabled')" width="80">
                    <template #default="scope">
                        <el-checkbox v-model="scope.row.Disabled" :label="$t('tuntap.fec.disabled')"/>
                    </template>
                </el-table-column>
                <el-table-column prop="Oper" :label="$t('common.oper')" width="110">
                    <template #default="scope">
                        <div>
                            <el-popconfirm :title="$t('common.delSure')" @confirm="handleDel(scope.$index)">
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
import { useTuntap } from './tuntap';
import { Delete, Plus, Warning, Refresh } from '@element-plus/icons-vue'
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { Delete, Plus, Warning, Refresh },
    setup(props) {

        const tuntap = useTuntap();
        const state = reactive({
            profiles: JSON.parse(JSON.stringify(tuntap.value.current.FecProfile))
        });
        if (state.profiles.length == 0) {
            state.profiles.push({ SourceSymbols: 0, RepairSymbols:0,Disabled:false });
        }

        const handleCellClick = (row, column) => {
            handleEdit(row, column.property);
        }
        const handleEdit = (row, p) => {
            state.profiles.forEach(c => {
                c[`SourceSymbolsEditing`] = false;
                c[`RepairSymbolsEditing`] = false;
                c[`DisabledEditing`] = false;
            })
            row[`${p}Editing`] = true;
            row[`__editing`] = true;
        }
        const handleEditBlur = (row, p) => {
            row[`${p}Editing`] = false;
            row[`__editing`] = false;
            try{row[p] = row[p].trim();}catch(w){}
            row[p] = +row[p];
        }
        const handleDel = (index) => {
            state.profiles.splice(index, 1);
            if (state.profiles.length == 0) {
                handleAdd(0);
            }
        }

        const handleAdd = (index) => {
            state.profiles.splice(index + 1, 0, { SourceSymbols: 0, RepairSymbols:0,Disabled:false });
        }
        const getData = ()=>{
            return state.profiles.map(c => { c.SourceSymbols = +c.SourceSymbols;c.RepairSymbols = +c.RepairSymbols; return c; });
        }

        return {
            state,handleDel,handleAdd,getData,handleCellClick,handleEditBlur,handleEdit
        }
    }
}
</script>
<style lang="stylus" scoped>
</style>