<template>
    <div class="net-head-setting">
        <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="8rem">
            <el-form-item label="设备名" prop="name">
                <a href="javascript:;" class="name">{{ state.form.name }}</a>
                <!-- <el-input v-model="state.form.name" maxlength="12" show-word-limit /> -->
            </el-form-item>
            <el-form-item label="服务器" prop="server">
                <a href="javascript:;">{{  state.form.server }}</a>
                <!-- <el-input v-model="state.form.server" /> -->
            </el-form-item>
            <el-form-item label="分组号" prop="groupid">
                <!-- <a href="javascript:;">{{  state.form.groupid }}</a> -->
                <el-input v-model="state.form.groupid" readonly size="small" />
            </el-form-item>
            <el-form-item label="本机IP" prop="ip">
                <a href="javascript:;">{{  state.form.ip }}</a>
                <!-- <el-input v-model="state.form.groupid" /> -->
            </el-form-item>
        </el-form>
    </div>
</template>

<script>
import { injectGlobalData } from '@/provide';
import { computed, reactive } from 'vue';

export default {
    setup () {

        const globalData = injectGlobalData();
        const state = reactive({
            form: {
                ip:computed(()=>globalData.value.config.Running.Tuntap.IP || "0.0.0.0"),
                name:computed(()=>globalData.value.config.Client.Name || "snltty"),
                groupid:computed(()=>globalData.value.config.Client.GroupId || "snltty"),
                server:computed(()=>globalData.value.config.Client.Server || "linker.snltty.com:1802"),
                relaySecretKey:(globalData.value.config.Running.Relay.Servers[0] || {SecretKey:'snltty'}).SecretKey,
            },
            rules: {
            }
        });

        return {
            state
        }
    }
}
</script>

<style lang="stylus">
.net-head-setting .el-form-item--default{margin-bottom:.2rem;}
.net-head-setting .el-form-item--default .el-form-item__label,
.net-head-setting .el-form-item--default .el-form-item__content{height:2.6rem;line-height:2.6rem;}
</style>
<style lang="stylus" scoped>
.name{font-weight:bold;font-size:2rem}
</style>