<template>
    <div class="device-item" :style="data.style">
        <dl>
            <dt>
                <div class="bg"></div>
                <div class="value flex">
                    <span class="name" :class="{connected:data.Connected}">
                        <span class="machine-mame">{{data.MachineName}}</span>
                        <i class="user-name" v-if="data.Screen.UserName"> - {{data.Screen.UserName}}</i>
                    </span>
                    <span class="flex-1"></span>
                    <template v-for="(item,index) in titleRightModules" :key="index">
                        <component :is="item" :data="data"></component>
                    </template>
                </div>
            </dt>
            <dd class="img">
                <div class="inner">
                    <canvas v-if="data.Connected" width="1920" height="1080" :id="`canvas-${data.MachineName}`"></canvas>
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
export default {
    props: {
        data: {
            type: Object,
            default: {}
        }
    },
    setup(props, { emit }) {
        const data = props.data;


        const titleRightFiles = require.context('../plugins/', true, /TitleRight\.vue/);
        const titleRightModules = titleRightFiles.keys().map(c => titleRightFiles(c).default);

        const screenFiles = require.context('../plugins/', true, /Screen\.vue/);
        const screenModules = screenFiles.keys().map(c => screenFiles(c).default);

        const btnLeftFiles = require.context('../plugins/', true, /BtnLeft\.vue/);
        const btnLeftModules = btnLeftFiles.keys().map(c => btnLeftFiles(c).default);

        const btnRightFiles = require.context('../plugins/', true, /BtnRight\.vue/);
        const btnRightModules = btnRightFiles.keys().map(c => btnRightFiles(c).default);

        const optionFiles = require.context('../plugins/', true, /\/Option\.vue/);
        const optionModules = optionFiles.keys().map(c => optionFiles(c).default).sort((a, b) => (a.sort || 0) - (b.sort || 0));

        return {
            data, titleRightModules, screenModules, btnLeftModules, btnRightModules, optionModules
        }
    }
}
</script>

<style lang="stylus" scoped>
.device-item {
    border: 1px solid #ddd;
    font-size: 1.6rem;
    background-color: #fff;
    box-shadow: 0 0 4px rgba(0, 0, 0, 0.05);
    border-radius: 4px;
    width: 98%;
    margin: 0 auto 1rem auto;
    position: relative;
    transition: 0.3s;

    &:after {
        content: '';
        position: absolute;
        left: 0;
        top: 0;
        right: 0;
        bottom: 0;
        z-index: -1;
    }

    dt {
        padding: 0.6rem 0.6rem 0 0.6rem;
        border-radius: 4px;
        position: relative;

        span.name {
            line-height: 2rem;

            &.connected {
                color: green;
                font-weight: bold;
            }

            span.machine-mame {
            }

            i.user-name {
                color: #666;
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
            background-color: #000;
            border-radius: 4px;
            border: 1px solid #ddd;
            box-sizing: border-box;

            canvas {
                width: 100%;
                position: absolute;
                height: 100%;
                border-radius: 4px;
            }

            .btns {
                position: absolute;
                left: 0;
                right: 0;
                top: 10%;

                .left {
                    padding-left: 0.6rem;
                }

                .right {
                    padding-right: 0.6rem;
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