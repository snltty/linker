<template>
    <el-dialog class="options-center" :title="$t('status.flowOnline')" destroy-on-close v-model="state.show" width="77rem" top="2vh">
        <div class="map-wrap" id="map">
        </div>
    </el-dialog>
</template>

<script>
import { nextTick, onMounted, reactive, watch } from 'vue';
import { getCitys } from '@/apis/flow';
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
        const drawMap = (citys)=>{

            const map = L.map('map').setView([38,105], 4);
            L.tileLayer('https://webrd01.is.autonavi.com/appmaptile?lang=zh_cn&size=1&scale=1&style=8&x={x}&y={y}&z={z}',
            {
                attribution: 'linker',
                maxZoom: 10
            }).addTo(map);
            for(let i = 0; i < citys.length; i++){
                const item = citys[i];
                const icon = new L.Icon({
                    iconSize:     [18, 28],
                    iconAnchor:   [9, 28],
                    shadowAnchor: [0, 0],
                    popupAnchor:  [0, -27],
                    shadowSize:   [0,0],
                    iconUrl:  '/marker-green.png',
                    shadowUrl: '/marker-shadow.png',
                });
                const html = `
                    <div class="marker-content" >
                        <h3 class="marker-title" style="font-size:1.6rem">${item.City} ${item.Count}</h3>
                    </div>
                `;

                const marker = new L.marker(new L.latLng([item.Lat,item.Lon]),{icon})
                    .bindPopup(html,{})
                    .on('mouseover', function (e) {
                        this.openPopup();
                    }).on('mouseout', function (e) {
                        this.closePopup();
                    }).addTo(map);
            }
        }

        onMounted(()=>{
            nextTick(()=>{
                getCitys().then((citys)=>{
                    drawMap(citys);
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