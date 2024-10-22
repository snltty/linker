<template>
    <el-form-item label="中继密钥">
        <el-input type="password" show-password v-model="state.list.SecretKey" maxlength="36" @change="handleSave" />
        <el-checkbox v-model="state.list.SSL" label="使用ssl" size="large" @change="handleSave" />
        <el-checkbox v-model="state.list.Disabled" label="禁用中继" size="large" @change="handleSave" />
        <span class="delay" :class="{red:state.list.Delay==-1,green:state.list.Delay>=0}">{{state.list.Delay }}ms</span>
    </el-form-item>
</template>
<script>
import { setRelayServers, setRelaySubscribe } from '@/apis/relay';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { onMounted, onUnmounted, reactive, watch } from 'vue'
export default {
    setup(props) {
        const globalData = injectGlobalData();
        const state = reactive({
            list:globalData.value.config.Client.Relay.Server,
            timer:0
        });
        watch(()=>globalData.value.config.Client.Relay.Server,()=>{
            state.list.Delay = globalData.value.config.Client.Relay.Server.Delay;
        })

        const handleSave = ()=>{
            setRelayServers(state.list).then(()=>{
                ElMessage.success('已操作');
            }).catch(()=>{
                ElMessage.error('操作失败');
            });;
        }

        const _setRelaySubscribe = ()=>{
            setRelaySubscribe().then(()=>{
                state.timer = setTimeout(_setRelaySubscribe,1000);
            }).catch(()=>{
                state.timer = setTimeout(_setRelaySubscribe,1000);
            });
        }
        onMounted(()=>{
            _setRelaySubscribe();
        });
        onUnmounted(()=>{
            clearTimeout(state.timer);
        })

        return {state,handleSave}
    }
}
</script>
<style lang="stylus" scoped>
.delay{margin-left:3rem;}
.green,.red{font-weight:bold;}
</style>