<template>
    <div>
        <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="10rem">
            <el-form-item label="" label-width="0">
                <div class="t-c w-100">端口为0则不监听</div>
            </el-form-item>
            <el-form-item label="" label-width="0">
                <el-row>
                    <el-col :span="12">
                        <el-form-item label="服务端口" prop="servicePort">
                            <el-input v-model="state.form.servicePort" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="web穿透端口" prop="webPort">
                            <el-input v-model="state.form.webPort" />
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="" label-width="0">
                <el-row>
                    <el-col :span="12">
                        <el-form-item label="开放最小端口" prop="tunnelPort1">
                            <el-input v-model="state.form.tunnelPort1" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="开放最大端口" prop="tunnelPort2">
                            <el-input v-model="state.form.tunnelPort2" />
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="" label-width="0">
                <el-row>
                    <el-col :span="12">
                        <el-form-item label="穿透密钥" prop="sForwardSecretKey">
                            <el-input v-model="state.form.sForwardSecretKey" maxlength="36" show-word-limit />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="中继密钥" prop="relaySecretKey">
                            <el-input v-model="state.form.relaySecretKey" maxlength="36" show-word-limit />
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="" label-width="0">
                <el-row>
                    <el-col :span="12">
                        <el-form-item label="信标密钥" prop="signinSecretKey">
                            <el-input v-model="state.form.signinSecretKey" maxlength="36" show-word-limit />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="更新密钥" prop="updaterSecretKey">
                            <el-input v-model="state.form.updaterSecretKey" maxlength="36" show-word-limit />
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            
        </el-form>
    </div>
</template>
<script>
import { injectGlobalData } from '@/provide';
import { reactive,  ref, inject, onMounted } from 'vue';
export default {
    setup(props) {

        const step = inject('step');
        const globalData = injectGlobalData();
        const state = reactive({
            show: false,
            form: {
                signinSecretKey:step.value.form.server.signinSecretKey || globalData.value.config.Server.SignIn.SecretKey,
                relaySecretKey:step.value.form.server.relaySecretKey || globalData.value.config.Server.Relay.SecretKey,
                sForwardSecretKey:step.value.form.server.sForwardSecretKey ||globalData.value.config.Server.SForward.SecretKey,
                servicePort:step.value.form.server.servicePort ||globalData.value.config.Server.ServicePort,
                webPort:step.value.form.server.webPort ||globalData.value.config.Server.SForward.WebPort,
                tunnelPort1:step.value.form.server.tunnelPort1 ||globalData.value.config.Server.SForward.TunnelPortRange[0],
                tunnelPort2:step.value.form.server.tunnelPort2 ||globalData.value.config.Server.SForward.TunnelPortRange[1],

                updaterSecretKey:step.value.form.server.updaterSecretKey ||globalData.value.config.Server.Updater.SecretKey,
            },
            rules: {
                relaySecretKey: [{ required: true, message: "必填", trigger: "blur" }],
                sForwardSecretKey: [{ required: true, message: "必填", trigger: "blur" }],
                servicePort: [
                    { required: true, message: "必填", trigger: "blur" },
                    {
                        type: "number",
                        min: 0,
                        max: 65535,
                        message: "数字 0-65535",
                        trigger: "blur",
                        transform(value) {
                            return Number(value);
                        },
                    },
                ],
                webPort: [
                    { required: true, message: "必填", trigger: "blur" },
                    {
                        type: "number",
                        min: 0,
                        max: 65535,
                        message: "数字 0-65535",
                        trigger: "blur",
                        transform(value) {
                            return Number(value);
                        },
                    },
                ],
                tunnelPort1: [
                    { required: true, message: "必填", trigger: "blur" },
                    {
                        type: "number",
                        min: 0,
                        max: 65535,
                        message: "数字 0-65535",
                        trigger: "blur",
                        transform(value) {
                            return Number(value);
                        },
                    },
                ],
                tunnelPort2: [
                    { required: true, message: "必填", trigger: "blur" },
                    {
                        type: "number",
                        min: 0,
                        max: 65535,
                        message: "数字 0-65535",
                        trigger: "blur",
                        transform(value) {
                            return Number(value);
                        },
                    },
                ],
            },
        });

        const formDom = ref(null);
        const handleValidate = () => {
            return new Promise((resolve, reject) => {
                formDom.value.validate((valid) => {
                    if (valid == false) {
                        reject();
                    }else{
                        resolve({
                            json:{
                                Server:{
                                    ServicePort: +state.form.servicePort,
                                    Relay:{
                                        SecretKey: state.form.relaySecretKey
                                    },
                                    SForward:{
                                        SecretKey: state.form.sForwardSecretKey,
                                        WebPort: +state.form.webPort,
                                        TunnelPortRange: [+state.form.tunnelPort1, +state.form.tunnelPort2]
                                    },
                                    Updater:{
                                        SecretKey: state.form.updaterSecretKey
                                    },
                                    SignIn:{
                                        SecretKey: state.form.signinSecretKey
                                    }
                                }  
                            },
                            form:{server:JSON.parse(JSON.stringify(state.form))}
                        });
                    }
                    
                });
            })
        }

        onMounted(()=>{
            if(step.value.json.Common.server == false){
                step.value.step+=step.value.increment;
            }
        })

        return { state, handleValidate, formDom };
    }
}
</script>
<style lang="stylus" scoped>
.body{
    padding:1rem 0 0 0;
}
.footer{
    padding: 1rem 0;
}
.el-card+.el-card{margin-top:1rem;}
</style>