<template>
    <span v-if="item.MachineId">
        <a href="javascript:;" :title="wlist.title" @click="handleWlist">
            <img :src="wlist.img" />
        </a>
    </span>
</template>

<script>
import { computed } from 'vue';
import { useWlist } from './wlist';
import { useI18n } from 'vue-i18n';

export default {
    props: ['item','type'],
    setup (props) {
        const {t} = useI18n();
        const wlistState = useWlist();
        const text = {'Relay':t('wlist.relay'),'Reverse':t('wlist.reverse')}[props.type];
        const choiceOnece = (json)=>{
            const arr = Object.keys(json).reduce((arr,curr)=>{
                arr.push({ id:curr,value:json[curr] });
                return arr;
            },[]);

            const denyOrAllow = arr.filter(c=>c.value <= 0);
            const result = denyOrAllow.length > 0 
            ? denyOrAllow.sort((a,b)=>a.value - b.value)[0]
            : arr.sort((a,b)=>b.value - a.value)[0];

            return {
                id:result.id,
                value:result.value,
                title:result.value < 0 ? t('"wlist.deny',[text]): result.value == 0 ? t('wlist.allow',[text]) : t('wlist.limit',[`${text}、${result.value}Mbps`]),
                img:result.value < 0 ? 'blist.svg':'wlist.svg'
            };
        }

        const wlist = computed(()=>props.item.hook_wlist === undefined || Object.keys(props.item.hook_wlist).length == 0
        ? {id:0,value:0,title:t('wlist.none'),img:'wlist-none.svg'}
        : choiceOnece(props.item.hook_wlist));

        const handleWlist = ()=>{
            if(!props.item.MachineId) return;
            wlistState.value.device.id = props.item.MachineId;
            wlistState.value.device.name = props.item.MachineName;
            wlistState.value.device.type = props.type;
            wlistState.value.device.typeText = text;
            wlistState.value.show = true;
        }

        return {
            wlist,handleWlist
        }
    }
}
</script>

<style lang="stylus" scoped>
img{
    height:1.4rem;
    vertical-align: middle;
    margin-right:.1rem
}
</style>