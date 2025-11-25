<template>
    <AccessBoolean value="TuntapChangeSelf,TuntapChangeOther,TuntapStatusSelf,TuntapStatusOther">
        <template #default="{values}">
            <div class="flex">
                <div class="flex-1">
                    <ConnectionShow :row="item" transactionId="tuntap"></ConnectionShow>         
                    <a href="javascript:;" class="a-line" @click="handleTuntapIP(item.hook_tuntap,values)" title="虚拟网卡IP">
                        <template v-if="item.Connected">
                            <template v-if="item.hook_tuntap.SetupError">
                                <strong class="red" :title="`setup ${item.hook_tuntap.SetupError}`">{{ item.hook_tuntap.IP }}</strong>
                            </template>
                            <template v-else-if="item.hook_tuntap.Exists">
                                <strong class="red" title="IP存在冲突，请使用新IP">{{ item.hook_tuntap.IP }}</strong>
                            </template>
                            <template v-else-if="item.hook_tuntap.Available == false">
                                <strong class="disable" title="IP不生效，可能是设备不在线">{{ item.hook_tuntap.IP }}</strong>
                            </template>
                            <template v-else-if="item.hook_tuntap.NatError">
                                <strong class="yellow" :title="`nat ${item.hook_tuntap.NatError}`">{{ item.hook_tuntap.IP }}</strong>
                            </template>
                            <template v-else-if="item.hook_tuntap.AppNat && item.hook_tuntap.running">
                                <strong class="app-nat" :title="`虚拟网卡IP\r\n应用层DNAT`">{{ item.hook_tuntap.IP }}</strong>
                            </template>
                            <template v-else-if="item.hook_tuntap.running">
                                <strong class="green gateway" :title="`虚拟网卡IP\r\n系统NAT`">{{ item.hook_tuntap.IP }}</strong>
                            </template>
                            <template v-else>
                                <strong>{{ item.hook_tuntap.IP }}</strong>
                            </template>
                        </template>
                        <template v-else>
                            <strong class="disable" title="IP不生效，可能是设备不在线">{{ item.hook_tuntap.IP }}</strong>
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
            <div>
                <template v-for="(item1,index) in  item.hook_tuntap.Lans" :key="index">
                    <template v-if="item.hook_tuntap.Available == false">
                        <div class="lan flex disable" title="IP不生效，可能是设备不在线">
                            <span>{{ item1.IP }}/{{ item1.PrefixLength }}</span>
                            <span class="flex-1 remark" :title="item1.Remark">{{ item1.Remark }}</span>
                        </div>
                    </template>
                    <template v-else-if="item1.Disabled">
                        <div class="lan flex disable" title="已禁用">
                            <span>{{ item1.IP }}/{{ item1.PrefixLength }}</span>
                            <span class="flex-1 remark" :title="item1.Remark">{{ item1.Remark }}</span>
                        </div>
                    </template>
                    <template v-else-if="item1.Exists">
                        <div class="lan flex yellow" title="与其它设备填写IP、或本机局域网IP有冲突、或与本机外网IP一致">
                            <span>{{ item1.IP }}/{{ item1.PrefixLength }}</span>
                            <span class="flex-1 remark" :title="item1.Remark">{{ item1.Remark }}</span>
                        </div>
                    </template>
                    <template v-else>
                        <div class="lan flex green" title="正常使用">
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
export default {
    props:['item','config'],
    components:{Loading,Share,ConnectionShow},
    setup (props) {
        
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
                ElMessage.success('无权限');
                return;
            }
            }else{
                if(!access.TuntapStatusOther){
                ElMessage.success('无权限');
                return;
            }
            }

            const fn = props.item.Connected && _tuntap.running ? stopTuntap (_tuntap.MachineId) : runTuntap(_tuntap.MachineId);
            _tuntap.loading = true;
            fn.then(() => {
                ElMessage.success('操作成功！');
            }).catch((err) => {
                console.log(err);
                ElMessage.error('操作失败！');
            })
        }
        const handleTuntapIP = (_tuntap,access) => {
            if(!props.config && machineId.value != _tuntap.MachineId){
                ElMessage.success('无权限');
                return;
            }
            if(machineId.value === _tuntap.MachineId){
                if(!access.TuntapChangeSelf){
                    ElMessage.success('无权限');
                    return;
                }
            }else{
                if(!access.TuntapChangeOther){
                    ElMessage.success('无权限');
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

.el-switch.is-disabled{opacity :1;}
.el-input{width:8rem;}
.switch-btn{ font-size:1.5rem;}

.lan{line-height:2rem}
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

</style>