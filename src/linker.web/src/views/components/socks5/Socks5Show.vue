<template>
    <AccessBoolean value="Socks5ChangeSelf,Socks5ChangeOther,Socks5StatusSelf,Socks5StatusOther">
        <template #default="{values}">
            <div class="flex">
                <div class="flex-1">
                    <ConnectionShow :row="item" transactionId="socks5"></ConnectionShow>
                    <a href="javascript:;" class="a-line" @click="handleSocks5Port(item.hook_socks5,values)" title="此设备的socks5代理">
                        <template v-if="item.hook_socks5.SetupError">
                            <strong class="red" :title="item.hook_socks5.SetupError">
                                socks5://*:{{ item.hook_socks5.Port }}
                            </strong>
                        </template>
                        <template v-else>
                            <template v-if="item.Connected &&item.hook_socks5.running">
                                <strong class="green gateway">socks5://*:{{ item.hook_socks5.Port }}</strong>
                            </template>
                            <template v-else>
                                <span>socks5://*:{{ item.hook_socks5.Port }}</span>
                            </template>
                        </template>
                    </a>
                </div>
                <template v-if="item.hook_socks5.loading">
                    <div>
                        <el-icon size="14" class="loading"><Loading /></el-icon>
                    </div>
                </template>
                <template v-else>
                    <el-switch :model-value="item.Connected && item.hook_socks5.running" :loading="item.hook_socks5.loading" disabled @click="handleSocks5(item.hook_socks5,values)"  size="small" inline-prompt active-text="😀" inactive-text="😣" > 
                    </el-switch>
                </template>
            </div>
            <div>
                <div>
                    <template v-for="(item1,index) in  item.hook_socks5.Lans" :key="index">
                        <template v-if="item1.Disabled">
                            <div class="flex disable" title="已禁用">
                                <span>{{ item1.IP }}/{{ item1.PrefixLength }}</span>
                                <span class="flex-1 remark" :title="item1.Remark">{{ item1.Remark }}</span>
                            </div>
                        </template>
                        <template v-else-if="item1.Exists">
                            <div class="flex yellow" title="与其它设备填写IP、或本机局域网IP有冲突">
                                <span>{{ item1.IP }}/{{ item1.PrefixLength }}</span>
                                <span class="flex-1 remark" :title="item1.Remark">{{ item1.Remark }}</span>
                            </div>
                        </template>
                        <template v-else>
                            <div class="flex green" title="正常使用" :class="{green:item.Connected && item.hook_socks5.running}">
                                <span>{{ item1.IP }}/{{ item1.PrefixLength }}</span>
                                <span class="flex-1 remark" :title="item1.Remark">{{ item1.Remark }}</span>
                            </div>
                        </template>
                    </template>
                </div>
            </div>
        </template>
    </AccessBoolean>
</template>

<script>
import { stopSocks5, runSocks5 } from '@/apis/socks5';
import { ElMessage } from 'element-plus';
import { useSocks5 } from './socks5';
import {Loading} from '@element-plus/icons-vue'
import { injectGlobalData } from '@/provide';
import { computed } from 'vue';
import ConnectionShow from '../tunnel/ConnectionShow.vue';
export default {
    props:['item','config'],
    components:{Loading,ConnectionShow},
    setup (props) {
        
        const socks5 = useSocks5();
        const globalData = injectGlobalData();
        const machineId = computed(() => globalData.value.config.Client.Id);

        const handleSocks5 = (_socks5,access) => {
            if(!props.config){
                return;
            }
            if(machineId.value === _socks5.MachineId){
                if(!access.Socks5StatusSelf){
                ElMessage.success('无权限');
                return;
            }
            }else{
                if(!access.Socks5StatusOther){
                ElMessage.success('无权限');
                return;
            }
            }
            const fn = props.item.Connected && _socks5.running ? stopSocks5 (_socks5.MachineId) : runSocks5(_socks5.MachineId);
            _socks5.loading = true;
            fn.then(() => {
                ElMessage.success('操作成功！');
            }).catch((err) => {
                console.log(err);
                ElMessage.error('操作失败！');
            })
        }
        const handleSocks5Port = (_socks5,access) => {
            if(!props.config && machineId.value != _socks5.MachineId){
                return;
            }
            if(machineId.value === _socks5.MachineId){
                if(!access.Socks5ChangeSelf){
                ElMessage.success('无权限');
                return;
            }
            }else{
                if(!access.Socks5ChangeOther){
                ElMessage.success('无权限');
                return;
            }
            }
            _socks5.device = props.item;
            socks5.value.current = _socks5;
            socks5.value.showEdit = true;
        }

        return {
            socks5, handleSocks5, handleSocks5Port,
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

.remark{
    padding-left:.4rem;
    text-align:right;
    white-space: nowrap;      /* 禁止换行 */
    overflow: hidden;         /* 隐藏超出部分 */
    text-overflow: ellipsis;  /* 显示省略号 */
    max-width: 100%;
    color:#666;
}

.switch-btn{
    font-size:1.5rem;
}

</style>