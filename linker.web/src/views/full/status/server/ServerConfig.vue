<template>
    <a href="javascript:;" :class="{connected:state.connected}" title="更改你的连接设置" @click="handleConfig">
        <el-icon size="16"><Promotion /></el-icon> <span>信标服务器</span>
    </a>
    <el-dialog v-model="state.show" title="连接设置" width="300" append-to-body>
        <div>
            <el-form :model="state.form" :rules="state.rules" label-width="6rem">
                <el-form-item label="机器名" prop="name">
                    <el-input v-model="state.form.name" maxlength="12" show-word-limit />
                </el-form-item>
                <el-form-item label="分组名" prop="groupid">
                    <el-select v-model="state.groupid" @change="handleGroupChange">
                        <el-option v-for="item in state.form.groups" :key="item.Id" :label="item.Name" :value="item.Id"/>
                    </el-select>
                </el-form-item>
            </el-form>
        </div>
        <template #footer>
            <div class="dialog-footer t-c">
                <el-button @click="state.show = false" :loading="state.loading">取消</el-button>
                <el-button type="primary" @click="handleSave" :loading="state.loading">确定保存</el-button>
            </div>
        </template>
    </el-dialog>
</template>
<script>
import { setSignIn } from '@/apis/signin';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, reactive, ref } from 'vue';
import {Promotion,CirclePlus} from '@element-plus/icons-vue'
export default {
    components:{Promotion,CirclePlus},
    props:['config'],
    setup(props) {

        const globalData = injectGlobalData();
        const hasConfig = computed(()=>globalData.value.hasAccess('Config')); 
        const state = reactive({
            show: false,
            loading: false,
            connected: computed(() => globalData.value.signin.Connected),
            groupid: globalData.value.config.Client.Group.Id,
            form: {
                name: globalData.value.config.Client.Name,
                groups: globalData.value.config.Client.Groups,
            },
            rules: {},
        });

        const handleConfig = () => {
            if(!props.config || !hasConfig.value){
                return;
            }
            state.form.name = globalData.value.config.Client.Name;
            state.form.groups = globalData.value.config.Client.Groups;

            state.groupid = globalData.value.config.Client.Group.Id;
            state.show = true;
        }

        const handleGroupChange = (value)=>{
            const index = state.form.groups.map((item,index)=>{
                item.$index =  index;
                return item;
            }).filter(c=>c.Id == value)[0].$index;
            const temp = state.form.groups[index];
            state.form.groups[index] = state.form.groups[0];
            state.form.groups[0] = temp;
        }
        const handleSave = () => {
            state.loading = true;
            setSignIn(state.form).then(() => {
                state.loading = false;
                state.show = false;
                ElMessage.success('已操作');
                setTimeout(()=>{
                    window.location.reload();
                },1000);
            }).catch((err) => {
                state.loading = false;
                ElMessage.success('操作失败!');
            });
        }
        return {
         config:props.config,  state, handleConfig, handleSave,handleGroupChange
        }
    }
}
</script>
<style lang="stylus" scoped>
a{
    a{color:#333;}
    a{margin-left:.6rem;}

    &.connected {
        color:green;font-weight:bold;
    }  
    .el-icon{
        vertical-align:text-bottom;
    }
}
</style>