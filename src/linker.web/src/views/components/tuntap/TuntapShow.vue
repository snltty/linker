<template>
    <AccessBoolean value="TuntapChangeSelf,TuntapChangeOther,TuntapStatusSelf,TuntapStatusOther">
        <template #default="{values}">
            <div class="nowrap flex">
                <div class="flex-1">
                    <ConnectionShow :row="item" transactionId="tuntap"></ConnectionShow>         
                    <a href="javascript:;" class="a-line" @click="handleTuntapIP(item.hook_tuntap,values)" :title="$t('tuntap.show.title')">
                        <template v-if="item.Connected">
                            <template v-if="item.hook_tuntap.SetupError">
                                <strong class="red" :title="`setup ${item.hook_tuntap.SetupError}`">{{ item.hook_tuntap.IP }}/{{ item.hook_tuntap.PrefixLength }}</strong>
                            </template>
                            <template v-else-if="item.hook_tuntap.Exists">
                                <strong class="red" :title="$t('tuntap.show.clash')">{{ item.hook_tuntap.IP }}/{{ item.hook_tuntap.PrefixLength }}</strong>
                            </template>
                            <template v-else-if="item.hook_tuntap.Available == false">
                                <strong class="disable" :title="$t('tuntap.show.offline')">{{ item.hook_tuntap.IP }}/{{ item.hook_tuntap.PrefixLength }}</strong>
                            </template>
                            <template v-else-if="item.hook_tuntap.NatError">
                                <strong class="yellow" :title="`nat ${item.hook_tuntap.NatError}`">{{ item.hook_tuntap.IP }}/{{ item.hook_tuntap.PrefixLength }}</strong>
                            </template>
                            <template v-else-if="item.hook_tuntap.AppNat && item.hook_tuntap.running">
                                <strong class="app-nat" :title="$t('tuntap.show.dnat')">{{ item.hook_tuntap.IP }}/{{ item.hook_tuntap.PrefixLength }}</strong>
                            </template>
                            <template v-else-if="item.hook_tuntap.running">
                                <strong class="green" :title="$t('tuntap.show.snat')">{{ item.hook_tuntap.IP }}/{{ item.hook_tuntap.PrefixLength }}</strong>
                            </template>
                            <template v-else>
                                <strong>{{ item.hook_tuntap.IP }}/{{ item.hook_tuntap.PrefixLength }}</strong>
                            </template>
                        </template>
                        <template v-else>
                            <strong class="disable" :title="$t('tuntap.show.offline')">{{ item.hook_tuntap.IP }}/{{ item.hook_tuntap.PrefixLength }}</strong>
                        </template>
                    </a>
                </div>
                <template v-if="item.hook_tuntap.loading">
                    <div>
                        <el-icon size="14" class="loading"><Loading /></el-icon>
                    </div>
                </template>
                <template v-else>
                    <el-switch :model-value="item.Connected && item.hook_tuntap.running" :loading="item.hook_tuntap.loading" disabled @click="handleTuntap(item.hook_tuntap,values)"  size="small" inline-prompt active-text="😀" inactive-text="😣" > 
                    </el-switch>
                </template>
            </div>
            <div class="nowrap">
                <template v-for="(item1,index) in  item.hook_tuntap.Lans" :key="index">
                    <template v-if="item.hook_tuntap.Available == false">
                        <div class="flex disable" :title="$t('tuntap.show.offline')">
                            <span>{{ item1.IP }}/{{ item1.PrefixLength }}</span>
                            <span class="flex-1 remark" :title="item1.Remark">{{ item1.Remark }}</span>
                        </div>
                    </template>
                    <template v-else-if="item1.Disabled">
                        <div class="flex disable" :title="$t('tuntap.show.disabled')">
                            <span>{{ item1.IP }}/{{ item1.PrefixLength }}</span>
                            <span class="flex-1 remark" :title="item1.Remark">{{ item1.Remark }}</span>
                        </div>
                    </template>
                    <template v-else-if="item1.Exists">
                        <div class="flex yellow" :title="$t('tuntap.show.clash')">
                            <span>{{ item1.IP }}/{{ item1.PrefixLength }}</span>
                            <span class="flex-1 remark" :title="item1.Remark">{{ item1.Remark }}</span>
                        </div>
                    </template>
                    <template v-else>
                        <div class="flex green" :title="$t('tuntap.show.normal')">
                            <span>{{ item1.IP }}/{{ item1.PrefixLength }}</span>
                            <span class="flex-1 remark" :title="item1.Remark">{{ item1.Remark }}</span>
                        </div>
                    </template>
                </template>
            </div>
            <template v-if="showDelay">
                <template v-if="item.hook_tuntap.Delay>=0 && item.hook_tuntap.Delay<=100">
                    <div class="delay green">{{ item.hook_tuntap.Delay }}ms</div>
                </template>
                <template v-else>
                    <div class="delay yellow">{{ item.hook_tuntap.Delay }}ms</div>
                </template>
            </template>
        </template>
    </AccessBoolean>
</template>

<script>
import { stopTuntap, runTuntap, refreshTuntap } from '@/apis/tuntap';
import { ElMessage } from 'element-plus';
import { useTuntap } from './tuntap';
import {Loading,Share} from '@element-plus/icons-vue'
import { injectGlobalData } from '@/provide';
import { computed } from 'vue';
import ConnectionShow from '../tunnel/ConnectionShow.vue';
import { useI18n } from 'vue-i18n';
export default {
    props:['item','config'],
    components:{Loading,Share,ConnectionShow},
    setup (props) {
        
        const {t} = useI18n();
        const tuntap = useTuntap();
        const globalData = injectGlobalData();
        const machineId = computed(() => globalData.value.config.Client.Id);


        const showDelay = computed(()=>((globalData.value.config.Running.Tuntap || {Switch:0}).Switch & 2) == 2);
        const handleTuntap = (_tuntap,access) => {
            if(!props.config){
                return;
            }
            if(machineId.value === _tuntap.MachineId){
                if(!access.TuntapStatusSelf){
                    ElMessage.success(t('common.access'));
                return;
            }
            }else{
                if(!access.TuntapStatusOther){
                    ElMessage.success(t('common.access'));
                return;
            }
            }

            const fn = props.item.Connected && _tuntap.running ? stopTuntap (_tuntap.MachineId) : runTuntap(_tuntap.MachineId);
            _tuntap.loading = true;
            fn.then(() => {
                ElMessage.success(t('common.opered'));
            }).catch((err) => {
                console.log(err);
                ElMessage.error(t('common.operFail'));
            })
        }
        const handleTuntapIP = (_tuntap,access) => {
            if(!props.config && machineId.value != _tuntap.MachineId){
                    ElMessage.success(t('common.access'));
                return;
            }
            if(machineId.value === _tuntap.MachineId){
                if(!access.TuntapChangeSelf){
                    ElMessage.success(t('common.access'));
                    return;
                }
            }else{
                if(!access.TuntapChangeOther){
                    ElMessage.success(t('common.access'));
                    return;
                }
            }
            _tuntap.device = props.item;
            tuntap.value.current = _tuntap;
            tuntap.value.showEdit = true;
        }
        const handleTuntapRefresh = ()=>{
           refreshTuntap();
        }

        return {
            item:computed(()=>props.item),tuntap,showDelay, handleTuntap, handleTuntapIP,handleTuntapRefresh
        }
    }
}
</script>

<style lang="stylus" scoped>

@keyframes loading {
    from{transform:rotate(0deg)}
    to{transform:rotate(360deg)}
}
.el-icon.loading,a.loading{
    vertical-align:middle;font-weight:bold;
    animation:loading 1s linear infinite;
}
.el-switch{
    height:1.8rem;
    line-height:1.8rem;
    &.is-disabled{opacity :1;}
}
.remark{
    padding-left:.4rem;
    text-align:right;
    white-space: nowrap;      /* 禁止换行 */
    overflow: hidden;         /* 隐藏超出部分 */
    text-overflow: ellipsis;  /* 显示省略号 */
    max-width: 100%;
    color:#666;
}

.delay{position: absolute;right:-8px;bottom:-8px;line-height:normal}
.nowrap{line-height:1.8rem;}
</style>