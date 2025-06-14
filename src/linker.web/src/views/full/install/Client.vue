<template>
    <div>
        <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="8rem">
            <el-form-item label="" label-width="0">
                <el-row class="w-100">
                    <el-col :sm="12" :xs="24">
                        <el-form-item label="机器名" prop="name">
                            <el-input v-model="state.form.name" maxlength="32" show-word-limit />
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="" label-width="0" v-if="globalData.isPc">
                <el-row class="w-100">
                    <el-col :sm="12" :xs="24" v-if="globalData.isPc">
                        <el-form-item label="管理端口" prop="web">
                            <el-input  v-model="state.form.web" />
                        </el-form-item>
                    </el-col>
                    <el-col :sm="12" :xs="24">
                        <el-form-item label="管理密码" prop="password">
                            <el-input  type="password" v-model="state.form.password" show-password maxlength="36" show-word-limit/>
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="" label-width="0">
                <el-row class="w-100">
                    <el-col :sm="12" :xs="24">
                        <el-form-item label="分组名" prop="groupid">
                            <el-input v-model="state.form.groupid" maxlength="36" show-word-limit />
                        </el-form-item>
                    </el-col>
                    <el-col :sm="12" :xs="24">
                        <el-form-item label="分组密码" prop="groupPassword">
                            <el-input v-model="state.form.groupPassword" type="password" show-password maxlength="36" show-word-limit />
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="" label-width="0">
                <el-row class="w-100">   
                    <el-col :span="24">
                        <el-form-item label-width="8rem" prop="hasServer">
                            <el-checkbox v-model="state.form.hasServer" label="我有服务器(自建服务器)" size="large" />
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>

            <el-form-item label="" label-width="0" v-if="state.form.hasServer">
                <el-row class="w-100">
                    <el-col :sm="12" :xs="24">
                        <el-form-item label="信标服务" prop="server">
                            <el-input v-model="state.form.server"/>
                        </el-form-item>
                    </el-col>
                    <el-col :sm="12" :xs="24">
                        <el-form-item label="信标密钥" prop="serverSecretKey">
                            <el-input v-model="state.form.serverSecretKey" maxlength="36" show-word-limit />
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            
            <el-form-item label="" label-width="0" v-if="state.form.hasServer">
                <el-row class="w-100">
                    <el-col :sm="12" :xs="24">
                        <el-form-item label="穿透密钥" prop="sForwardSecretKey">
                            <el-input v-model="state.form.sForwardSecretKey" maxlength="36" show-word-limit />
                        </el-form-item>
                    </el-col>
                    <el-col :sm="12" :xs="24">
                        <el-form-item label="中继密钥" prop="relaySecretKey">
                            <el-input v-model="state.form.relaySecretKey" maxlength="36" show-word-limit />
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="" label-width="0" v-if="state.form.hasServer">
                <el-row class="w-100">
                    <el-col :sm="12" :xs="24">
                        <el-form-item label="更新密钥" prop="updaterSecretKey">
                            <el-input v-model="state.form.updaterSecretKey" maxlength="36" show-word-limit />
                        </el-form-item>
                    </el-col>
                    <el-col :sm="12" :xs="24" v-if="globalData.isPc">
                        <el-form-item label="占位">
                            <el-input disabled maxlength="36" show-word-limit />
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
        </el-form>
    </div>
</template>
<script>
import { injectGlobalData } from '@/provide';
import { reactive,  watch, ref, inject, onMounted } from 'vue';
export default {
    setup(props) {

        const step = inject('step');
        const globalData = injectGlobalData();

        const state = reactive({
            form: {
                name:step.value.form.client.name || globalData.value.config.Client.Name,
                groupid: step.value.form.client.groupid ||globalData.value.config.Client.Group.Id,
                groupPassword: step.value.form.client.groupPassword ||globalData.value.config.Client.Group.Password,
                web: step.value.form.client.web ||globalData.value.config.Client.CApi.WebPort,
                password:step.value.form.client.password || globalData.value.config.Client.CApi.ApiPassword,

                hasServer:step.value.form.client.hasServer ||false,
                server:step.value.form.client.server ||globalData.value.config.Client.Server.Host,
                serverSecretKey:step.value.form.client.serverSecretKey ||globalData.value.config.Client.ServerSecretKey,
                sForwardSecretKey:step.value.form.client.sForwardSecretKey ||globalData.value.config.Client.SForward.SecretKey,
                relaySecretKey:step.value.form.client.relaySecretKey ||(globalData.value.config.Client.Relay.Servers[0] || {SecretKey:'snltty'}).SecretKey,
                updaterSecretKey:step.value.form.client.updaterSecretKey ||globalData.value.config.Client.Updater.SecretKey,
            },
            rules: {
                name: [{ required: true, message: "必填", trigger: "blur" }],
                groupid: [{ required: true, message: "必填", trigger: "blur" }],
                groupPassword: [{ required: true, message: "必填", trigger: "blur" }],
                password: [{ required: true, message: "必填", trigger: "blur" }],
                web: [
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
            }
        });
        const formDom = ref(null);
        const handleValidate = () => {
            return new Promise((resolve, reject) => {
                formDom.value.validate((valid) => {
                    if (valid) {
                        resolve({
                            json:{
                                Client:{
                                    name: state.form.name,
                                    groupid: state.form.groupid,
                                    groupPassword: state.form.groupPassword,
                                    web: +state.form.web,
                                    password: state.form.password,

                                    hasServer: state.form.hasServer,
                                    server: state.form.server,
                                    serverSecretKey: state.form.serverSecretKey,
                                    sForwardSecretKey: state.form.sForwardSecretKey,
                                    relaySecretKey: state.form.relaySecretKey,
                                    updaterSecretKey: state.form.updaterSecretKey,
                                }
                            },
                            form:{
                                client:JSON.parse(JSON.stringify(state.form))
                            }
                        });
                    } else {
                        reject();
                    }
                })
            })
        }
        onMounted(()=>{
            if(step.value.json.Common.client == false){
                step.value.step +=step.value.increment;
            }
        })

        return { state,globalData, handleValidate, formDom };
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
.phone .el-dialog__body .el-col .el-form-item:last-child {margin-bottom:.6rem}
</style>