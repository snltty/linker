<template>
    <div class="keyboard-wrap">
        <a href="javascript:;" class="close"><el-icon>
                <Close />
            </el-icon></a>
        <h3>{{state.name}}</h3>
        <ul>
            <template v-for="(item,index) in state.list" :key="index">
                <li class="flex">
                    <template v-for="(item1,index1) in item" :key="index1">
                        <div class="flex-1 key" :style="item1.style">
                            <div class="inner" @mousedown="handleKeyDown(item1)" @mouseup="handleKeyUp(item1)">
                                {{item1.text}}
                            </div>
                        </div>
                    </template>
                </li>
            </template>
        </ul>
    </div>
</template>

<script>
import { computed, reactive } from 'vue'
import { injectPluginState } from '../../provide';
import { keyboard } from '@/apis/command';
export default {
    setup() {

        const pluginState = injectPluginState();
        const state = reactive({
            name: computed(() => pluginState.value.command.items.length > 0 ? pluginState.value.command.items[0].MachineName : 'unknow'),
            names: computed(() => pluginState.value.command.items.map(c => c.MachineName)),
            list: [

                [
                    { text: 'Esc', style: 'flex:0.8', key: 0x1B }, { text: 'F1', key: 0x70 }, { text: 'F4', key: 0x73 }, { text: 'F5', key: 0x74 }, { text: 'F12', key: 0x7B }, { text: '-', key: 0x6D }, { text: '+', key: 0x6B }
                ],
                [
                    { text: 'Tab', key: 0x9 }, { text: 'Q', key: 0x51 }, { text: 'W', key: 0x57 }, { text: 'E', key: 0x45 }, { text: 'Y', key: 0x59 }, { text: 'Ins', key: 0x2D }, { text: 'Back', key: 0x8 }
                ],
                [
                    { text: 'Cap', key: 0x14 }, { text: 'A', key: 0x41 }, { text: 'S', key: 0x53 }, { text: 'D', key: 0x44 }, { text: 'L', key: 0x4C }, { text: 'Enter', key: 0xD }
                ],
                [
                    { text: 'Shift', style: 'flex:1.5', key: 0xA0 }, { text: 'Z', key: 0x5A }, { text: 'X', key: 0x58 }, { text: 'C', key: 0x43 }, { text: 'V', key: 0x56 }, { text: 'Num', key: 0x90 }
                ],
                [
                    { text: 'Ctrl', key: 0xA2 }, { text: 'Win', key: 0x5B }, { text: 'Alt', key: 18 }, { text: 'Space', key: 0x20 }
                ]
            ]
        });

        const handleKeyDown = (item1) => {
            keyboard(state.names, item1.key, 0);
        }
        const handleKeyUp = (item1) => {
            keyboard(state.names, item1.key, 2);
        }

        return { state, handleKeyDown, handleKeyUp }
    }
}
</script>

<style lang="stylus" scoped>
.keyboard-wrap {
    position: absolute;
    left: 0.6rem;
    right: 0.6rem;
    bottom: 0.6rem;
    border-radius: 4px;
    z-index: 99999;

    &:after {
        content: '';
        position: absolute;
        left: 0;
        top: 0;
        right: 0;
        bottom: 0;
        z-index: -1;
        border-radius: 4px;
        background-color: rgba(219, 234, 255, 0.5);
        border: 1px solid rgba(18, 63, 76, 0.8);
        backdrop-filter: blur(2px);
    }

    a.close {
        position: absolute;
        right: -0.4rem;
        top: -0.8rem;
        color: #fff;
        background-color: rgba(255, 255, 255, 0.2);
        display: block;
        border-radius: 50%;
        border: 1px solid rgba(255, 255, 255, 0.2);
        width: 1.6rem;
        height: 1.6rem;
        text-align: center;
        line-height: 1.6rem;
    }

    h3 {
        text-align: center;
        padding-top: 0.6rem;
        color: #164e51;
    }

    ul {
        padding: 0.6rem;

        .key {
            padding: 0.2rem;
            text-align: center;

            .inner {
                border: 1px solid rgba(46, 90, 95, 0.8);
                background-color: rgba(255, 255, 255, 0.1);
                padding: 0.6rem 0;
                border-radius: 4px;
                color: #164e51;
                transition: 0.1s;
                -webkit-touch-callout: none;
                -webkit-user-select: none;
                -khtml-user-select: none;
                -moz-user-select: none;
                -ms-user-select: none;
                user-select: none;

                &:active {
                    background-color: rgba(46, 90, 95, 0.2);
                    color: #fff;
                }
            }
        }
    }
}
</style>