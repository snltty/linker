<template>
    <el-dialog class="options-center" :title="$t('status.flowOnline')" destroy-on-close v-model="state.show" width="77rem" top="2vh">
        <div class="map-wrap" id="map">
        </div>
    </el-dialog>
</template>

<script>
import { nextTick, onMounted, reactive, watch } from 'vue';
import { getTunnelInfo } from '@/apis/tunnel';
import { getSignInNames } from '@/apis/signin';
import { useI18n } from 'vue-i18n';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    setup (props,{emit}) {

        const {t} = useI18n();
        const state = reactive({
            show: true,
        });
        
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });
        const drawMap = (tunnels,devices)=>{
            const map = L.map('map').setView([38,105], 4);
            L.tileLayer('https://webrd01.is.autonavi.com/appmaptile?lang=zh_cn&size=1&scale=1&style=8&x={x}&y={y}&z={z}',
            {
                attribution: 'linker',
                maxZoom: 10
            }).addTo(map);
            for(let i = 0; i < devices.length; i++){
                try{
                    const item = devices[i];
                    if(!tunnels[item.MachineId]) continue;
                    const net = tunnels[item.MachineId].Net;
                    if(net.Lat == 0 || net.Lon == 0) continue;
                    const icon = new L.Icon({
                        iconSize:     [18, 28],
                        iconAnchor:   [9, 28],
                        shadowAnchor: [0, 0],
                        popupAnchor:  [0, -27],
                        shadowSize:   [0,0],
                        iconUrl: item.Online ? '/marker-green.png' : '/marker-grey.png',
                        shadowUrl: '/marker-shadow.png',
                    });
                    const html = `
                        <div class="marker-content" >
                            <h3 class="marker-title" style="font-size:1.6rem">${item.MachineName}</h3>
                            <div class="marker-text" style="font-size:1.3rem">${t('status.flowArea')} : ${net.CountryCode}、${net.City}</div>
                            <div class="marker-text" style="font-size:1.3rem">${t('status.flowNet')} : ${net.Isp}</div>
                        </div>
                    `;
                    
                    const marker = new L.marker(new L.latLng([net.Lat+ Math.random()/5,net.Lon+ Math.random()/5]),{icon})
                        .bindPopup(html,{})
                        .on('mouseover', function (e) {
                            this.openPopup();
                        }).on('mouseout', function (e) {
                            this.closePopup();
                        }).addTo(map);
                        
                }catch(e){
                    console.log(e);
                }
            }
        }

        onMounted(()=>{
            nextTick(()=>{
                getTunnelInfo().then((tunnels)=>{
                    getSignInNames().then(devices=>{
                        drawMap(tunnels.List,devices);
                    }).catch(()=>{});
                }).catch(()=>{});
               
            });
        })

        return {
            state
        }
    }
}
</script>

<style lang="stylus" scoped>
#map{
    height:60rem;
}
</style>