<template>
    <div class="action-wrap">
        <el-card shadow="never">
            <template #header>设置定义验证的静态Json参数</template>
            <div>
                <el-input v-model="state.list" :rows="10" type="textarea" resize="none" @change="handleSave"/>
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
import { setArgs } from '@/apis/action';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import {  reactive } from 'vue'
export default {
    setup(props) {
        const globalData = injectGlobalData();
        const state = reactive({
            list:globalData.value.config.Client.Action.Args[globalData.value.config.Client.ServerInfo.Host] || ''
        });
        const handleSave = ()=>{
            try{
                if(state.list && typeof(JSON.parse(state.list)) != 'object'){
                    ElMessage.error('Json格式错误');
                    return;
                }
            }catch(e){
                ElMessage.error('Json格式错误');
                return;
            }
            const json = {};
            json[globalData.value.config.Client.ServerInfo.Host] = state.list;
            setArgs(json).then(()=>{
                ElMessage.success('已操作');
            }).catch(()=>{
                ElMessage.error('操作失败');
            });;
        }

        return {state,handleSave}
    }
}
</script>
<style lang="stylus" scoped>
.action-wrap{
    font-size:1.3rem;
    padding:1.5rem
}
</style>