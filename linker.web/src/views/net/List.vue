<template>
    <div class="net-list-wrap flex flex-column absolute" >
        <div class="flex-1 scrollbar">
            <ul>
                <template v-for="(item,index) in devices.page.List" :key="index">
                    <li>
                        <dl>
                            <dt class="flex">
                                <div>
                                    <DeviceName @edit="handleDeviceEdit" :item="item"></DeviceName>
                                </div>
                                <div class="flex-1"></div>
                                <div>
                                    <UpdaterBtn :config="false" :item="item"></UpdaterBtn>
                                </div>
                            </dt>
                            <dd class="tuntap">
                                <TuntapShow @edit="handleTuntapEdit" :item="item"></TuntapShow>
                            </dd>
                        </dl>
                    </li>
                </template>
            </ul>
        </div>
        <div class="page t-c">
            <div class="page-wrap t-c">
                <el-pagination small background layout="total,sizes,prev,pager, next"  :pager-count="1" :total="devices.page.Count"
                    :page-size="devices.page.Request.Size" :current-page="devices.page.Request.Page"
                    @current-change="handlePageChange" @size-change="handlePageSizeChange" :page-sizes="[10, 20, 50, 100,255]" />
            </div>
        </div>
        <TuntapEdit v-if="tuntap.showEdit" v-model="tuntap.showEdit"  @change="handleTuntapRefresh"></TuntapEdit>
        <DeviceEdit v-if="devices.showDeviceEdit" v-model="devices.showDeviceEdit"  @change="handlePageChange" :data="devices.deviceInfo"></DeviceEdit>
    </div>
</template>
<script>
import { subWebsocketState } from '@/apis/request.js'
import { injectGlobalData } from '@/provide.js'
import { reactive, onMounted,  onUnmounted } from 'vue'
import { provideTuntap } from '../full/devices/tuntap'
import { provideDevices } from '../full/devices/devices'
import { provideUpdater } from '../full/devices/updater'
import { StarFilled} from '@element-plus/icons-vue'
import UpdaterBtn from '../full/devices/UpdaterBtn.vue'
import DeviceName from '../full/devices/DeviceName.vue'
import DeviceEdit from '../full/devices/DeviceEdit.vue'
import TuntapShow from '../full/devices/TuntapShow.vue';
import TuntapEdit from './TuntapEdit.vue'
export default {
    components: {StarFilled,UpdaterBtn,DeviceName,DeviceEdit,TuntapShow,TuntapEdit},
    setup(props) {

        const globalData = injectGlobalData();
        const state = reactive({
        });

        const {devices, machineId, _getSignList, _getSignList1,handleDeviceEdit,
            handlePageChange, handlePageSizeChange, handleDel,clearDevicesTimeout} = provideDevices();
        const {tuntap,_getTuntapInfo,handleTuntapRefresh,clearTuntapTimeout,handleTuntapEdit,sortTuntapIP}  = provideTuntap();
        const {_getUpdater,clearUpdaterTimeout} = provideUpdater();

        onMounted(() => {
            subWebsocketState((state) => { 
                if (state){
                    handlePageChange();
                    _getSignList();
                    handleTuntapRefresh();
                } 
            });
            
            _getSignList();
            _getSignList1();
            _getTuntapInfo();

            _getUpdater();
        });
        onUnmounted(() => {
            clearDevicesTimeout();
            clearTuntapTimeout();
            clearUpdaterTimeout();
        });
        
        return {
            state,devices,handleDeviceEdit, machineId, handlePageChange,handlePageSizeChange, handleDel,
            tuntap,handleTuntapEdit,handleTuntapRefresh
        }
    }
}
</script>
<style lang="stylus" scoped>
.net-list-wrap{

    ul{
        padding:2rem 2rem 1rem 2rem;
        li{
            margin-bottom:1rem;border:1px solid #ddd; background-color:#fff;font-size:1.3rem;
            border-radius:.4rem;
            dt{padding:.6rem;border-bottom:1px solid #ddd;}
            dd.tuntap{padding:1rem;position:relative}
        }
    }

    .page{padding:.6rem 0;border-top:1px solid #ddd;background-color:#f5f5f5;box-shadow:-1px -2px 3px rgba(0,0,0,.05);}
    .page-wrap{
        display:inline-block;
    }
}
</style>