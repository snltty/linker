<template>
    <AccessBoolean value="Sync">
        <template #default="{values}">
            <div>
                <slot>
                    <el-button class="btn" size="small" @click="handleShowSync(values)"><el-icon><Share /></el-icon></el-button>
                </slot>
                <el-dialog class="options-center" :title="$t('server.sync')" destroy-on-close v-model="state.showNames" width="54rem" top="2vh">
                    <div>
                        <div class="t-c">
                            {{ `${$t('server.sync')}【${$t(`server.async${state.name}`)}】${$t('server.asyncText')}` }}
                        </div>
                        <el-transfer class="src-tranfer mgt-1"
                            v-model="state.srcIdValues"
                            filterable
                            :filter-method="srcFilterMethod"
                            :data="state.srcIds"
                            :titles="[$t('firewall.unselect'), $t('firewall.selected')]"
                            :props="{
                                key: 'MachineId',
                                label: 'MachineName',
                            }"
                        />
                        <div class="t-c w-100 mgt-1">
                                <el-button @click="state.showNames = false">{{$t('common.cancel')}}</el-button>
                                <el-button type="primary" @click="handleConfirm">{{$t('common.confirm')}}</el-button>
                            </div>
                    </div>
                </el-dialog>
            </div>
        </template>
    </AccessBoolean>
</template>

<script>
import { getSignInNames } from '@/apis/signin';
import { setSync } from '@/apis/sync';
import { injectGlobalData } from '@/provide';
import {Share} from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus';
import { computed, reactive } from 'vue';
import { useI18n } from 'vue-i18n';
export default {
    props:['name'],
    components: {Share},
    setup (props) {
        const { t } = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            name:props.name,
            loading:false,
            showNames:false,
            srcIdValues:[],
            srcIds:[],
        });

        const handleConfirm = ()=>{
            setSync({
                names:[props.name],
                ids:state.srcIdValues
            }).then(res => {
                ElMessage.success(t('common.oper'));
                state.showNames = false;
            });
        }
        const handleShowSync = (access)=>{
            if(!access.Sync){
                ElMessage.success(t('common.access'));
                return;
            }
            state.showNames = true;
            _getSignInNames();
        }
        const _getSignInNames = ()=>{
            state.loading = true;
            getSignInNames().then((res)=>{
                state.loading = false;
                state.srcIds = res;
            }).catch((e)=>{
                state.loading = false;
            });
        }
        const srcFilterMethod = (query, item) => {
            return item.MachineName.toLowerCase().includes(query.toLowerCase())
        }

        return {
            state,handleShowSync,srcFilterMethod,handleConfirm
        }
    }
}
</script>
<style lang="stylus">
.el-transfer.src-tranfer .el-transfer__buttons .el-button{display:block;}
.el-transfer.src-tranfer .el-transfer__buttons .el-button:nth-child(2){margin:1rem 0 0 0;}
</style>
<style lang="stylus" scoped>
</style>