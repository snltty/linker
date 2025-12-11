<template>
    <a href="javascript:;" class="memory" :title="$t('status.support')" @click="state.showPay = true">
        <img src="@/assets/dianchi.svg" alt="memory" />
        <span>{{$t('status.support')}}</span>
    </a>
    <el-dialog v-model="state.showPay" :title="$t('status.support')" width="80%">
        <div class="pay">
            <p class="t-c">
                <a href="https://afdian.com/a/snltty" class="memory a-line" :title="$t('status.support')" target="_blank">
                    <img src="@/assets/dianchi.svg" alt="memory" />
                    <span>{{$t('status.support')}}</span>
                </a>
            </p>
            <p class="t-c">
                OR
            </p>
            <p>
                <img src="@/assets/pay.jpg" alt="pay" width="100%"/>
            </p>
        </div>
    </el-dialog>
</template>

<script>
import { reactive } from 'vue';

export default {
    setup () {
        const state = reactive({
            showPay:false
        });
        return {state}
    }
}
</script>

<style lang="stylus" scoped>
a.memory{
    img{height:2rem;vertical-align:sub;margin-right:.1rem;}
    margin-right:.6rem;
}
</style>