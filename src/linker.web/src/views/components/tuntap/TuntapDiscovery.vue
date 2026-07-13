<template>
    <div class="w-100">
        <div class="wrap">
            <el-table stripe  :data="state.discoverys" border size="small" width="100%" height="400px" @cell-dblclick="handleCellClick">
                <el-table-column prop="Name" :label="$t('tuntap.discovery.name')"></el-table-column>
                <el-table-column prop="Addr" :label="$t('tuntap.discovery.addr')">
                    <template #default="scope">
                        <span>{{ scope.row.Address }}:{{scope.row.Port}}</span>
                    </template>
                </el-table-column>
               
                <el-table-column prop="Disabled" :label="$t('tuntap.discovery.disabled')" width="100">
                    <template #default="scope">
                        <el-checkbox v-model="scope.row.Disabled" :label="$t('tuntap.discovery.disabled')"/>
                    </template>
                </el-table-column>
                <el-table-column prop="LanIps" :label="$t('tuntap.discovery.lanips')">
                    <template #default="scope">
                        <template v-if="scope.row.LanIpsEditing">
                            <el-select multiple v-model="scope.row.LanIps" @blur="handleEditBlur(scope.row, 'LanIps')">
                                <el-option v-for="value in state.ipv4" :label="value" :value="value"></el-option>
                            </el-select>
                        </template>
                        <template v-else>
                            <a href="javascript:;" class="a-line" @click="handleEdit(scope.row, 'LanIps')">{{ scope.row.LanIps.join('、') || '0.0.0.0' }}</a>
                        </template>
                    </template>
                </el-table-column>
                <el-table-column prop="Remark" :label="$t('tuntap.discovery.remark')"></el-table-column>
            </el-table>
        </div>
    </div>
</template>
<script>
import { onMounted, reactive } from 'vue';
import { useTuntap } from './tuntap';
import { Delete, Plus, Warning, Refresh } from '@element-plus/icons-vue'
import { getDiscoverys } from '@/apis/tuntap';
import { getBindIpv4 } from '@/apis/tunnel';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { Delete, Plus, Warning, Refresh },
    setup(props) {

        const tuntap = useTuntap();
        const state = reactive({
            discoverys: [],
            ipv4:[]
        });
        const handleCellClick = (row, column) => {
            handleEdit(row, column.property);
        }
        const handleEdit = (row, p) => {
            state.discoverys.forEach(c => {
                c[`LanIpsEditing`] = false;
            })
            row[`${p}Editing`] = true;
            row[`__editing`] = true;
        }
        const handleEditBlur = (row, p) => {
            row[`${p}Editing`] = false;
            row[`__editing`] = false;
        }
        const loadData = ()=>{
            getDiscoverys().then((res)=>{
                const settings = tuntap.value.current.Discoverys.reduce((json,item,index)=>{
                    json[item.Name] = item;
                    return json;
                },{})
                res.forEach(c=>{
                    const item = settings[c.Name] || {Disabled:true,LanIps:[]};
                    c.Disabled = item.Disabled;
                    c.LanIps = item.LanIps;
                })
                state.discoverys = res;
            }).catch(()=>{});
            getBindIpv4(tuntap.value.current.MachineId).then((res)=>{
                state.ipv4 = res;
            }).catch(()=>{})
        }
        const getData = ()=>{
            return state.discoverys.map(c =>c);
        }

        onMounted(()=>{
            loadData();
        })

        return {
            state,getData,handleCellClick,handleEditBlur,handleEdit
        }
    }
}
</script>
<style lang="stylus" scoped>
</style>