<template>
    <div>
        <el-dialog v-model="state.show" title="初始化配置" width="500" top="2vh">
            <div>
                <div class="head t-c">
                    <el-checkbox v-model="state.form.client" label="客户端" />
                    <el-checkbox v-model="state.form.server" label="服务端" />
                </div>
                <div class="body">
                    <el-card shadow="never" v-if="state.form.client">
                        <template #header>
                        <div class="card-header"><span>客户端</span></div>
                        </template>
                        <div>
                            <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="8rem">
                                <el-form-item label="" label-width="0">
                                    <el-row>
                                        <el-col :span="12">
                                            <el-form-item label="机器名" prop="name">
                                                <el-input v-model="state.form.name" maxlength="12" show-word-limit />
                                            </el-form-item>
                                        </el-col>
                                        <el-col :span="12">
                                            <el-form-item label="分组名" prop="groupid">
                                                <el-input v-model="state.form.groupid" maxlength="36" show-word-limit />
                                            </el-form-item>
                                        </el-col>
                                    </el-row>
                                </el-form-item>
                                <el-form-item label="" label-width="0">
                                    <el-row>
                                        <el-col :span="12">
                                            <el-form-item label="接口端口" prop="api">
                                                <el-input v-model="state.form.api" />
                                            </el-form-item>
                                        </el-col>
                                        <el-col :span="12">
                                            <el-form-item label="网页端口" prop="web">
                                                <el-input v-model="state.form.web" />
                                            </el-form-item>
                                        </el-col>
                                    </el-row>
                                </el-form-item>
                                <el-form-item label="" label-width="0">
                                    <el-form-item label="接口密码" prop="password">
                                        <el-input type="password" v-model="state.form.password" show-password maxlength="36"
                                            show-word-limit />
                                    </el-form-item>
                                </el-form-item>
                            </el-form>
                        </div>
                    </el-card>
                    <el-card shadow="never" v-if="state.form.server">
                        <template #header>
                        <div class="card-header"><span>服务端</span></div>
                        </template>
                        <div>
                            <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="10rem">
                                <el-form-item label="" label-width="0">
                                    <el-row>
                                        <el-col :span="12">
                                            <el-form-item label="服务端口" prop="servicePort">
                                                <el-input v-model="state.form.servicePort" />
                                            </el-form-item>
                                        </el-col>
                                        <el-col :span="12">
                                            <el-form-item label="web穿透端口" prop="sforwardPort">
                                                <el-input v-model="state.form.sforwardPort" />
                                            </el-form-item>
                                        </el-col>
                                    </el-row>
                                </el-form-item>
                                <el-form-item label="" label-width="0">
                                    <el-row>
                                        <el-col :span="12">
                                            <el-form-item label="开放最小端口" prop="sforwardPort1">
                                                <el-input v-model="state.form.sforwardPort1" />
                                            </el-form-item>
                                        </el-col>
                                        <el-col :span="12">
                                            <el-form-item label="开放最大端口" prop="sforwardPort2">
                                                <el-input v-model="state.form.sforwardPort2" />
                                            </el-form-item>
                                        </el-col>
                                    </el-row>
                                </el-form-item>
                                <el-form-item label="" label-width="0">
                                    <el-row>
                                        <el-col :span="12">
                                            <el-form-item label="穿透密钥" prop="sforwardKey">
                                                <el-input v-model="state.form.sforwardKey" maxlength="36" show-word-limit />
                                            </el-form-item>
                                        </el-col>
                                        <el-col :span="12">
                                            <el-form-item label="中继密钥" prop="relayKey">
                                                <el-input v-model="state.form.relayKey" maxlength="36" show-word-limit />
                                            </el-form-item>
                                        </el-col>
                                    </el-row>
                                </el-form-item>
                            </el-form>
                        </div>
                    </el-card>
                </div>
            </div>
            <template #footer>
                <div class="dialog-footer t-c">
                    <el-button @click="state.show = false" :loading="state.loading">取消</el-button>
                    <el-button type="primary" @click="handleSave" :loading="state.loading">确定保存</el-button>
                </div>
            </template>
        </el-dialog>
    </div>
</template>
<script>
import { injectGlobalData } from '@/provide';
import { install } from '@/apis/signin';
import { reactive, computed, watch, ref } from 'vue';
import { ElMessage } from 'element-plus';
export default {
    setup(props) {

        const globalData = injectGlobalData();
        const state = reactive({
            show: false,
            form: {
                client: true,
                server: true,
                restart: false,
                name: '',
                groupid: '',
                api: 0,
                web: 0,
                password: '',

                relayKey:'',
                sforwardKey:'',
                servicePort:1802,
                sforwardPort:80,
                sforwardPort1:10000,
                sforwardPort2:60000,
            },
            rules: {
                name: [{ required: true, message: "必填", trigger: "blur" }],
                groupid: [{ required: true, message: "必填", trigger: "blur" }],
                password: [{ required: true, message: "必填", trigger: "blur" }],
                api: [
                    { required: true, message: "必填", trigger: "blur" },
                    {
                        type: "number",
                        min: 1,
                        max: 65535,
                        message: "数字 1-65535",
                        trigger: "blur",
                        transform(value) {
                            return Number(value);
                        },
                    },
                ],
                web: [
                    { required: true, message: "必填", trigger: "blur" },
                    {
                        type: "number",
                        min: 1,
                        max: 65535,
                        message: "数字 1-65535",
                        trigger: "blur",
                        transform(value) {
                            return Number(value);
                        },
                    },
                ],
            },
            loading: false,
        });

        watch(() => globalData.value.configed, (val) => {
            if (val) {
                state.show = globalData.value.connected && globalData.value.configed && !globalData.value.config.Client.GroupId;
                state.form.name = globalData.value.config.Client.Name;
                state.form.groupid = globalData.value.config.Client.GroupId;
                state.form.api = globalData.value.config.Client.CApi.ApiPort;
                state.form.web = globalData.value.config.Client.CApi.WebPort;
                state.form.password = globalData.value.config.Client.CApi.ApiPassword;
            }
        })

        const formDom = ref(null);
        const handleSave = () => {
            formDom.value.validate((valid) => {
                if (valid == false) {
                    return false;
                }
                state.loading = true;
                install(state.form).then(() => {
                    state.loading = false;
                    ElMessage.success('已操作!');
                }).catch(() => {
                    state.loading = false;
                })
            });
        }

        return { state, handleSave, formDom };
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