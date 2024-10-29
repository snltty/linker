<template>
    <div>
        <div class="flex">
            <div class="flex-1">
                <a href="javascript:;" class="a-line" @click="handleSocks5Port(socks5.list[item.MachineId])" title="此设备的socks5代理">
                    <template v-if="socks5.list[item.MachineId].SetupError">
                        <strong class="red" :title="socks5.list[item.MachineId].SetupError">
                            socks5://*:{{ socks5.list[item.MachineId].Port }}
                        </strong>
                    </template>
                    <template v-else>
                        <template v-if="socks5.list[item.MachineId].running">
                            <strong class="green gateway">socks5://*:{{ socks5.list[item.MachineId].Port }}</strong>
                        </template>
                        <template v-else>
                            <strong>socks5://*:{{ socks5.list[item.MachineId].Port }}</strong>
                        </template>
                    </template>
                </a>
            </div>
            <template v-if="socks5.list[item.MachineId].loading">
                <div>
                    <el-icon size="14" class="loading"><Loading /></el-icon>
                </div>
            </template>
            <template v-else>
                <el-switch v-model="socks5.list[item.MachineId].running" :loading="socks5.list[item.MachineId].loading" disabled @click="handleSocks5(socks5.list[item.MachineId])"  size="small" inline-prompt active-text="😀" inactive-text="😣" > 
                </el-switch>
            </template>
        </div>
        <div>
            <div>
                <template v-for="(item1,index) in  socks5.list[item.MachineId].Lans" :key="index">
                    <template v-if="item1.Disabled">
                        <div class="flex yellow" title="已禁用">{{ item1.IP }} / {{ item1.PrefixLength }}</div>
                    </template>
                    <template v-else-if="item1.Exists">
                        <div class="flex red" title="与其它设备填写IP、或本机局域网IP有冲突">{{ item1.IP }} / {{ item1.PrefixLength }}</div>
                    </template>
                    <template v-else>
                        <div class="flex" title="正常使用" :class="{green:socks5.list[item.MachineId].running}">{{ item1.IP }} / {{ item1.PrefixLength }}</div>
                    </template>
                </template>
            </div>
        </div>
    </div>
</template>

<script>
import { stopSocks5, runSocks5 } from '@/apis/socks5';
import { ElMessage } from 'element-plus';
import { useSocks5 } from './socks5';
import {Loading} from '@element-plus/icons-vue'
import { injectGlobalData } from '@/provide';
import { computed } from 'vue';
export default {
    props:['item','config'],
    emits: ['edit','refresh'],
    components:{Loading},
    setup (props,{emit}) {
        
        const socks5 = useSocks5();
        const globalData = injectGlobalData();
        const machineId = computed(() => globalData.value.config.Client.Id);
        const hasSocks5ChangeSelf = computed(()=>globalData.value.hasAccess('Socks5ChangeSelf')); 
        const hasSocks5ChangeOther = computed(()=>globalData.value.hasAccess('Socks5ChangeOther')); 
        const hasSocks5StatusSelf = computed(()=>globalData.value.hasAccess('Socks5StatusSelf')); 
        const hasSocks5StatusOther = computed(()=>globalData.value.hasAccess('Socks5StatusOther')); 
        const handleSocks5 = (socks5) => {
            if(!props.config){
                return;
            }
            if(machineId.value === socks5.MachineId){
                if(!hasSocks5StatusSelf.value){
                    return;
                }
            }else{
                if(!hasSocks5StatusOther.value){
                    return;
                }
            }
            const fn = socks5.running ? stopSocks5 (socks5.MachineId) : runSocks5(socks5.MachineId);
            socks5.loading = true;
            fn.then(() => {
                ElMessage.success('操作成功！');
            }).catch(() => {
                ElMessage.error('操作失败！');
            })
        }
        const handleSocks5Port = (socks5) => {
            if(!props.config && machineId.value != socks5.MachineId){
                return;
            }
            if(machineId.value === socks5.MachineId){
                if(!hasSocks5ChangeSelf.value){
                    return;
                }
            }else{
                if(!hasSocks5ChangeOther.value){
                    return;
                }
            }
            socks5.device = props.item;
            emit('edit',socks5);
        }
        const handleSocks5Refresh = ()=>{
            emit('refresh');
        }

        return {
            item:computed(()=>props.item),socks5,  handleSocks5, handleSocks5Port,handleSocks5Refresh
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

.switch-btn{
    font-size:1.5rem;
}

</style>