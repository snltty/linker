<template>
    <div class="device-item" :style="data.style">
        <dl>
            <dt>
                <div class="bg"></div>
                <div class="value flex">
                    <span class="name" :class="{connected:data.Connected}">
                        <span class="machine-mame">{{data.MachineName}}</span>
                        <template v-for="(item,index) in titleLeftModules" :key="index">
                            <component :is="item" :data="data"></component>
                        </template>
                    </span>
                    <span class="flex-1"></span>
                    <template v-for="(item,index) in titleCenterModules" :key="index">
                        <component :is="item" :data="data"></component>
                    </template>
                    <span class="flex-1"></span>
                    <template v-for="(item,index) in titleRightModules" :key="index">
                        <component :is="item" :data="data"></component>
                    </template>
                </div>
            </dt>
            <dd class="img">
                <div class="inner">
                    <canvas v-if="data.Connected && data.Screen.width>0 && data.Screen.height>0" :width="data.Screen.width" :height="data.Screen.height" :id="`canvas-${data.MachineName}`" @dblclick="handleCanvasReset" @touchstart="handleCanvasTouchstart" @touchend="handleCanvasTouchend" @touchmove="handleCanvasTouchmove"></canvas>
                    <template v-for="(item,index) in screenModules" :key="index">
                        <component :is="item" :data="data"></component>
                    </template>
                    <div class="btns flex">
                        <div class="left">
                            <template v-for="(item,index) in btnLeftModules" :key="index">
                                <component :is="item" :data="data"></component>
                            </template>
                        </div>
                        <div class="flex-1"></div>
                        <div class="right">
                            <template v-for="(item,index) in btnRightModules" :key="index">
                                <component :is="item" :data="data"></component>
                            </template>
                        </div>
                    </div>
                </div>
            </dd>
            <dd class="options">
                <el-row>
                    <template v-for="(item,index) in optionModules" :key="index">
                        <component :is="item" :data="data"></component>
                    </template>
                </el-row>
            </dd>
        </dl>
    </div>
</template>

<script>
import { injectGlobalData } from '@/views/provide';
import { computed } from 'vue';

export default {
    props: {
        data: {
            type: Object,
            default: {}
        }
    },
    setup(props, { emit }) {
        const data = props.data;

        const globalData = injectGlobalData();
        const plugins = computed(()=>globalData.value.config.Common.Plugins||[]);

        const titleLeftFiles = require.context('../plugins/', true, /TitleLeft\.vue/);
        const _titleLeftModules = titleLeftFiles.keys().map(c => titleLeftFiles(c).default);
        const titleLeftModules = computed(()=>_titleLeftModules.filter(c=>plugins.value.length == 0 || plugins.value.indexOf(c.pluginName)>=0));

        const titleCenterFiles = require.context('../plugins/', true, /TitleCenter\.vue/);
        const _titleCenterModules = titleCenterFiles.keys().map(c => titleCenterFiles(c).default);
        const titleCenterModules = computed(()=>_titleCenterModules.filter(c=>plugins.value.length == 0 || plugins.value.indexOf(c.pluginName)>=0));

        const titleRightFiles = require.context('../plugins/', true, /TitleRight\.vue/);
        const _titleRightModules = titleRightFiles.keys().map(c => titleRightFiles(c).default);
        const titleRightModules = computed(()=>_titleRightModules.filter(c=>plugins.value.length == 0 || plugins.value.indexOf(c.pluginName)>=0));

        const screenFiles = require.context('../plugins/', true, /Screen\.vue/);
        const _screenModules = screenFiles.keys().map(c => screenFiles(c).default);
        const screenModules = computed(()=>_screenModules.filter(c=>plugins.value.length == 0 || plugins.value.indexOf(c.pluginName)>=0));

        const btnLeftFiles = require.context('../plugins/', true, /BtnLeft\.vue/);
        const _btnLeftModules = btnLeftFiles.keys().map(c => btnLeftFiles(c).default);
        const btnLeftModules = computed(()=>_btnLeftModules.filter(c=>plugins.value.length == 0 || plugins.value.indexOf(c.pluginName)>=0));

        const btnRightFiles = require.context('../plugins/', true, /BtnRight\.vue/);
        const _btnRightModules = btnRightFiles.keys().map(c => btnRightFiles(c).default);
        const btnRightModules = computed(()=>_btnRightModules.filter(c=>plugins.value.length == 0 || plugins.value.indexOf(c.pluginName)>=0));

        const optionFiles = require.context('../plugins/', true, /\/Option\.vue/);
        const _optionModules = optionFiles.keys().map(c => optionFiles(c).default).sort((a, b) => (a.sort || 0) - (b.sort || 0));
        const optionModules = computed(()=>_optionModules.filter(c=>plugins.value.length == 0 || plugins.value.indexOf(c.pluginName)>=0));

        const handleCanvasTouchstart = (event) => {
            if (data.Screen.touchstart) {
                data.Screen.touchstart(event);
            }
        }
        const handleCanvasTouchend = (event) => {
            if (data.Screen.touchend) {
                data.Screen.touchend(event);
            }
        }
        const handleCanvasTouchmove = (event) => {
            if (data.Screen.touchmove) {
                data.Screen.touchmove(event);
            }
        }
        const handleCanvasReset = () => {
            if (data.Screen.reset) {
                data.Screen.reset();
            }
        }

        return {
            data, titleLeftModules, titleCenterModules, titleRightModules, screenModules, btnLeftModules, btnRightModules, optionModules
            , handleCanvasTouchstart, handleCanvasTouchend, handleCanvasTouchmove, handleCanvasReset
        }
    }
}
</script>

<style lang="stylus" scoped>
// @media (min-width: 768px) {
// .device-item {
// width: 39rem !important;
// background-color: rgba(255, 255, 255, 1) !important;
// margin: 0 0 0.6rem 0 !important;
// }
// }
.device-item {
    // border: 1px solid #ddd;
    font-size: 1.6rem;
    // background-color: #fff;
    box-shadow: 0 0 4px rgba(0, 0, 0, 0.05);
    width: 100%;
    margin: 0 auto 0.6rem auto;
    position: relative;
    transition: 0.3s;
    background-color: rgba(255, 255, 255, 0.5);
    border-radius: 4px;

    &:after {
        content: '';
        position: absolute;
        left: 0;
        top: 0;
        right: 0;
        bottom: 0;
        z-index: -1;
        border: 1px solid rgba(251, 241, 242, 0.4);
        // background-color: rgba(255, 255, 255, 0.5);
        border-radius: 4px;
        // backdrop-filter: blur(2px);
    }

    dt {
        padding: 0.6rem 0.6rem 0 0.6rem;
        border-radius: 4px;
        position: relative;

        span.name {
            line-height: 2rem;

            &.connected {
                color: var(--el-color-primary);
                font-weight: bold;
            }
        }
    }

    dd.img {
        padding: 0.6rem;
        position: relative;
        font-size: 0;
        box-sizing: border-box;

        &:before {
            content: '';
            display: inline-block;
            padding-bottom: 56.25%;
            width: 0.1px;
            vertical-align: middle;
        }

        .inner {
            position: absolute;
            left: 0.6rem;
            top: 0.6rem;
            right: 0.6rem;
            bottom: 0.6rem;
            overflow: hidden;
            background-color: rgba(0, 0, 0, 0.1);
            border-radius: 4px;
            border: 1px solid rgba(255, 255, 255, 0.2);
            box-sizing: border-box;

            canvas {
                width: 100%;
                position: absolute;
                height: 100%;
                border-radius: 4px;
            }

            .btns {
                pointer-events: none;
                position: absolute;
                left: 0;
                right: 0;
                top: 10%;

                .left {
                    padding-left: 0.6rem;
                    pointer-events: all;
                }

                .right {
                    padding-right: 0.6rem;
                    pointer-events: all;
                }

                .left, .right {
                    a {
                        width: 2.4rem;
                        height: 2.4rem;
                        text-align: center;
                        line-height: 2.8rem;
                        margin-bottom: 0.6rem;
                        display: block;
                        font-size: 2rem;
                        border-radius: 50%;
                        border: 1px solid #3e5a6e;
                        box-shadow: 0 0 4px rgba(255, 255, 255, 0.1);
                        background-color: rgba(255, 255, 255, 0.5);
                        color: #3e5a6e;
                        transition: 0.3s;

                        &:hover {
                            box-shadow: 0 0 4px 2px rgba(255, 255, 255, 0.5);
                        }
                    }
                }
            }
        }
    }

    dd.options {
        padding: 0 0.6rem 0.6rem 0.6rem;
        border-radius: 4px;
        text-align: center;

        .el-col {
            text-align: right;

            .el-switch {
                --el-switch-off-color: #ccc;
                --el-switch-on-color: #69b56c;
            }
        }
    }
}
</style>