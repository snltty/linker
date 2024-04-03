<template>
    <div class="foot-options-wrap flex">
        <el-button size="small" plain dark @click="handleRefresh">
            <el-icon>
                <Refresh />
            </el-icon>
        </el-button>
        <span class="flex-1"></span>
        <div class="options-btn">
            <p>
                <template v-for="(item,index) in footOptionTopModules" :key="index">
                    <component :is="item"></component>
                </template>
            </p>
            <p>
                <template v-for="(item,index) in footOptionBottomModules" :key="index">
                    <component :is="item"></component>
                </template>
            </p>
        </div>
        <span class="flex-1"></span>
        <el-button size="small" plain dark @click="handleUpdate">{{username}}</el-button>
    </div>
</template>

<script>
import { computed } from 'vue';
import { ElMessageBox } from 'element-plus';
import { injectGlobalData } from '@/views/provide';
export default {
    setup() {

        const footOptionTopFiles = require.context('../plugins/', true, /FootOptionTop\.vue/);
        const footOptionTopModules = footOptionTopFiles.keys().map(c => footOptionTopFiles(c).default);

        const footOptionBottomFiles = require.context('../plugins/', true, /FootOptionBottom\.vue/);
        const footOptionBottomModules = footOptionBottomFiles.keys().map(c => footOptionBottomFiles(c).default);

        const globalData = injectGlobalData();
        const username = computed(() => globalData.value.username);
        const handleRefresh = () => {
            window.location.reload();
        }
        const handleUpdate = () => {
            ElMessageBox.confirm('是否确定重选角色？', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                globalData.value.username = '';
                localStorage.setItem('username', '');
            }).catch(() => { });
        }

        return {
            footOptionTopModules, footOptionBottomModules, username, handleRefresh, handleUpdate
        }
    }
}
</script>

<style lang="stylus" scoped>
.foot-options-wrap {
    text-align: center;
    padding: 0.6rem;

    .el-button {
        background-color: rgba(255, 255, 255, 0.05);
        border-color: rgba(18, 63, 76, 0.8);
        color: #f5f5f5;
    }

    .el-dropdown {
        margin: 0 0.6rem;
    }

    .options-btn {
        .el-button+.el-button {
            margin-left: 0.4rem;
        }

        p {
            padding-top: 0.4rem;

            &:nth-child(1) {
                padding: 0;
            }
        }
    }
}
</style>