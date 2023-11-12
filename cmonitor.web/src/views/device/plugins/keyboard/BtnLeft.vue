<template>
    <div>
        <a href="javascript:;" @click="handleKeyBoard">
            <img src="../../../../assets/keyboard.svg" width="20">
        </a>
    </div>
</template>

<script>
import { injectPluginState } from '../../provide'
export default {
    props: ['data'],
    setup(props) {

        const pluginState = injectPluginState();
        const handleKeyBoard = () => {
            pluginState.value.keyboard.devices = [props.data];
            pluginState.value.keyboard.showKeyBoard = true;
        }

        return {
            handleKeyBoard
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
}
</style>