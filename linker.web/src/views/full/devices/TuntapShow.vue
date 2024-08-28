<template>
    <div>
        <div class="flex">
            <div class="flex-1">
                <a href="javascript:;" class="a-line" @click="handleTuntapIP(tuntap.list[item.MachineId])" :title="tuntap.list[item.MachineId].Gateway?'我在路由器上，所以略有不同':'此设备的虚拟网卡IP'">
                    <template v-if="tuntap.list[item.MachineId].Error">
                        <el-popover placement="top" title="msg" width="20rem"  trigger="hover" :content="tuntap.list[item.MachineId].Error">
                            <template #reference>
                                <strong class="red">{{ tuntap.list[item.MachineId].IP }}</strong>
                            </template>
                        </el-popover>
                    </template>
                    <template v-else>
                        <template v-if="tuntap.list[item.MachineId].running">
                            <strong class="green" :class="{gateway:tuntap.list[item.MachineId].Gateway}">{{ tuntap.list[item.MachineId].IP }}</strong>
                        </template>
                        <template v-else>
                            <strong :class="{gateway:tuntap.list[item.MachineId].Gateway}">{{ tuntap.list[item.MachineId].IP }}</strong>
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
            <template v-if="tuntap.list[item.MachineId].Error1">
                <el-popover placement="top" title="msg" width="20rem"  trigger="hover" :content="tuntap.list[item.MachineId].Error1">
                    <template #reference>
                        <div class="yellow">
                            <template v-for="(item1,index) in  tuntap.list[item.MachineId].LanIPs" :key="index">
                                <div>
                                    {{ item1 }} / {{ tuntap.list[item.MachineId].Masks[index] }}
                                </div>
                            </template>
                        </div>
                    </template>
                </el-popover>
            </template>
            <template v-else>
                <div>
                    <template v-for="(item1,index) in  tuntap.list[item.MachineId].LanIPs" :key="index">
                        <div>
                            {{ item1 }} / {{ tuntap.list[item.MachineId].Masks[index] }}
                        </div>
                    </template>
                </div>
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
import {Loading} from '@element-plus/icons-vue'
import { injectGlobalData } from '@/provide';
import { computed } from 'vue';
export default {
    props:['item','config'],
    emits: ['edit','refresh'],
    components:{Loading},
    setup (props,{emit}) {
        
        const tuntap = useTuntap();
        const globalData = injectGlobalData();
        const machineId = computed(() => globalData.value.config.Client.Id);
        const showDelay = computed(()=>((globalData.value.config.Running.Tuntap || {Switch:0}).Switch & 2) == 2);
        const handleTuntap = (tuntap) => {
            if(!props.config && machineId.value != tuntap.MachineId){
                return;
            }
            const fn = tuntap.running ? stopTuntap (tuntap.MachineId) : runTuntap(tuntap.MachineId);
            tuntap.loading = true;
            fn.then(() => {
                ElMessage.success('操作成功！');
            }).catch(() => {
                ElMessage.error('操作失败！');
            })
        }
        const handleTuntapIP = (tuntap) => {
            if(!props.config && machineId.value != tuntap.MachineId){
                return;
            }
            emit('edit',tuntap);
        }
        const handleTuntapRefresh = ()=>{
            emit('refresh');
        }

        return {
            item:props.item,tuntap,showDelay,  handleTuntap, handleTuntapIP,handleTuntapRefresh
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

.gateway{
    background:linear-gradient(90deg, #c5b260, #858585, #c5b260, #858585);
    -webkit-background-clip:text;
    -webkit-text-fill-color:hsla(0,0%,100%,0);
    &.green{
        background:linear-gradient(90deg, #e4bb10, #008000, #e4bb10, #008000);
        -webkit-background-clip:text;
        -webkit-text-fill-color:hsla(0,0%,100%,0);
    }
}
.delay{position: absolute;right:0;bottom:0;line-height:normal}

.switch-btn{
    font-size:1.5rem;
}

</style>