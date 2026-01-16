<template>
    <div class="wakeup-setting-wrap flex flex-column h-100">
        <div class="inner h-100">
            <Wakeup></Wakeup>
        </div>
    </div>
</template>

<script>
import { reactive } from '@vue/reactivity'
import Wakeup from '../../../components/wakeup/Wakeup.vue'
export default {
    components: {
        Wakeup,
    },
    setup(props,{emit}) {
        const state = reactive({
        })
        return {
            state,
        }
    }
}
</script>
<style lang="stylus" scoped>
.wakeup-setting-wrap {
    padding: 1rem;
    box-sizing: border-box;
    font-size:1.3rem

    .inner {
        width 100%;
        box-sizing: border-box;
    }
}
</style>