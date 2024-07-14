<template>
    <div>
        <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="8rem">
            <el-form-item label="" label-width="0">
                <div class="t-c w-100">
                    <p>端口为0则不监听</p>
                    <p>相同分组名之间的客户端相互可见</p>
                </div>
            </el-form-item>
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
                name: globalData.value.config.Client.Name,
                groupid: globalData.value.config.Client.GroupId,
                api: globalData.value.config.Client.CApi.ApiPort,
                web: globalData.value.config.Client.CApi.WebPort,
                password: globalData.value.config.Client.CApi.ApiPassword
            },
            rules: {
                name: [{ required: true, message: "必填", trigger: "blur" }],
                groupid: [{ required: true, message: "必填", trigger: "blur" }],
                password: [{ required: true, message: "必填", trigger: "blur" }],
                api: [
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
                            Client:{
                                name: state.form.name,
                                groupid: state.form.groupid,
                                api: +state.form.api,
                                web: +state.form.web,
                                password: state.form.password
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
</style>