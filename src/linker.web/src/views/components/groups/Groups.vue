<template>
  <el-dialog class="options-center" :title="$t('status.group')" destroy-on-close v-model="state.show" width="77rem" top="2vh">
    <div class="group-wrap">
    <el-table stripe  :data="state.list" border size="small" width="100%" height="70vh" @cell-dblclick="handleCellClick">
        <el-table-column prop="Name" :label="$t('status.groupName')" width="100">
            <template #default="scope">
                <template v-if="scope.row.NameEditing">
                    <el-input v-trim autofocus size="small" v-model="scope.row.Name"
                        @blur="handleEditBlur(scope.row, 'Name')"></el-input>
                </template>
                <template v-else>
                    <a  href="javascript:;" class="a-line" @click="handleEdit(scope.row, 'Name')">{{ scope.row.Name || '未知' }}</a>
                </template>
            </template>
        </el-table-column>
        <el-table-column prop="Id" label="Id" >
            <template #default="scope">
                <template v-if="scope.row.IdEditing">
                    <el-input v-trim autofocus size="small" v-model="scope.row.Id"
                        @blur="handleEditBlur(scope.row, 'Id')"></el-input>
                </template>
                <template v-else>
                    <a  href="javascript:;" class="a-line" @click="handleEdit(scope.row, 'Id')">{{ scope.row.Id }}</a>
                </template>
            </template>
        </el-table-column>
        <el-table-column prop="Password" :label="$t('status.groupPassword')" >
            <template #default="scope">
                <template v-if="scope.row.PasswordEditing">
                    <el-input v-trim type="password" show-password size="small" v-model="scope.row.Password" @blur="handleEditBlur(scope.row, 'Password')"></el-input>
                </template>
                <template v-else>
                    <a href="javascript:;" class="a-line" @click="handleEdit(scope.row, 'Password')">
                        <template><span>***</span></template>
                    </a>
                </template>
            </template>
        </el-table-column>
        <el-table-column prop="Oper" :label="$t('status.groupOper')" width="110" fixed="right">
            <template #header>
                <div class="flex">
                    <strong>{{ $t('status.groupOper') }}</strong><span class="flex-1"></span><Sync name="GroupSecretKey"></Sync>
                </div>
            </template>
            <template #default="scope">
                <div>
                    <el-popconfirm :title="$t('status.groupDelConfirm')" @confirm="handleDel(scope.$index)">
                        <template #reference>
                            <el-button type="danger" size="small">
                                <el-icon><Delete /></el-icon>
                            </el-button>
                        </template>
                    </el-popconfirm>
                    <el-button size="small" @click="handleAdd(scope.$index)">
                        <el-icon><Plus /></el-icon>
                    </el-button>
                    <!-- <el-button v-if="scope.$index > 0" type="primary" size="small" @click="handleUse(scope.$index)">
                        <el-icon><Select /></el-icon>
                    </el-button> -->
                </div>
            </template>
        </el-table-column>
    </el-table>
   </div>
    </el-dialog>
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
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components:{Delete,Plus,Select,Sync },
    setup(props,{emit}) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            list:globalData.value.config.Client.Groups,
            show:true
        });
        watch(()=>globalData.value.config.Client.Groups,()=>{
            if(state.list.filter(c=>c['__editing']).length == 0){
                state.list = globalData.value.config.Client.Groups;
            }
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

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
            try{row[p] = row[p].trim();}catch(w){}
            handleSave();
        }

        const handleDel = (index)=>{
            state.list.splice(index,1);
            handleSave();
        }
        const handleAdd = (index)=>{
            if(state.list.filter(c=>c.Id == '' || c.Name == '').length > 0){
                ElMessage.error(t('status.groupValidate'));
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
                name:globalData.value.config.Client.Name.trim(),
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

        return {globalData,state,handleCellClick,handleEditBlur,handleEdit,handleDel,handleAdd,handleUse}
    }
}
</script>
<style lang="stylus" scoped>
</style>