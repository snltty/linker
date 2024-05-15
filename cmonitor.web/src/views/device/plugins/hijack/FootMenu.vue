<template>
    <a href="javascript:;">
        <el-dropdown>
            <span class="el-dropdown-link">
                <el-icon>
                    <Umbrella />
                </el-icon>
                <span>网络</span>
                <el-icon class="el-icon--right">
                    <arrow-down />
                </el-icon>
            </span>
            <template #dropdown>
                <el-dropdown-menu>
                    <el-dropdown-item @click="handleProcess">程序配置</el-dropdown-item>
                    <el-dropdown-item divided @click="handleRule">分组配置</el-dropdown-item>
                </el-dropdown-menu>
            </template>
        </el-dropdown>
    </a>
</template>

<script>
import { injectPluginState } from '@/views/device/provide'
export default {
    pluginName:'hijack',
    sort: 2,
    setup() {

        const pluginState = injectPluginState();
        const handleProcess = () => {
            pluginState.value.hijack.showProcessSetting = true;
        }
        const handleRule = () => {
            pluginState.value.hijack.showRuleSetting = true;
        }
        return {
            handleProcess, handleRule
        }
    }
}
</script>

<style lang="stylus" scoped>
.el-icon {
    font-size: 1.6rem;
    vertical-align: middle;
    margin-top: -1px;
}

.el-dropdown {
    color: var(--foot-menu-dropdown-color);
    font-size: 1.6rem;

    .el-dropdown-link {
    }
}
</style>