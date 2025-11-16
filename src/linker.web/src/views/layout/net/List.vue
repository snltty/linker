<template>
    <div class="net-list-wrap flex flex-column absolute" >
        <div class="flex-1 scrollbar">
            <ul>
                <template v-for="(item,index) in devices.page.List" :key="index">
                    <li>
                        <dl>
                            <dt class="flex">
                                <div>
                                    <DeviceName :item="item"></DeviceName>
                                </div>
                                <div class="flex-1"></div>
                                <div>
                                    <UpdaterBtn :config="false" :item="item"></UpdaterBtn>
                                </div>
                            </dt>
                            <dd class="tuntap">
                                <TuntapShow v-if="item.hook_tuntap" :item="item"></TuntapShow>
                            </dd>
                        </dl>
                    </li>
                </template>
            </ul>
        </div>
        <div class="page t-c">
            <div class="page-wrap t-c">
                <el-pagination size="small" background layout="prev,pager, next"  :pager-count="5" :total="devices.page.Count"
                    :page-size="devices.page.Request.Size" :current-page="devices.page.Request.Page"
                    @current-change="handlePageChange" @size-change="handlePageSizeChange" :page-sizes="[10, 20, 50, 100,255]" />
            </div>
        </div>
    </div>
</template>
<script>
import { reactive, onMounted,  onUnmounted } from 'vue'
import { StarFilled} from '@element-plus/icons-vue'
import { provideTuntap } from '../../components/tuntap/tuntap'
import { provideDevices } from '../../components/device/devices'
import UpdaterBtn from '../../components/updater/UpdaterBtn.vue'
import DeviceName from '../../components/device/DeviceName.vue'
import TuntapShow from '../../components/tuntap/TuntapShow.vue';
import { provideConnections } from '../../components/tunnel/connections'
import { provideAccess } from '@/views/components/accesss/access'
import { provideUpdater } from '@/views/components/updater/updater'
export default {
    components: {StarFilled,UpdaterBtn,DeviceName,TuntapShow},
    setup(props) {

        const state = reactive({
        });

        const {devices,deviceAddHook,deviceRefreshHook, deviceStartProcess, handlePageChange, handlePageSizeChange,deviceClearTimeout,setSort} = provideDevices();
        const {accessDataFn,accessProcessFn,accessRefreshFn} = provideAccess();
        deviceAddHook('access',accessDataFn,accessProcessFn,accessRefreshFn);
        const {updater, updaterDataFn, updaterProcessFn,updaterRefreshFn,updaterSubscribe, updaterClearTimeout} = provideUpdater();
        deviceAddHook('updater',updaterDataFn,updaterProcessFn,updaterRefreshFn);
        const {tuntap,tuntapDataFn,tuntapProcessFn,tuntapRefreshFn,getTuntapMachines,sortTuntapIP}  = provideTuntap();
        deviceAddHook('tuntap',tuntapDataFn,tuntapProcessFn,tuntapRefreshFn);
        const {connections,connectionDataFn,connectionProcessFn,connectionRefreshFn } = provideConnections();
        deviceAddHook('connection',connectionDataFn,connectionProcessFn,connectionRefreshFn);
        onMounted(() => {
            handlePageChange();
            
            deviceStartProcess();
        });
        onUnmounted(() => {
            deviceClearTimeout();
            updaterClearTimeout();
        });
        
        return {
            state,devices, handlePageChange,handlePageSizeChange,
            tuntap
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

    .page{padding:.6rem 0;border-top:1px solid #ddd;background-color:rgba(250,250,250,0.5);box-shadow:-1px -2px 3px rgba(0,0,0,.05);}
    .page-wrap{
        display:inline-block;
    }
}
</style>