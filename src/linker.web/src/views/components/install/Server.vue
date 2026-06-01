<template>
    <div>
        <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="14rem">
            <el-form-item label="" label-width="0">
                <el-row>
                    <el-col :span="12">
                        <el-form-item :label="$t('install.servicePort')" prop="servicePort">
                            <el-input v-trim v-model="state.form.servicePort" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-col :span="12">
                            <el-form-item :label="$t('install.anonymous')" prop="anonymous">
                                <el-checkbox v-model="state.form.anonymous">{{$t('install.anonymous')}}</el-checkbox>
                            </el-form-item>
                        </el-col>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="" label-width="0">
                <el-row>
                    <el-col :span="12">
                        <el-form-item :label="$t('install.superKey')" prop="superKey">
                            <el-input v-trim v-model="state.form.superKey" type="password" show-password maxlength="36" show-word-limit />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item :label="$t('install.superPwd')" prop="superPassword">
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
import { useI18n } from 'vue-i18n';
export default {
    setup(props) {

        const {t} = useI18n();
        const step = inject('step');
        const globalData = injectGlobalData();
        const state = reactive({
            show: false,
            form: {
                servicePort:step.value.form.server.servicePort ||globalData.value.config.Server.ServicePort,
                anonymous:step.value.form.server.anonymous ||globalData.value.config.Server.SignIn.Anonymous,
                superKey:step.value.form.server.superKey ||globalData.value.config.Server.SignIn.SuperKey,
                superPassword:step.value.form.server.superPassword ||globalData.value.config.Server.SignIn.SuperPassword,
                webPort:step.value.form.server.webPort ||globalData.value.config.Server.Reverse.WebPort,
                TunnelPorts:step.value.form.server.TunnelPorts ||globalData.value.config.Server.Reverse.TunnelPorts,
            },
            rules: {
                superKey: [{ required: true, message: t('install.required'), trigger: "blur" }],
                superPassword: [{ required: true, message: t('install.required'), trigger: "blur" }],
                servicePort: [
                    { required: true, message: t('install.required'), trigger: "blur" },
                    {
                        type: "number",
                        min: 0,
                        max: 65535,
                        message: "0-65535",
                        trigger: "blur",
                        transform(value) {
                            return Number(value);
                        },
                    },
                ],
                webPort: [
                    { required: true, message: t('install.required'), trigger: "blur" },
                    {
                        type: "number",
                        min: 0,
                        max: 65535,
                        message: "0-65535",
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

                                    Reverse:{
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