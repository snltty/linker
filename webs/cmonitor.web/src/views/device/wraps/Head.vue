<template>
    <div class="head-wrap">
        <div class="inner flex">
            <a href="/" class="logo">
                <img src="@/assets/logo.png" alt="">
            </a>
            <ul class="menu flex">
                <template v-for="(item,index) in footMenuModules" :key="index">
                    <li>
                        <component :is="item"></component>
                    </li>
                </template>
            </ul>
            <div class="options">
                <template v-for="(item,index) in footOptionTopModules" :key="index">
                    <component :is="item"></component>
                </template>
                <template v-for="(item,index) in footOptionBottomModules" :key="index">
                    <component :is="item"></component>
                </template>
            </div>
            <div class="flex-1"></div>
            <a class="username" href="javascript:;" @click="handleUpdate">{{username}}</a>
        </div>
    </div>
</template>

<script>
import { injectGlobalData } from '@/views/provide';
import { ElMessageBox } from 'element-plus';
import { computed } from 'vue';
export default {
    setup() {

        const globalData = injectGlobalData();
        const plugins = computed(()=>globalData.value.config.Common.Plugins||[]);

        const footMenuFiles = require.context('../plugins/', true, /FootMenu\.vue/);
        const _footMenuModules = footMenuFiles.keys().map(c => footMenuFiles(c).default).sort((a, b) => a.sort - b.sort);
        const footMenuModules = computed(()=>_footMenuModules.filter(c=>plugins.value.length == 0 || plugins.value.indexOf(c.pluginName)>=0));


        const footOptionTopFiles = require.context('../plugins/', true, /FootOptionTop\.vue/);
        const _footOptionTopModules = footOptionTopFiles.keys().map(c => footOptionTopFiles(c).default);
        const footOptionTopModules = computed(()=>_footOptionTopModules.filter(c=>plugins.value.length == 0 || plugins.value.indexOf(c.pluginName)>=0));

        const footOptionBottomFiles = require.context('../plugins/', true, /FootOptionBottom\.vue/);
        const _footOptionBottomModules = footOptionBottomFiles.keys().map(c => footOptionBottomFiles(c).default);
        const footOptionBottomModules = computed(()=>_footOptionBottomModules.filter(c=>plugins.value.length == 0 || plugins.value.indexOf(c.pluginName)>=0));

        const username = computed(() => globalData.value.username);
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



        return { footMenuModules, footOptionTopModules, footOptionBottomModules, username, handleUpdate }
    }
}
</script>

<style lang="stylus">
.head-wrap {
    .el-icon {
        margin-top: -2px !important;
    }
}
</style> 
<style lang="stylus" scoped>
.head-wrap {
    --foot-menu-dropdown-color: #333;
}

.head-wrap {
    position: relative;
    z-index: 9;
    padding: 1rem 1rem 0 1rem;

    .inner {
        border-radius: 4px;
        background-color: rgba(255, 255, 255, 1);
        height: 4rem;
        line-height: 4rem;
        overflow: hidden;

        .logo {
            padding-top: 0.4rem;
            padding-left: 0.6rem;
            height: 4rem;
            box-sizing: border-box;

            img {
                height: 3.2rem;
            }
        }

        .username {
            padding: 0 0.6rem;
            font-size: 1.4rem;
            color: #333;
            text-decoration: underline;
        }

        ul.menu {
            padding-left: 2rem;

            li {
                text-align: center;

                a {
                    padding: 1.2rem 1rem;
                    font-size: 1.6rem;
                    display: block;
                    color: #333;
                    line-height: 1;

                    &:hover {
                        background-color: rgba(0, 0, 0, 0.05);
                    }
                }
            }
        }

        .options {
            padding-left: 2rem;
        }
    }
}
</style>