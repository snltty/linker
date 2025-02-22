<template>
    <div>
        <div class="flex">
            <div class="flex-1">
                <ConnectionShow :data="connections.list[item.MachineId]" :row="item"></ConnectionShow>         
                <a href="javascript:;" class="a-line" @click="handleTuntapIP(tuntap.list[item.MachineId])" title="虚拟网卡IP">
                    <template v-if="tuntap.list[item.MachineId].SetupError">
                        <strong class="red" :title="tuntap.list[item.MachineId].SetupError">{{ tuntap.list[item.MachineId].IP }}</strong>
                    </template>
                    <template v-else-if="tuntap.list[item.MachineId].Upgrade && tuntap.list[item.MachineId].NatError">
                        <strong class="yellow" :title="tuntap.list[item.MachineId].NatError">{{ tuntap.list[item.MachineId].IP }}</strong>
                    </template>
                    <template v-else>
                        <template v-if="tuntap.list[item.MachineId].running">
                            <strong class="green gateway">{{ tuntap.list[item.MachineId].IP }}</strong>
                        </template>
                        <template v-else>
                            <strong>{{ tuntap.list[item.MachineId].IP }}</strong>
                        </template>
                    </template>
                </a>
            </div>
            <template v-if="tuntap.list[item.MachineId].loading">
                <div>
                    <el-icon size="14" class="loading"><Loading /></el-icon>
                </div>
            </template>
            <template v-else>
                <el-switch v-model="tuntap.list[item.MachineId].running" :loading="tuntap.list[item.MachineId].loading" disabled @click="handleTuntap(tuntap.list[item.MachineId])"  size="small" inline-prompt active-text="😀" inactive-text="😣" > 
                </el-switch>
            </template>
        </div>
        <div>
            <div>
                <template v-for="(item1,index) in  tuntap.list[item.MachineId].Lans" :key="index">
                    <template v-if="item1.Disabled">
                        <div class="flex yellow" title="已禁用">{{ item1.IP }} / {{ item1.PrefixLength }}</div>
                    </template>
                    <template v-else-if="item1.Exists">
                        <div class="flex yellow" title="与其它设备填写IP、或本机局域网IP有冲突">{{ item1.IP }} / {{ item1.PrefixLength }}</div>
                    </template>
                    <template v-else>
                        <div class="flex" title="正常使用" :class="{green:tuntap.list[item.MachineId].running}">{{ item1.IP }} / {{ item1.PrefixLength }}</div>
                    </template>
                </template>
            </div>
            <template v-if="tuntap.list[item.MachineId].Any">
                <div class="any green"><el-icon><Share /></el-icon></div>
            </template>
            <template v-if="showDelay">
                <template v-if="tuntap.list[item.MachineId].Delay>=0 && tuntap.list[item.MachineId].Delay<=100">
                    <div class="delay green">{{ tuntap.list[item.MachineId].Delay }}ms</div>
                </template>
                <template>
                    <div class="delay yellow">{{ tuntap.list[item.MachineId].Delay }}ms</div>
                </template>
            </template>
        </div>
    </div>
</template>

<script>
import { stopTuntap, runTuntap } from '@/apis/tuntap';
import { ElMessage } from 'element-plus';
import { useTuntap } from './tuntap';
import {Loading,Share} from '@element-plus/icons-vue'
import { injectGlobalData } from '@/provide';
import { computed } from 'vue';
import { useTuntapConnections } from './connections';
import ConnectionShow from './ConnectionShow.vue';
export default {
    props:['item','config'],
    emits: ['edit','refresh'],
    components:{Loading,Share,ConnectionShow},
    setup (props,{emit}) {
        
        const tuntap = useTuntap();
        const globalData = injectGlobalData();
        const machineId = computed(() => globalData.value.config.Client.Id);
        const hasTuntapChangeSelf = computed(()=>globalData.value.hasAccess('TuntapChangeSelf')); 
        const hasTuntapChangeOther = computed(()=>globalData.value.hasAccess('TuntapChangeOther')); 
        const hasTuntapStatusSelf = computed(()=>globalData.value.hasAccess('TuntapStatusSelf')); 
        const hasTuntapStatusOther = computed(()=>globalData.value.hasAccess('TuntapStatusOther')); 
        const connections = useTuntapConnections();


        const showDelay = computed(()=>((globalData.value.config.Running.Tuntap || {Switch:0}).Switch & 2) == 2);
        const handleTuntap = (tuntap) => {
            if(!props.config){
                return;
            }
            if(machineId.value === tuntap.MachineId){
                if(!hasTuntapStatusSelf.value){
                    return;
                }
            }else{
                if(!hasTuntapStatusOther.value){
                    return;
                }
            }

            const fn = tuntap.running ? stopTuntap (tuntap.MachineId) : runTuntap(tuntap.MachineId);
            tuntap.loading = true;
            fn.then(() => {
                ElMessage.success('操作成功！');
            }).catch((err) => {
                console.log(err);
                ElMessage.error('操作失败！');
            })
        }
        const handleTuntapIP = (tuntap) => {
            if(!props.config && machineId.value != tuntap.MachineId){
                return;
            }
            if(machineId.value === tuntap.MachineId){
                if(!hasTuntapChangeSelf.value){
                    return;
                }
            }else{
                if(!hasTuntapChangeOther.value){
                    return;
                }
            }
            tuntap.device = props.item;
            emit('edit',tuntap);
        }
        const handleTuntapRefresh = ()=>{
            emit('refresh');
        }

        return {
            item:computed(()=>props.item),tuntap,showDelay,connections,  handleTuntap, handleTuntapIP,handleTuntapRefresh
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
.el-input{
    width:8rem;
}

.delay{position: absolute;right:0;bottom:0;line-height:normal}
.switch-btn{
    font-size:1.5rem;
}

.any {
    position: absolute;left:-7px;top:-2px;line-height:normal
    &.green {
        background: linear-gradient(270deg, #caff00, green, #0d6d23, #e38a00, green);
        background-clip: text;
        -webkit-background-clip: text;
        -webkit-text-fill-color: hsla(0, 0%, 100%, 0);
    }

}
</style>