<template>
    <div class="signin-wrap" :style="{height:`${state.height}px`}">
        <el-card shadow="never">
            <template #header>服务器相关设置</template>
            <div>
                <el-form label-width="auto">
                    <el-form-item label="服务器地址">
                        <div class="flex">
                            <el-input class="flex-1" v-model="state.list.Host" @change="handleSave" />
                            <span>服务器地址。ip:端口 或者 域名:端口</span>
                        </div>
                    </el-form-item>
                    <el-form-item label="信标密钥">
                        <div class="flex">
                            <el-input class="flex-1" type="password" show-password maxlength="36" v-model="state.list.SecretKey" @change="handleSave" />
                            <span>密钥正确时可连接服务器</span>
                        </div>
                    </el-form-item>
                    <RelayServers></RelayServers>
                    <SForward></SForward>
                    <Updater></Updater>
                </el-form>
            </div>
            <template #footer>
                <div class="t-c">
                    <el-button type="success" @click="handleSave">确定更改</el-button>
                </div>
            </template>
        </el-card>
    </div>
</template>
<script>
import { setSignInServers } from '@/apis/signin';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, reactive } from 'vue'
import SForward from './SForward.vue';
import Updater from './Updater.vue';
import RelayServers from './RelayServers.vue';
export default {
    components:{SForward,Updater,RelayServers},
    setup(props) {
        const globalData = injectGlobalData();
        const state = reactive({
            list:globalData.value.config.Client.Server,
            height: computed(()=>globalData.value.height-90),
        });

        const handleSave = ()=>{
            setSignInServers(state.list).then(()=>{
                ElMessage.success('已操作，请在右下角【信标服务器】重连');
            }).catch((err)=>{
                console.log(err);
                ElMessage.error('操作失败');
            });
        }

        return {state,handleSave}
    }
}
</script>
<style lang="stylus" scoped>
    
</style>