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

        const globalData = injectGlobalData();
        const plugins = computed(()=>globalData.value.config.Common.Plugins||[]);

        const files = require.context('../plugins/', true, /index\.js/);
        const _pluginSettings = files.keys().map(c => files(c).default);
        const pluginSettings = computed(()=>_pluginSettings.filter(c=>plugins.value.length == 0 || plugins.value.indexOf(c.pluginName)>=0))

        pluginSettings.value.forEach((item) => {
            try {
                item.init && item.init();
            } catch (e) {
                console.log(e);
            }
        });

        watch(() => globalData.value.updateDeviceFlag, () => {
            getData();
        })

        const getData = () => {
            getList(globalData.value.groupid).then((res) => {
                globalData.value.allDevices = res.map(c => {
                    return pluginSettings.value.reduce((result, item, index) => {
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
                    pluginSettings.value.forEach(plugin => {
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

                let totalHeight = 0;
                let doms = [...items].map((item, index) => {
                    const topLine = item.offsetTop - scrollTop;
                    const middleLine = topLine + item.offsetHeight / 2;
                    const offset = Math.abs(middleLine - wrapHeight / 2);
                    totalHeight += item.offsetHeight + 6;
                    return { dom: item, index: index, top: item.offsetTop, offset: offset, height: item.offsetHeight };
                });

                //哪个是在最中间的
                const sorted = doms.sort((a, b) => a.offset - b.offset);
                const middleItem = sorted[0];
                if (scrollTop < doms[0].height / 2) {
                    globalData.value.currentDevice = globalData.value.devices[0];
                } else if (scrollTop + wrapHeight >= totalHeight) {
                    globalData.value.currentDevice = globalData.value.devices[globalData.value.devices.length - 1];
                } else {
                    globalData.value.currentDevice = globalData.value.devices[middleItem.index];
                }

                if (globalData.value.pc) {

                } else {
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
                }


                //有哪些需要报告
                const reportDoms = doms.filter(item => item.index >= middleItem.index - 2 && item.index <= middleItem.index + 2).map(c => c.index);
                globalData.value.reportNames = globalData.value.devices
                    .filter(c => c.Connected)
                    .filter((value, index) => reportDoms.indexOf(index) >= 0)
                    .map(c => c.MachineName);
            } catch (e) {
                console.log(e);
            }
        }

        let getListTimer = 0;
        const updateListInterver = () => {
            if (websocketState.connected) {
                getList(globalData.value.groupid).then((res) => {
                    globalData.value.allDevices.forEach(c => {
                        let item = res.filter(d => d.MachineName == c.MachineName)[0];
                        if (item) {
                            c.Connected = item.Connected;
                        }
                    });
                }).catch(() => {
                });
            }

            getListTimer = setTimeout(() => {
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
            clearTimeout(getListTimer);

            pluginSettings.value.forEach((item) => {
                item.uninit && item.uninit();
            });
        });

        return {
            devices
        }
    }
}
</script>

<style lang="stylus" scoped></style>