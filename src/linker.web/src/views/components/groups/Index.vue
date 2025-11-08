<template>
    <el-dropdown>
        <span class="el-dropdown-link" :class="{connected:state.connected}">
            <el-icon class="left"><Avatar /></el-icon>
            <span>{{state.groupName|| '未知'}}</span>
            <el-icon class="right"><ArrowDown /></el-icon>
        </span>
        <template #dropdown>
            <AccessShow value="Group">
                <el-dropdown-menu>
                    <el-dropdown-item v-for="item in state.groups" @click="handleGroupChange(item.Id)">{{item.Name || '未知'}}</el-dropdown-item>
                    <el-dropdown-item @click="state.showGroups = true">{{$t('status.group')}}</el-dropdown-item>
                </el-dropdown-menu>
            </AccessShow>
        </template>
    </el-dropdown>
    <Groups v-if="state.showGroups" v-model="state.showGroups"></Groups>
</template>
<script>
import { setSignIn } from '@/apis/signin';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, reactive, ref } from 'vue';
import {ArrowDown,Avatar} from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n';
import Groups from './Groups.vue';
export default {
    components:{ArrowDown,Avatar,Groups},
    props:['config'],
    setup(props) {
        const { t } = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            loading: false,
            connected: computed(() => globalData.value.signin.Connected),
            groupName: computed(()=>globalData.value.config.Client.Group.Name),
            groups:computed(()=>globalData.value.config.Client.Groups),
            showGroups:false
        });
        const handleGroupChange = (value)=>{
            const groups = globalData.value.config.Client.Groups;
            const index = groups.map((item,index)=>{
                item.$index =  index;
                return item;
            }).filter(c=>c.Id == value)[0].$index;
            const temp =groups[index];
            groups[index] = groups[0];
            groups[0] = temp;
            handleSave(groups);
        }
        const handleSave = (groups) => {
            state.loading = true;
            setSignIn({
                Name:globalData.value.config.Client.Name,
                Groups:groups,
            }).then(() => {
                state.loading = false;
                state.show = false;
                ElMessage.success(t('common.oper'));
                setTimeout(()=>{
                    window.location.reload();
                },1000);
            }).catch((err) => {
                console.log(err);
                state.loading = false;
                ElMessage.error(t('common.operFail'));
            });
        }
        return {
         config:props.config,state,handleGroupChange
        }
    }
}
</script>
<style lang="stylus" scoped>
.el-dropdown{vertical-align: inherit;margin-right:1rem}

    .el-dropdown-link{   
        &.connected {
            color:green;font-weight:bold;
        }
        .el-icon{
            vertical-align:bottom;
        }
    }  
    

</style>