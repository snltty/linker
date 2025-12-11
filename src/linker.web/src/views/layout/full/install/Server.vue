<template>
    <div>
        <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="12rem">
            <el-form-item label="" label-width="0">
                <el-row>
                    <el-col :span="12">
                        <el-form-item label="服务端口" prop="servicePort">
                            <el-input v-trim v-model="state.form.servicePort" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-col :span="12">
                            <el-form-item label="匿名登录" prop="anonymous">
                                <el-checkbox v-model="state.form.anonymous">匿名登录</el-checkbox>
                            </el-form-item>
                        </el-col>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="" label-width="0">
                <el-row>
                    <el-col :span="12">
                        <el-form-item label="管理密钥" prop="superKey">
                            <el-input v-trim v-model="state.form.superKey" type="password" show-password maxlength="36" show-word-limit />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="管理密码" prop="superPassword">
                            <el-input v-trim v-model="state.form.superPassword" type="password" show-password maxlength="36" show-word-limit />
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
                servicePort:step.value.form.server.servicePort ||globalData.value.config.Server.ServicePort,
                anonymous:step.value.form.server.anonymous ||globalData.value.config.Server.SignIn.Anonymous,
                superKey:step.value.form.server.superKey ||globalData.value.config.Server.SignIn.SuperKey,
                superPassword:step.value.form.server.superPassword ||globalData.value.config.Server.SignIn.SuperPassword,
                webPort:step.value.form.server.webPort ||globalData.value.config.Server.SForward.WebPort,
                TunnelPorts:step.value.form.server.TunnelPorts ||globalData.value.config.Server.SForward.TunnelPorts,
            },
            rules: {
                superKey: [{ required: true, message: "必填", trigger: "blur" }],
                superPassword: [{ required: true, message: "必填", trigger: "blur" }],
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
                ]
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
                                    Anonymous: state.form.anonymous,
                                    SuperKey: state.form.superKey,
                                    SuperPassword: state.form.superPassword,

                                    SForward:{
                                        WebPort: +state.form.webPort,
                                        TunnelPorts:state.form.TunnelPorts
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
            if(step.value.json.Common.server == false || globalData.value.isPC == false){
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