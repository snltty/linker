<template>
    <div class="action-wrap">
        <el-table :data="state.list" border size="small" width="100%" :height="`${state.height}px`" @cell-dblclick="handleCellClick">
            <el-table-column prop="Name" label="服务器名称" width="140"></el-table-column>
            <el-table-column prop="Host" label="服务器地址" width="200"></el-table-column>
            <el-table-column prop="json" label="Json参数" >
                <template #default="scope">
                    <template v-if="scope.row.jsonEditing">
                        <el-input type="password" show-password size="small" v-model="scope.row.json" @blur="handleEditBlur(scope.row, 'json')"></el-input>
                    </template>
                    <template v-else></template>
                </template>
            </el-table-column>
        </el-table>
    </div>
</template>
<script>
import { setArgs } from '@/apis/action';
import { setSignInServers } from '@/apis/signin';
import { injectGlobalData } from '@/provide';
import { ElMessage, ElMessageBox } from 'element-plus';
import { computed, inject, onMounted, reactive, watch } from 'vue'
export default {
    label:'验证',
    name:'action',
    order:0,
    setup(props) {
        const globalData = injectGlobalData();
        const state = reactive({
            list:[],
            height: computed(()=>globalData.value.height-20),
        });
        watch(()=>globalData.value.config.Client.Servers,()=>{
            init();
        });

        const init = ()=>{
            if(state.list.filter(c=>c['__editing']).length == 0){
                const jsons = globalData.value.config.Client.Action.Args;
                state.list = globalData.value.config.Client.Servers.map(c=>{
                    return Object.assign(c,{json:jsons[c.Host]||''});
                });
            }
        }

        const handleCellClick = (row, column) => {
            handleEdit(row, column.property);
        }
        const handleEdit = (row, p) => {
            row[`${p}Editing`] = true;
            row[`__editing`] = true;
        }
        const handleEditBlur = (row, p) => {
            row[`${p}Editing`] = false;
            row[`__editing`] = false;
            handleSave();
        }

        const handleSave = ()=>{

            if(state.list.filter(c=>{
                try{
                    if(c.json && typeof(JSON.parse(c.json)) != 'object'){
                        return true;
                    }
                    return false;
                }catch(e){
                    return true;
                }
            }).length > 0){
                ElMessage.error('Json格式错误');
                return;
            }

            const args = state.list.reduce((json,item,index)=>{
                json[item.Host] = item.json;
                return json;
            },{});
            console.log(args);

            setArgs(args).then(()=>{
                ElMessage.success('已操作');
            }).catch(()=>{
                ElMessage.success('操作失败');
            });;
        }

        onMounted(()=>{
            init();
        });

        return {state,handleCellClick,handleEditBlur}
    }
}
</script>
<style lang="stylus" scoped>
.action-wrap{
    padding:1rem;
}
</style>