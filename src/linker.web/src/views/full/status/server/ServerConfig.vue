<template>
    <a href="javascript:;" :class="{connected:state.connected}" :title="$t('status.messengerChange')" @click="handleConfig">
        <el-icon size="16"><Promotion /></el-icon> <span>{{$t('status.messenger')}}</span>
    </a>
    <el-dialog v-model="state.show" :title="$t('common.setting')" width="300" append-to-body>
        <div>
            <el-form :model="state.form" :rules="state.rules" label-width="6rem">
                <el-form-item :label="$t('status.messengerName')" prop="name" v-if="hasRenameSelf">
                    <el-input v-model="state.form.name" maxlength="32" show-word-limit />
                </el-form-item>
                <el-form-item :label="$t('status.messengerGroup')" prop="groupid" v-if="hasGroup">
                    <el-select v-model="state.groupid" @change="handleGroupChange">
                        <el-option v-for="item in state.form.groups" :key="item.Id" :label="item.Name" :value="item.Id"/>
                    </el-select>
                </el-form-item>
            </el-form>
        </div>
        <template #footer>
            <div class="dialog-footer t-c">
                <el-button @click="state.show = false" :loading="state.loading">{{$t('common.cancel')}}</el-button>
                <el-button type="primary" @click="handleSave" :loading="state.loading">{{$t('common.confirm')}}</el-button>
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
import { useI18n } from 'vue-i18n';
export default {
    components:{Promotion,CirclePlus},
    props:['config'],
    setup(props) {
        const { t } = useI18n();
        const globalData = injectGlobalData();
        const hasRenameSelf = computed(()=>globalData.value.hasAccess('RenameSelf')); 
        const hasGroup = computed(()=>globalData.value.hasAccess('Group')); 
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
            if(!props.config || (!hasGroup.value && !hasRenameSelf.value)){
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
         config:props.config,hasRenameSelf,hasGroup,  state, handleConfig, handleSave,handleGroupChange
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