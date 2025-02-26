<template>
    <div :style="{ height: `${state.height}px` }">
        <el-card shadow="never">
            <template #header>{{ $t('server.asyncText') }}</template>
            <div>
                <el-checkbox v-model="state.checkAll" :indeterminate="state.isIndeterminate"
                    @change="handleCheckAllChange">{{
                        $t('server.asyncCheckAll') }}</el-checkbox>
                <el-checkbox-group v-model="state.checkeds" @change="handleCheckedsChange">
                    <el-row>
                        <template v-for="name in state.names">
                            <el-col :span="8">
                                <el-checkbox :key="name.name" :label="name.label" :value="name.name">{{ name.label
                                    }}</el-checkbox>
                            </el-col>
                        </template>
                    </el-row>
                </el-checkbox-group>
            </div>
            <template #footer>
                <div class="t-c">
                    <el-button type="success" @click="handleSync">{{ $t('common.confirm') }}</el-button>
                </div>
            </template>
        </el-card>
    </div>
</template>
<script>
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, onMounted, reactive } from 'vue'
import { getSyncNames, setSync } from '@/apis/sync';
import { useI18n } from 'vue-i18n';
export default {
    setup(props) {
        const { t } = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            names: [],
            checkAll: false,
            isIndeterminate: false,
            checkeds: [],
            height: computed(() => globalData.value.height - 90),
        });

        const handleCheckAllChange = (val) => {
            state.checkeds = val ? state.names.map(c => c.name) : [];
            state.isIndeterminate = false
        }
        const handleCheckedsChange = (value) => {
            const checkedCount = value.length
            state.checkAll = checkedCount === state.names.length
            state.isIndeterminate = checkedCount > 0 && checkedCount < state.names.length;
        }

        const labels = {
            'SignInServer': t('server.asyncSignInServer'),
            'SignInSecretKey': t('server.asyncSignInSecretKey'),
            'GroupSecretKey': t('server.asyncGroupSecretKey'),
            'RelaySecretKey': t('server.asyncRelaySecretKey'),
            'SForwardSecretKey': t('server.asyncSForwardSecretKey'),
            'UpdaterSecretKey': t('server.asyncUpdaterSecretKey'),
            'TunnelTransports': t('server.asyncTunnelTransports')
        }
        onMounted(() => {
            getSyncNames().then(res => {
                state.names = res.map(c => {
                    return { name: c, label: labels[c] }
                });
            });
        });
        const handleSync = () => {
            if (state.checkeds.length == 0) {
                ElMessage.error(t('server.asyncSelect'));
                return;
            }
            setSync(state.checkeds).then(res => {
                ElMessage.success(t('common.oper'));
            });
        }

        return { state, handleCheckAllChange, handleCheckedsChange, handleSync }
    }
}
</script>
<style lang="stylus" scoped>

</style>