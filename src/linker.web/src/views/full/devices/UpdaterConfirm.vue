<template>
    <el-dialog class="options-center" title="更新" destroy-on-close v-model="state.show" width="40rem" top="2vh">
        <div class="updater-wrap t-c">
            <div class="t-l">
                <ul>
                    <li v-for="item in state.msg">{{ item }}</li>
                </ul>
            </div>
            <div class="flex mgt-1">
                <el-row class="w-100">
                    <el-col :span="10">
                        <el-select v-model="state.type" size="large">
                            <el-option v-for="item in state.types" :key="item.value" :label="item.label" :value="item.value" />
                        </el-select>
                    </el-col>
                    <el-col :span="4">
                        ->
                    </el-col>
                    <el-col :span="10">
                        <el-select v-model="state.version" size="large" filterable allow-create default-first-option>
                            <el-option v-for="item in state.versions" :key="item.value" :label="item.label" :value="item.value" />
                        </el-select>
                    </el-col>
                </el-row>
            </div>
            <div class="mgt-1 t-c">
                <el-button type="success" @click="handleUpdate" plain>确 定</el-button>
            </div>
            
        </div>
    </el-dialog>
</template>

<script>
import { injectGlobalData } from '@/provide';
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { confirm, getUpdaterMsg } from '@/apis/updater';
import { useUpdater } from './updater';

export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    setup (props,{emit}) {

        const globalData = injectGlobalData();
        const hasUpdateSelf = computed(()=>globalData.value.hasAccess('UpdateSelf')); 
        const hasUpdateOther = computed(()=>globalData.value.hasAccess('UpdateOther')); 
        const updater = useUpdater();
        const serverVersion = computed(()=>globalData.value.signin.Version);
        const updaterVersion = computed(()=>updater.value.current.Version);

        const types = [
                {label:`仅【${updater.value.device.MachineName}】`,value:updater.value.device.MachineId},
                hasUpdateOther.value ? {label:`本组所有`,value:'g-all'} : {},
                hasUpdateOther.value ?  {label:`本服务器所有`,value:'s-all'} : {},
            ].filter(c=>c.value);
        const versions = [
                {label:`${updaterVersion.value}【最新版本】`,value:updaterVersion.value},
                {label:`${serverVersion.value}【服务器版本】`,value:serverVersion.value},
            ].filter(c=>c.value);
        const state = reactive({
            show: true,
            type:types[0] || '',
            version:versions[0] || '',
            types:types,
            versions:versions,
            msg:[]
        });
        
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });
        const handleUpdate = ()=>{   
            const data = {
                MachineId:updater.value.device.MachineId,
                Version:state.version.value || state.version,
                GroupAll:state.type == 'g-all',
                All:state.type == 's-all',
            };
            if(data.All || data.GroupAll){
                data.MachineId = '';
            }
            confirm(data).then(()=>{
            }).catch(()=>{
            });
            state.show = false;
        }

        onMounted(()=>{
            getUpdaterMsg().then((res)=>{
                state.msg = res.Msg;
            });
        });

        return {
            state,updater,handleUpdate
        }
    }
}
</script>

<style lang="stylus" scoped>
</style>