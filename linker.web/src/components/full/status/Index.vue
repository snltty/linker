<template>
    <div class="status-wrap flex">
        <div class="copy">
            <a href="https://github.com/snltty/linker" target="_blank">snltty©linker</a>
            <a href="javascript:;" class="download" @click="handleUpdate()" :title="updateText" :class="updateColor">
                <span>
                    <span>{{self.Version}}</span>
                    <template v-if="updater.Version">
                        <template v-if="updater.Status == 1">
                            <el-icon size="14" class="loading"><Loading /></el-icon>
                        </template>
                        <template v-else-if="updater.Status == 2">
                            <el-icon size="14"><Download /></el-icon>
                        </template>
                        <template v-else-if="updater.Status == 3 || updater.Status == 5">
                            <el-icon size="14" class="loading"><Loading /></el-icon>
                            <span class="progress" v-if="updater.Length ==0">0%</span>
                            <span class="progress" v-else>{{parseInt(updater.Current/updater.Length*100)}}%</span>
                        </template>
                        <template v-else-if="updater.Status == 6">
                            <el-icon size="14" class="yellow"><CircleCheck /></el-icon>
                        </template>
                    </template>
                    <template v-else>
                        <el-icon size="14"><Download /></el-icon>
                    </template>
                </span>
            </a>
        </div>
        
        <div class="flex-1"></div>
        <div class="api"><Api :config="config"></Api></div>
        <div class="server" ><Server :config="config"></Server></div>
    </div>
</template>
<script>
import { computed, h, ref } from 'vue';
import Api from './Api.vue'
import Server from './Server.vue'
import { injectGlobalData } from '@/provide';
import {Download,Loading,CircleCheck} from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox,ElSelect,ElOption } from 'element-plus';
import { confirm, exit } from '@/apis/updater';
export default {
    components:{Api,Server,Download,Loading,CircleCheck},
    props:['config'],
    setup(props) {

        const globalData = injectGlobalData();
        const updater = computed(()=>globalData.value.updater);
        const updaterVersion = computed(()=>updater.value.Version);
        const self  = computed(()=>globalData.value.self);
        const serverVersion = computed(()=>globalData.value.signin.Version);
        
        const updaterMsg = computed(()=>{
            return `${updater.value.Version}->${updater.value.DateTime}\n${updater.value.Msg.map((value,index)=>`${index+1}、${value}`).join('\n')}`;
        });
        const updateText = computed(()=>{
            if(!updater.value.Version || !self.value.Version){
                return '未检测到更新';
            }
            
            if(updater.value <= 2) {
                return updater.value.Version != serverVersion.value 
                ? `与服务器版本(${serverVersion.value})不一致，建议更新` 
                : self.value.Version != updater.value.Version 
                    ? `不是最新版本(${self.value.Version})，建议更新\n${updaterMsg.value}` 
                    : `是最新版本，但我无法阻止你喜欢更新\n${updaterMsg.value}`
            }
            return {
                3:'正在下载',
                4:'已下载',
                5:'正在解压',
                6:'已解压，请重启',
            }[updater.value.Status];
        });
        const updateColor = computed(()=>{
            return updater.value.Version != serverVersion.value 
            ? 'red' 
            : self.value.Version != updater.value.Version 
                ? 'yellow' :'green'
        });
        const handleUpdate = ()=>{
            if(!updater.value.Version){
                ElMessage.error('未检测到更新');
                return;
            }
            //未检测，检测中，下载中，解压中
            if([0,1,3,5].indexOf(updater.value.Status)>=0){
                ElMessage.error('操作中，请稍后!');
                return;
            }
            //已解压
            if(updater.value.Status == 6){
                ElMessageBox.confirm('确定关闭程序吗？', '提示', {
                    confirmButtonText: '确定',
                    cancelButtonText: '取消',
                    type: 'warning'
                }).then(() => {
                    exit(self.value.MachineId);
                }).catch(() => {});
                return;
            }

            //已检测
            if(updater.value.Status == 2){
                const selectedValue = ref(updaterVersion.value);
                const selectOptions = [
                    h(ElOption, { label: `仅[${self.value.MachineName}] -> ${updaterVersion.value}(最新版本)`, value: updaterVersion.value }),
                ];
                if(props.config){
                    selectOptions.push(h(ElOption, { label: `[所有] -> ${updaterVersion.value}(最新版本)`, value: `all->${updaterVersion.value}` }))
                }
                if(self.value.Version != serverVersion.value && updaterVersion.value != serverVersion.value){
                    selectOptions.push(h(ElOption, { label: `仅[${self.value.MachineName}] -> ${serverVersion.value}(服务器版本)`, value: serverVersion.value }));
                    if(props.config){
                        selectOptions.push(h(ElOption, { label: `[所有] -> ${serverVersion.value}(服务器版本)`, value: `all->${serverVersion.value}` }));
                    }
                }

                ElMessageBox({
                    title: '选择版本',
                    message: () => h(ElSelect, {
                        modelValue: selectedValue.value,
                        placeholder: '请选择',
                        style:'width:20rem;',
                        'onUpdate:modelValue': (val) => {
                            selectedValue.value = val
                        }
                    }, selectOptions),
                    confirmButtonText: '确定',
                    cancelButtonText: '取消'
                }).then(() => {
                    const data = {
                        MachineId:self.value.MachineId,
                        Version:selectedValue.value.replace('all->',''),
                        All:selectedValue.value.indexOf('all->') >= 0
                    };
                    if(data.All){
                        data.MachineId = '';
                    }
                    confirm(data);
                }).catch(() => {});
            }
        }

        return {
            config:props.config,self,updater,updateText,updateColor,handleUpdate
        }
    }
}
</script>
<style lang="stylus" scoped>
.status-wrap{
    border-top:1px solid #ddd;
    background-color:#f5f5f5;
    height:3rem;
    line-height:3rem;
    font-size:1.2rem;
    color:#555;
    

    .copy{
        padding-left:.5rem;
        a{color:#555;}
    }

    a.download{
        margin-left:.6rem
        .el-icon{
            vertical-align:text-bottom;font-weight:bold;
            &.loading{
                animation:loading 1s linear infinite;
            }

            margin-left:.3rem
        }
    }
}
</style>