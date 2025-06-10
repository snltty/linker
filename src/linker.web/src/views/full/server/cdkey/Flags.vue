<template>
    <div class="wrap">
        <el-radio-group v-model="state.time" size="small" @change="handleChange">
            <el-radio-button :label="$t('server.cdkeyFlagAll')" :value="0" />
            <el-radio-button :label="$t('server.cdkeyFlagTimein')" :value="1" />
            <el-radio-button :label="$t('server.cdkeyFlagTimeout')" :value="2"/>
        </el-radio-group>
        <el-radio-group v-model="state.bytes" size="small" @change="handleChange">
            <el-radio-button :label="$t('server.cdkeyFlagAll')" :value="0" />
            <el-radio-button :label="$t('server.cdkeyFlagBytesin')" :value="4" />
            <el-radio-button :label="$t('server.cdkeyFlagBytesout')" :value="8"/>
        </el-radio-group>
        <el-radio-group v-model="state.deleted" size="small" @change="handleChange">
            <el-radio-button :label="$t('server.cdkeyFlagAll')" :value="0" />
            <el-radio-button :label="$t('server.cdkeyFlagUnDeleted')" :value="16" />
            <el-radio-button :label="$t('server.cdkeyFlagDeleted')" :value="32"/>
        </el-radio-group>
    </div>
</template>

<script>
import { onMounted, reactive } from 'vue';

export default {
    emits:['change'],
    setup (props,{emit}) {
    
        const state = reactive({ 
            time: 1,
            bytes: 4,
            deleted: 16,
        });

        const handleChange = () => {
            emit('change',state.time | state.bytes | state.deleted);
        }   

        onMounted(() => {
            handleChange();
        })

        return {state,handleChange}
    }
}
</script>

<style lang="stylus" scoped>
.el-radio-group{margin-right:.6rem}
.wrap{padding-bottom:1rem}
</style>