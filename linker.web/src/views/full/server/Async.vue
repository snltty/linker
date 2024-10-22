<template>
    <div :style="{height:`${state.height}px`}">
        <el-card shadow="never">
            <template #header>选择你需要同步的项，将这些配置同步到本组所有客户端</template>
            <div>
                <el-checkbox v-model="state.checkAll" :indeterminate="state.isIndeterminate" @change="handleCheckAllChange">全选</el-checkbox>
                <el-checkbox-group v-model="state.checkeds" @change="handleCheckedsChange">
                    <el-row>
                        <template v-for="name in state.names">
                            <el-col :span="8">
                                <el-checkbox :key="name.name" :label="name.label" :value="name.name">{{ name.label }}</el-checkbox>
                            </el-col>
                        </template>
                    </el-row>
                </el-checkbox-group>
            </div>
            <template #footer>
                <div class="t-c">
                    <el-button type="success" @click="handleSync">确定同步</el-button>
                </div>
            </template>
        </el-card>
    </div>
</template>
<script>
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, onMounted, reactive } from 'vue'
import { getSyncNames, setSync } from '@/apis/config';
export default {
    label:'同步配置',
    name:'async',
    order:7,
    setup(props) {
        const globalData = injectGlobalData();
        const state = reactive({
            names:[],
            checkAll:false,
            isIndeterminate:false,
            checkeds:[],
            height: computed(()=>globalData.value.height-90),
        });

        const handleCheckAllChange = (val)=>{
            state.checkeds = val ? state.names.map(c=>c.name) : [];
            state.isIndeterminate = false
        }
        const handleCheckedsChange = (value)=>{
            const checkedCount = value.length
            state.checkAll = checkedCount === state.names.length
            state.isIndeterminate = checkedCount > 0 && checkedCount < state.names.length;
        }

        const labels = {
            'SignInSecretKey':'当前信标密钥',
            'GroupSecretKey':'当前分组密码',
            'RelaySecretKey':'当前中继密钥',
            'SForwardSecretKey':'当前服务器穿透密钥',
            'UpdaterSecretKey':'服务器更新密钥',
            'TunnelTransports':'打洞协议列表'
        }
        onMounted(()=>{
            getSyncNames().then(res=>{
                state.names = res.map(c=>{
                    return {name:c,label:labels[c]}
                });
            });
        });
        const handleSync = ()=>{
            if(state.checkeds.length == 0) {
                ElMessage.error('至少选择一个');
                return;
            }
            setSync(state.checkeds).then(res=>{
                ElMessage.success('已操作');
            });
        }

        return {state,handleCheckAllChange,handleCheckedsChange,handleSync}
    }
}
</script>
<style lang="stylus" scoped>
    
</style>