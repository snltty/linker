<template>
    <el-dialog class="options" title="窗口使用时间统计" destroy-on-close v-model="state.show" center align-center width="94%">
        <div class="wrap flex flex-column">
            <h4>{{state.startTime}} - 至今({{(state.totalTime/1000).toFixed(2)}}s)</h4>
            <div class="inner flex-1 scrollbar">
                <ul>
                    <template v-for="(item,index) in state.list" :key="index">
                        <li>
                            <dl>
                                <dt>{{item.Desc}} <el-button @click="showTitles(item)" size="small">{{item.titleLength}}</el-button></dt>
                                <dd>
                                    <el-progress :percentage="(item.Time/state.totalTime*100).toFixed(2)" />
                                </dd>
                            </dl>
                        </li>
                    </template>
                </ul>
            </div>
        </div>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
            <el-button type="success" @click="handleCancel" plain>确 定</el-button>
        </template>
    </el-dialog>
    <el-dialog class="options" title="详细标题" destroy-on-close v-model="state.showTitles" center align-center width="94%">
        <div class="wrap flex flex-column">
            <h5>{{state.currentTime/1000}}s</h5>
            <div class="inner flex-1 scrollbar">
                <ul>
                    <template v-for="(item,index) in state.currentTitles" :key="index">
                        <li>
                            <dl>
                                <dt>{{item.t}}</dt>
                                <dd>
                                    <el-progress :percentage="(item.v/state.currentTime*100).toFixed(2)" />
                                </dd>
                            </dl>
                        </li>
                    </template>
                </ul>
            </div>
        </div>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
            <el-button type="success" @click="handleCancel" plain>确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { reactive } from '@vue/reactivity';
import { onMounted, watch } from '@vue/runtime-core';
import { getActiveTimes } from '../../../../apis/active'
import { injectPluginState } from '../../provide'
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: {},
    setup(props, { emit }) {

        const pluginState = injectPluginState();
        const state = reactive({
            show: props.modelValue,
            loading: false,
            startTime: new Date(),
            totalTime: 1,
            list: [],
            showTitles: false,
            currentTitles: [],
            currentTime: 1,
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });
        const getData = () => {
            getActiveTimes(pluginState.value.activeWindow.devices[0].MachineName).then((res) => {
                state.startTime = res.StartTime;
                state.totalTime = res.List.reduce((val, item, index) => {
                    item.titleLength = Object.keys(item.Titles).length;
                    val += item.Time;
                    return val;
                }, 0);
                state.list = res.List.sort((a, b) => b.Time - a.Time);
            }).catch((e) => {
            })
        }
        const showTitles = (item) => {
            state.showTitles = true;
            let res = [];
            let time = 0;
            for (let j in item.Titles) {
                res.push({
                    t: j,
                    v: item.Titles[j]
                });
                time += item.Titles[j];
            }
            state.currentTitles = res.sort((a, b) => b.v - a.v);
            state.currentTime = time;
        }

        onMounted(() => {
            getData();
        });
        const handleCancel = () => {
            state.show = false;
        }

        return {
            state, handleCancel, showTitles
        }
    }
}
</script>
<style lang="stylus" scoped>
.wrap {
    height: 70vh;

    .inner {
        border: 1px solid #ddd;
        border-radius: 4px;
        padding: 1rem 0.6rem 1rem 1rem;

        li {
            border: 1px solid #ddd;
            padding: 0.6rem;
            margin-bottom: 0.6rem;
            border-radius: 0.4rem;
        }

        dt {
            word-break: break-all;
        }

        dd {
            .time {
                height: 1rem;
                background-color: green;
            }
        }
    }
}
</style>