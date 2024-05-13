<template>
<div>
    <div class="flex">
        <div class="flex-1">
            <a href="javascript:;" class="a-line" @click="handleTuntapIP(data)">
                <template v-if="data.running">
                    <strong class="green">{{data.IP }}</strong>
                </template>
                <template v-else>
                    <span>{{ data.IP }}</span>
                </template>
            </a>
        </div>
        <el-button size="small" :loading="data.loading" @click="handleTuntap(data)">
            <span v-if="data.running">卸载</span>
            <span v-else>安装</span>
        </el-button>
    </div>
    <div>{{data.LanIPs.join('、')}}</div>

    <el-dialog v-model="state.show" :close-on-click-modal="false" append-to=".app-wrap" title="设置虚拟网卡IP" width="400">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="80">
                <el-form-item label="网卡IP" prop="IP">
                    <el-input v-model="state.ruleForm.IP" />
                </el-form-item>
                <el-form-item label="局域网IP" prop="LanIP">
                    <template v-for="(item,index) in state.ruleForm.LanIPs" :key="index">
                        <div class="flex" style="margin-bottom:.6rem">
                            <div class="flex-1">
                                <el-input v-model="state.ruleForm.LanIPs[index]" />
                            </div>
                            <div>
                                <el-button type="danger" @click="handleDel(index)">删除</el-button>
                                <el-button type="primary" @click="handleAdd(index)">添加</el-button>
                            </div>
                        </div>
                    </template>
                </el-form-item>
                <el-form-item label="" prop="alert">
                    <div>网卡IP和局域网IP都是 /24 掩码</div>
                </el-form-item>
                <el-form-item label="" prop="Btns">
                    <div>
                        <el-button @click="state.show = false">取消</el-button>
                        <el-button type="primary" @click="handleSave">确认</el-button>
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </el-dialog>
</div>
</template>
<script>
import { stopTuntap ,runTuntap, updateTuntap} from '@/apis/tuntap';
import { ElMessage } from 'element-plus';
import { reactive, ref } from 'vue';

export default {
    props:['data'],
    emits:['change'],
    setup(props,{emit}) {

        const ruleFormRef = ref(null);
        const state = reactive({
            show:false,
            ruleForm:{
                IP:props.data.IP,
                LanIPs:props.data.LanIPs.slice(0)
            },
            rules:{}
        });
        if(state.ruleForm.LanIPs.length == 0){
            state.ruleForm.LanIPs.push('');
        }

        const handleTuntap = (tuntap)=>{
            const fn = tuntap.running ? stopTuntap(tuntap.MachineName) : runTuntap(tuntap.MachineName);
            fn.then(()=>{
                ElMessage.success('操作成功！');
                emit('change');
            }).catch(()=>{
                ElMessage.error('操作失败！');
            })
        }
        const handleTuntapIP = (tuntap)=>{
            state.show = true;
        }
        const handleDel = (index)=>{
            if(state.ruleForm.LanIPs.length == 1) return;
            state.ruleForm.LanIPs.splice(index,1);
        }
        const handleAdd = (index)=>{
            state.ruleForm.LanIPs.splice(index+1,0,'');
        }
        const handleSave = ()=>{
            const json = JSON.parse(JSON.stringify(props.data));
            json.IP = state.ruleForm.IP || '0.0.0.0';
            json.LanIPs = state.ruleForm.LanIPs.filter(c=>c);
            updateTuntap(json).then(()=>{
                state.show = false;
                ElMessage.success('已操作！');
                emit('change')
            }).catch(()=>{
                ElMessage.error('操作失败！');
            });
        }

        return {
            data:props.data,state,ruleFormRef,handleTuntap,handleTuntapIP,handleDel,handleAdd,handleSave
        }
    }
}
</script>
<style lang="stylus" scoped>
    
</style>