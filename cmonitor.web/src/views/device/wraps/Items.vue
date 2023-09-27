<template>
    <template v-if="devices.length > 0">
        <template v-for="(item) in devices" :key="item.MachineName">
            <Item :data="item"></Item>
        </template>
    </template>
    <template v-else>
        <el-empty description="或许你应该先去选择管理设备"></el-empty>
    </template>
</template>
<script>
import Item from './Item.vue'
import { computed, watch } from 'vue'
import { getList } from '../../../apis/signin.js'
import { websocketState } from '../../../apis/request'
import { nextTick, onMounted, onUnmounted } from 'vue'
import { injectGlobalData } from '@/views/provide'
import { subNotifyMsg } from '@/apis/request'
export default {
    components: { Item },
    setup() {

        const files = require.context('../plugins/', true, /index\.js/);
        const pluginSettings = files.keys().map(c => files(c).default);
        pluginSettings.forEach((item) => {
            item.init && item.init();
        });

        const globalData = injectGlobalData();

        const getData = () => {
            getList().then((res) => {
                globalData.value.allDevices = res.map(c => {
                    return pluginSettings.reduce((result, item, index) => {
                        if (item.field) {
                            result = Object.assign(result, item.field());
                        }
                        return result;
                    }, c);
                }).sort((a, b) => a.MachineName < b.MachineName ? -1 : 1);
                nextTick(() => {
                    updateVisibleItems();
                });
            }).catch(() => {
            });
        }

        const devices = computed(() => {
            nextTick(() => {
                updateVisibleItems();
            });
            return globalData.value.devices;
        });

        const subMessage = () => {
            subNotifyMsg('/notify/report/report', (res) => {
                if (globalData.value.reportNames.indexOf(res.Name) == -1) return;
                if (typeof res.Report == 'string') {
                    res.Report = JSON.parse(res.Report);
                }
                let item = globalData.value.devices.filter(c => c.MachineName == res.Name)[0];
                if (item) {
                    pluginSettings.forEach(plugin => {
                        plugin.update && plugin.update(item, res.Report);
                    });
                }
            });
        }

        const listWrapScrollListener = () => {
            nextTick(() => {
                document.querySelector('#device-list-wrap').querySelector('.items').addEventListener('scroll', updateVisibleItems);
            });
        }
        const listWrapRemoveScrollListener = () => {
            try {
                document.querySelector('#device-list-wrap').querySelector('.items').removeEventListener('scroll', updateVisibleItems);
            } catch (e) { }
        }
        const updateVisibleItems = () => {
            try {
                const itemsWrap = document.querySelector('#device-list-wrap').querySelector('.items');
                const scrollTop = itemsWrap.scrollTop;
                const items = itemsWrap.querySelectorAll('.device-item');
                if (items.length == 0) return;
                const wrapHeight = itemsWrap.offsetHeight;

                const doms = [...items].map((item, index) => {
                    const topLine = item.offsetTop - scrollTop;
                    const middleLine = topLine + item.offsetHeight / 2;
                    const offset = Math.abs(middleLine - wrapHeight / 2);
                    return { dom: item, index: index, offset: offset };
                });

                //哪个是在最中间的
                const middleItem = doms.sort((a, b) => a.offset - b.offset)[0];
                for (let i = 0; i < items.length; i++) {
                    let style = 'z-index:9;background-color:rgba(255,255,255,1);';
                    const dist = Math.abs((middleItem.index - i));
                    const opacity = 1 - Math.abs((middleItem.index - i)) / 10 * 2;
                    const translateZ = 100 + (dist * 100);
                    if (i < middleItem.index && middleItem.index > 1) {
                        style = `background-color:rgba(255,255,255,${opacity});z-index:8;transform: translateZ(-${translateZ}px) `;
                        style += `translateY(30px);`;
                    } else if (i > middleItem.index && middleItem.index < items.length - 2) {
                        style = `background-color:rgba(255,255,255,${opacity});z-index:8;transform: translateZ(-${translateZ}px) `;
                        style += `translateY(-30px);`;
                    }
                    items[i].style = style;
                }

                //有哪些需要报告
                const reportDoms = doms.filter(item => item.index >= middleItem.index - 2 && item.index <= middleItem.index + 2).map(c => c.index);
                globalData.value.reportNames = globalData.value.devices
                    .filter((value, index) => reportDoms.indexOf(index) >= 0)
                    .map(c => c.MachineName);
            } catch (e) {
                console.log(e);
            }
        }

        const updateListInterver = () => {
            if (websocketState.connected) {
                getList().then((res) => {
                    globalData.value.allDevices.forEach(c => {
                        let item = res.filter(d => d.MachineName == c.MachineName)[0];
                        if (item) {
                            c.Connected = item.Connected;
                        }
                    });
                }).catch(() => {
                });
            }

            setTimeout(() => {
                updateListInterver();
            }, 1000);
        }

        onMounted(() => {
            getData();
            listWrapScrollListener();
            updateVisibleItems();
            subMessage();
            updateListInterver();
        });
        onUnmounted(() => {
            listWrapRemoveScrollListener();
        });

        return {
            devices
        }
    }
}
</script>

<style lang="stylus" scoped></style>