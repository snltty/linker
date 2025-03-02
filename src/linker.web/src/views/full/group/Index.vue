<template>
   <div class="group-wrap">
    <el-table stripe  :data="state.list" border size="small" width="100%" :height="`${state.height}px`" @cell-dblclick="handleCellClick">
        <el-table-column prop="Name" :label="$t('server.groupName')" width="100">
            <template #default="scope">
                <template v-if="scope.row.NameEditing">
                    <el-input autofocus size="small" v-model="scope.row.Name"
                        @blur="handleEditBlur(scope.row, 'Name')"></el-input>
                </template>
                <template v-else>
                    {{ scope.row.Name }}
                </template>
            </template>
        </el-table-column>
        <el-table-column prop="Id" label="Id" >
            <template #default="scope">
                <template v-if="scope.row.IdEditing">
                    <el-input autofocus size="small" v-model="scope.row.Id"
                        @blur="handleEditBlur(scope.row, 'Id')"></el-input>
                </template>
                <template v-else>
                    {{ scope.row.Id }}
                </template>
            </template>
        </el-table-column>
        <el-table-column prop="Password" :label="$t('server.groupPassword')" >
            <template #default="scope">
                <template v-if="scope.row.PasswordEditing">
                    <el-input type="password" show-password size="small" v-model="scope.row.Password" @blur="handleEditBlur(scope.row, 'Password')"></el-input>
                </template>
                <template v-else>{{ scope.row.Password.replace(/.{1}/g,'*') }}</template>
            </template>
        </el-table-column>
        <el-table-column prop="Oper" :label="$t('server.groupOper')" width="160">
            <template #header>
                <div class="flex">
                    <strong>{{ $t('server.groupOper') }}</strong><span class="flex-1"></span><Sync name="GroupSecretKey"></Sync>
                </div>
            </template>
            <template #default="scope">
                <div>
                    <el-popconfirm :title="$t('server.groupDelConfirm')" @confirm="handleDel(scope.$index)">
                        <template #reference>
                            <el-button type="danger" size="small">
                                <el-icon><Delete /></el-icon>
                            </el-button>
                        </template>
                    </el-popconfirm>
                    <el-button size="small" @click="handleAdd(scope.$index)">
                        <el-icon><Plus /></el-icon>
                    </el-button>
                    <el-button v-if="scope.$index > 0" type="primary" size="small" @click="handleUse(scope.$index)">
                        <el-icon><Select /></el-icon>
                    </el-button>
                </div>
            </template>
        </el-table-column>
    </el-table>
   </div>
</template>
<script>
import { setSignIn, setSignInGroups } from '@/apis/signin';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, reactive, watch } from 'vue'
import { Delete,Plus,Select } from '@element-plus/icons-vue';
import { useI18n } from 'vue-i18n';
import Sync from '../sync/Index.vue'
export default {
    components:{Delete,Plus,Select,Sync },
    setup(props) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            list:globalData.value.config.Client.Groups || [],
            height: computed(()=>globalData.value.height-70),
        });
        watch(()=>globalData.value.config.Client.Groups,()=>{
            if(state.list.filter(c=>c['__editing']).length == 0){
                state.list = globalData.value.config.Client.Groups;
            }
        })

        const handleCellClick = (row, column) => {
            handleEdit(row, column.property);
        }
        const handleEdit = (row, p) => {
            state.list.forEach(c => {
                c[`NameEditing`] = false;
                c[`IdEditing`] = false;
                c[`PasswordEditing`] = false;
            })
            row[`${p}Editing`] = true;
            row[`__editing`] = true;
        }
        const handleEditBlur = (row, p) => {
            row[`${p}Editing`] = false;
            row[`__editing`] = false;
            handleSave();
        }

        const handleDel = (index)=>{
            state.list.splice(index,1);
            handleSave();
        }
        const handleAdd = (index)=>{
            if(state.list.filter(c=>c.Id == '' || c.Name == '').length > 0){
                return;
            }
            state.list.splice(index+1,0,{Name:'',Id:'',Password:''});
            handleSave();
        }
        const handleUse = (index)=>{
            const arr = state.list.slice();
            const temp = arr[index];
            arr[index] = arr[0];
            arr[0] = temp;
            setSignIn({
                name:globalData.value.config.Client.Name,
                groups:arr
            }).then(() => {
                ElMessage.success(t('common.oper'));
                setTimeout(()=>{
                    window.location.reload();
                },1000);
            }).catch((err) => {
                console.log(err);
                ElMessage.error(t('common.operFail'));
            });
        }

        const handleSave = ()=>{
            setSignInGroups(state.list).then(()=>{
                ElMessage.success(t('common.oper'));
            }).catch((err)=>{
                console.log(err);
                ElMessage.error(t('common.operFail'));
            });
        }

        return {state,handleCellClick,handleEditBlur,handleDel,handleAdd,handleUse}
    }
}
</script>
<style lang="stylus" scoped>
    .group-wrap{padding:1rem}
</style>