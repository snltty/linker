<template>
    <el-form label-width="8rem" ref="formDom" :model="state.form" :rules="state.rules">
        <el-form-item label="" label-width="0">
            <el-row>
                <el-col :xs="24" :sm="8" :md="8" :lg="8" :xl="8">
                    <el-form-item :label="$t('logger.count')" prop="Size">
                        <el-input v-trim size="default" v-model="state.form.Size"></el-input>
                    </el-form-item>
                </el-col>
                <el-col :xs="24" :sm="8" :md="8" :lg="8" :xl="8">
                    <el-form-item :label="$t('logger.level')" prop="LoggerType">
                        <el-select v-model="state.form.LoggerType">
                            <el-option :value="0" label="debug"></el-option>
                            <el-option :value="1" label="info"></el-option>
                            <el-option :value="2" label="warning"></el-option>
                            <el-option :value="3" label="error"></el-option>
                            <el-option :value="4" label="fatal"></el-option>
                        </el-select>
                    </el-form-item>
                </el-col>
            </el-row>
        </el-form-item>
        <el-form-item label-width="0">
            <div class="t-c w-100">
                <el-button type="primary" :loading="state.loading" @click="submit">{{$t('common.confirm')}}</el-button>
            </div>
        </el-form-item>
    </el-form>
</template>

<script>
import { ref, reactive } from "@vue/reactivity";
import { getLoggerConfig, setLoggerConfig } from "@/apis/logger";
import { onMounted } from "@vue/runtime-core";
import { ElMessage } from 'element-plus';
export default {
    setup() {
        const formDom = ref(null);
        const state = reactive({
            loading: false,
            configInfo: {},
            form: {
                Size: 0,
                LoggerType: -1,
            },
            rules: {
                Size: [
                    { required: true, message: "必填", trigger: "blur" },
                    {
                        type: "number",
                        min: 1,
                        max: 10000,
                        message: "数字 1-10000",
                        trigger: "blur",
                        transform(value) {
                            return Number(value);
                        },
                    },
                ],
            },
        });

        const loadConfig = () => {
            getLoggerConfig()
                .then((json) => {
                    state.configInfo = json;
                    state.form.Size = json.Size;
                    state.form.LoggerType = json.LoggerType;
                })
                .catch((e) => { });
        };
        const getJson = () => {
            let _json = JSON.parse(JSON.stringify(state.configInfo));
            _json.Size = +state.form.Size;
            _json.LoggerType = +state.form.LoggerType;
            return _json;
        };
        const submit = () => {
            return new Promise((resolve, reject) => {
                formDom.value.validate((valid) => {
                    if (valid == false) {
                        reject();
                        return false;
                    }
                    state.loading = true;
                    const _json = getJson();
                    setLoggerConfig(_json).then((res) => {
                        state.loading = false;
                        resolve();
                        if (res) {
                            ElMessage.success('操作成功!');
                        } else {
                            ElMessage.error('操作失败!');
                        }
                    }).catch((err) => {
                        console.log(err);
                        state.loading = false;
                        resolve();
                    })
                });
            });
        };

        onMounted(() => {
            loadConfig();
        });

        return {
            state,
            formDom,
            submit,
        };
    },
};
</script>

<style lang="stylus" scoped>
.el-row {
    width: 100%;
}

.el-form-item {
    width: 100%;
}

.el-form-item:last-child {
    margin-bottom: 0;
}

@media screen and (max-width: 768px) {
    .el-col {
        margin-top: 0.6rem;
    }
}
</style>