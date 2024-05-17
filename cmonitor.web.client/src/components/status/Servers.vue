<template>
    <el-table :data="state.list" border size="small" width="100%" height="300">
        <el-table-column prop="Name" label="名称"></el-table-column>
        <el-table-column prop="Host" label="地址" ></el-table-column>
    </el-table>
    <el-dialog v-model="state.showAdd" title="添加服务器" width="300" >
        <div>
            <el-form :model="state.formAdd" :rules="state.rulesAdd" label-width="6rem">
                <el-form-item label="名称" prop="name">
                    <el-input v-model="state.formAdd.name" maxlength="12" show-word-limit />
                </el-form-item>
                <el-form-item label="地址" prop="host">
                    <el-input v-model="state.formAdd.host" placeholder="ip/域名:端口" />
                </el-form-item>
            </el-form>
        </div>
        <template #footer>
        <div class="dialog-footer t-c">
            <el-button @click="state.showAdd = false" :loading="state.loading">取消</el-button>
            <el-button type="primary" @click="handleSaveAdd" :loading="state.loading">确定保存</el-button>
        </div>
        </template>
    </el-dialog>
</template>
<script>
import { reactive } from 'vue'
export default {
    props:{
        data:{
            type:Array,
            default:[]
        }
    },
    setup(props) {
        const state = reactive({
            list:props.data,
            
            showAdd:false,
            formAdd:{
                name:'',
                host:''
            },
            rulesAdd:{
                name:[
                    { required: true, message: '听填写', trigger: 'blur' },
                ],
                host:[
                    { required: true, message: '听填写', trigger: 'blur' },
                ]
            },
        });

        const handleDel = (item)=>{
            const servers = state.list.filter(c=>c.Host != item.Host || c.Name != item.Name);
        }
        const handleAdd = ()=>{
            state.showAdd = true;
            state.formAdd.name = '';
            state.formAdd.host = '';
        }
        const handleSaveAdd = ()=>{
            const servers = state.list || [];
            const name  =  state.formAdd.name.replace(/^\s|\s$/g,'');
            const host  =  state.formAdd.host.replace(/^\s|\s$/g,'');
            if(servers.filter(c=>c.Host == host).length > 0 || servers.filter(c=>c.Name == name).length > 0){
                ElMessage.error('已存在差不多相同的记录!');                
                return;
            }
            servers.push({Name:name,Host:host});
        }

        return {state,handleDel,handleAdd,handleSaveAdd}
    }
}
</script>
<style lang="stylus" scoped>
    
</style>