<template>
    <div class="foot-wrap">
        <ul class="flex">
            <template v-for="(item,index) in footMenuModules" :key="index">
                <li>
                    <component :is="item"></component>
                </li>
            </template>
        </ul>
    </div>
</template>

<script>
export default {
    components: {},
    setup() {
        const footMenuFiles = require.context('../plugins/', true, /FootMenu\.vue/);
        const footMenuModules = footMenuFiles.keys().map(c => footMenuFiles(c).default).sort((a, b) => a.sort - b.sort);
        return { footMenuModules }
    }
}
</script>

<style lang="stylus">
.el-dialog.is-align-center.options {
    margin-top: 3vh;
}
</style>

<style lang="stylus" scoped>
.foot-wrap {
    border-top: 1px solid rgba(0, 0, 0, 0.2);
    position: relative;
    z-index: 999;
}

ul li {
    width: 33.333333%;
    text-align: center;

    a {
        padding: 1.7rem 0;
        font-size: 1.6rem;
        display: block;
        color: #ebffef;
        line-height: 1;

        &:hover {
            background-color: rgba(0, 0, 0, 0.05);
        }
    }
}
</style>