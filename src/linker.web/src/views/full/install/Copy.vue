<template>
    <div>
        <div>
            <el-input v-trim v-model="state.content" type="textarea" :rows="10" resize="none"></el-input>
        </div>
        <div class="t-c mgt-1">
            <el-button type="primary" @click="handleSave">确定</el-button>
        </div>
    </div>
</template>

<script>
import { installCopy } from '@/apis/config';
import { ElMessage } from 'element-plus';
import { reactive } from 'vue';

export default {
    setup () {
        
        const state = reactive({ content:'' })
        const handleSave = ()=>{
            if(!state.content) return;
            installCopy(state.content).then((res)=>{
                if(!res){
                    ElMessage.error('保存失败，可能格式有误，无法解析');
                    return;
                }
                ElMessage.success('保存成功');
                window.location.reload();
            }).catch(()=>{
                ElMessage.error('保存失败');
            })
        }
        return {
            state,handleSave
        }
    }
}
</script>

<style lang="stylus" scoped>
</style>