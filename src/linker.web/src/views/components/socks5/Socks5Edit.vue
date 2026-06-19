<template>
     <el-dialog append-to=".app-wrap" v-model="state.show" :close-on-click-modal="false"  :title="$t('socks5.title',[state.machineName])" top="1vh" width="780">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="140">
                <!-- <el-form-item prop="gateway" class="mb-0">
                    {{$t('socks5.alert')}}
                </el-form-item> -->
                <el-form-item :label="$t('socks5.port')" prop="Port">
                    <el-input v-trim v-model="state.ruleForm.Port"  class="w-14" />
                </el-form-item>
                <div class="upgrade-wrap">
                    <Socks5Lan ref="socks5Dom"></Socks5Lan>
                </div>
                <el-form-item label="" prop="Btns" label-width="0">
                    <div class="t-c w-100">
                        <el-button @click="state.show = false">{{$t('common.cancel')}}</el-button>
                        <el-button type="primary" @click="handleSave">{{$t('common.confirm')}}</el-button>
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </el-dialog>
</template>
<script>
import {updateSocks5 } from '@/apis/socks5';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { reactive, ref, watch } from 'vue';
import { useSocks5 } from './socks5';
import Socks5Lan from './Socks5Lan.vue';
import { useI18n } from 'vue-i18n';
export default {
    props: ['modelValue'],
    emits: ['change','update:modelValue'],
    components: {Socks5Lan},
    setup(props, { emit }) {

        const {t} = useI18n ();
        const globalData = injectGlobalData();
        const socks5 = useSocks5();
        const ruleFormRef = ref(null);
        const state = reactive({
            show: true,
            machineName:socks5.value.current.device.MachineName,
            bufferSize:globalData.value.bufferSize,
            ruleForm: {
                Port: socks5.value.current.Port,
                Lans: []
            },
            rules: {}
        });
        if (state.ruleForm.Lans.length == 0) {
            state.ruleForm.Lans.push({IP:'0.0.0.0',PrefixLength:24});
        }
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const socks5Dom = ref(null);
        const handleSave = () => {
            const json = JSON.parse(JSON.stringify(socks5.value.current,(key,value)=> key =='device'?'':value));
            json.Port = +(state.ruleForm.Port || '1805');
            json.Lans = socks5Dom.value.getData();
            updateSocks5(json).then(() => {
                state.show = false;
                ElMessage.success(t('common.opered'));
                emit('change')
            }).catch((err) => {
                console.log(err);
                ElMessage.error(t('common.operFail'));
            });
        }

        return {
           state, ruleFormRef,socks5Dom,  handleSave
        }
    }
}
</script>
<style lang="stylus" scoped>
.el-switch.is-disabled{opacity :1;}
.upgrade-wrap{
    border:1px solid var(--table-border-color);
    margin-bottom:2rem
    padding:1rem;
}
</style>