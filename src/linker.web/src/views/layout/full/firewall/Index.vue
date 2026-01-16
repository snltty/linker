<template>
    <div class="firewall-setting-wrap flex flex-column h-100">
        <div class="inner h-100">
            <Firewall></Firewall>
        </div>
    </div>
</template>

<script>
import { reactive } from '@vue/reactivity'
import Firewall from '../../../components/firewall/Firewall.vue'
export default {
    components: {
        Firewall,
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
.firewall-setting-wrap {
    padding: 1rem;
    box-sizing: border-box;
    font-size:1.3rem

    .inner {
        width 100%;
        box-sizing: border-box;
    }
}
</style>