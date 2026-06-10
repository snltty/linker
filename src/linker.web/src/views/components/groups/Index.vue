<template>
    <el-dropdown>
        <span class="el-dropdown-link" :class="{connected:state.connected}">
            <span>{{(state.groupName|| $t('common.unknow')).substr(0,6)}}</span>
            <el-icon class="right"><ArrowDown /></el-icon>
        </span>
        <template #dropdown>
            <el-dropdown-menu>
                <AccessShow value="Group">
                     <el-dropdown-item v-for="item in state.groups" @click="handleGroupChange(item.Id)">{{item.Name || $t('common.unknow')}}</el-dropdown-item>
                    <el-dropdown-item @click="handleGroups"><el-icon><Setting /></el-icon>{{$t('group')}}</el-dropdown-item>
                    <el-dropdown-item @click="handleShare"><el-icon><Share /></el-icon>{{$t('group.share')}}</el-dropdown-item>
                </AccessShow>
                <el-dropdown-item @click="handleJoin"><el-icon><Plus /></el-icon>{{$t('group.join')}}</el-dropdown-item>
            </el-dropdown-menu>
        </template>
    </el-dropdown>
    <Groups v-if="state.showGroups" v-model="state.showGroups"></Groups>
</template>
<script>
import { setSignIn } from '@/apis/signin';
import { injectGlobalData } from '@/provide';
import { ElMessage, ElMessageBox } from 'element-plus';
import { computed, reactive, ref } from 'vue';
import {ArrowDown,Avatar,Setting,Share,Plus} from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n';
import Groups from './Groups.vue';
import { joinGroup, shareGroup } from '@/apis/config';
export default {
    components:{ArrowDown,Avatar,Groups,Setting,Share,Plus},
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
                ElMessage.success(t('common.opered'));
                setTimeout(()=>{
                    window.location.reload();
                },1000);
            }).catch((err) => {
                console.log(err);
                state.loading = false;
                ElMessage.error(t('common.operFail'));
            });
        }

        const handleGroups = ()=>{
            state.showGroups = true;
        }
        const handleShare = ()=>{
            shareGroup().then((res)=>{
                if(res){
                    navigator.clipboard.writeText(res);
                    ElMessage.success(t('common.copied'));
                }else{
                    ElMessage.error(t('common.operFail'));
                }
            }).catch(()=>{
                ElMessage.error(t('common.operFail'));
            })
        }
        const handleJoin = ()=>{
            ElMessageBox.prompt(t('group.join'), t('common.tips'), {
                confirmButtonText:  t('common.confirm'),
                cancelButtonText: t('common.cancel'),
            }).then(({ value }) => {
                joinGroup(value).then((res)=>{
                    if(res){
                         ElMessage.success(t('common.opered'));
                        setTimeout(()=>{
                            window.location.reload();
                        },1000);
                    }else{
                        ElMessage.error(t('common.operFail'));
                    }
                }).catch(()=>{
                    ElMessage.error(t('common.operFail'));
                })
            }).catch(() => {
            })
        }

        return {
            state,handleGroupChange,handleGroups,handleShare,handleJoin
        }
    }
}
</script>
<style lang="stylus" scoped>
.el-dropdown-link{   
    &.connected {
        color:green;
    }
    .el-icon{vertical-align: middle;}
    span{display: inline-flex;align-items: center;line-height: 1;}
}  
</style>