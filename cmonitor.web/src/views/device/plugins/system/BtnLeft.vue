<template>
    <div>
        <a href="javascript:;" @click="handleSystemOptions">
            <el-icon>
                <Tools />
            </el-icon>
            <span class="value">{{data.System.RegistrysCount}}</span>
        </a>
    </div>
</template>

<script>
import { injectPluginState } from '../../provide'
export default {
    props: ['data'],
    setup(props) {

        const pluginState = injectPluginState();
        const handleSystemOptions = () => {
            pluginState.value.system.devices = [props.data];
            pluginState.value.system.showSystemOptions = true;
        }

        return {
            handleSystemOptions
        }
    }
}
</script>

<style lang="stylus" scoped>
a {
    width: 2.4rem;
    height: 2.4rem;
    text-align: center;
    line-height: 2.8rem;
    margin-bottom: 0.6rem;
    display: block;
    font-size: 2rem;
    border-radius: 50%;
    border: 1px solid #3e5a6e;
    box-shadow: 0 0 4px rgba(255, 255, 255, 0.1);
    background-color: rgba(255, 255, 255, 0.5);
    color: #3e5a6e;
    transition: 0.3s;

    &:hover {
        box-shadow: 0 0 4px 2px rgba(255, 255, 255, 0.5);
    }

    position: relative;

    span.value {
        position: absolute;
        left: 110%;
        top: 50%;
        transform: translateY(-50%);
        color: rgba(255, 255, 255, 255);
        text-shadow: 1px 1px 1px rgba(0, 0, 0, 255);
        font-size: 1.4rem;
    }
}
</style>