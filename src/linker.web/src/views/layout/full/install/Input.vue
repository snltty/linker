<template>
    <div>
        <div class="head">
            <el-steps :active="step.step" finish-status="success">
                <template v-for="(item,index) in state.steps">
                    <el-step :title="item" />
                </template>
            </el-steps>
        </div>
        <div class="body">
            <el-card shadow="never" v-if="step.step == 1">
                <Common ref="currentDom"></Common>
            </el-card>
            <el-card shadow="never"  v-if="step.step == 2">
                <Server ref="currentDom"></Server>
            </el-card>
            <el-card shadow="never"  v-if="step.step == 3">
                <Client ref="currentDom"></Client>
            </el-card>
            <el-card shadow="never"  v-if="step.step == 4">
                <div class="t-c">完成保存后，请重启软件</div>
            </el-card>
        </div>
        <div class="footer t-c">
            <el-button :disabled="step.step <= 1" @click="handlePrev">上一步</el-button>
            <el-button v-if="step.step < state.steps.length" type="primary" @click="handleNext">下一步</el-button>
            <el-button v-else type="primary" @click="handleSave">完成</el-button>
        </div>
    </div>
</template>
<script>
import { injectGlobalData } from '@/provide';
import { install } from '@/apis/config';
import { reactive,   ref, provide, computed } from 'vue';
import { ElMessage } from 'element-plus';
import Common from './Common.vue'
import Client from './Client.vue'
import Server from './Server.vue'
export default {
    components: { Common,Client,Server },
    setup(props) {

        const globalData = injectGlobalData();
        const state = reactive({
            steps:computed(()=>['选择模式', globalData.value.isPc ? '服务端' : '','客户端','完成'])
        });

        const currentDom = ref(null);
        const step = ref({
            step:1,
            increment:1,
            json:{},
            form:{server:{},client:{},common:{}}
        });
        provide('step',step);
        const handlePrev = ()=>{
            step.value.step --;
            step.value.increment = -1;
        }
        const handleNext = ()=>{
            step.value.increment = 1;
            currentDom.value.handleValidate().then((json)=>{
                step.value.json = Object.assign(step.value.json,json.json);
                step.value.form = Object.assign(step.value.form,json.form);
                step.value.step ++;
            }).catch(()=>{
            });
        }
        const handleSave = ()=>{
            install(step.value.json).then(()=>{
                ElMessage.success('保存成功');
                window.location.reload();
            }).catch(()=>{
                ElMessage.error('保存失败');
            })
        }

        return { state,currentDom,step,handlePrev,handleNext,handleSave};
    }
}
</script>
<style lang="stylus" scoped>
.body{margin-top:1rem;}
.footer{
    margin-top:2rem
}
</style>