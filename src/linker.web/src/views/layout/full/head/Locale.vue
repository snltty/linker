<template>
    <PcShow>
        <div class="locale">
            <el-dropdown>
                <span class="el-dropdown-link">
                {{localeOptions[locale]}}
                <el-icon>
                    <ArrowDown />
                </el-icon>
                </span>
                <template #dropdown>
                <el-dropdown-menu>
                    <el-dropdown-item v-for="(item,index) in localeOptions" @click="handleLocale(index)">{{item}}</el-dropdown-item>
                </el-dropdown-menu>
                </template>
            </el-dropdown>
        </div>
    </PcShow>
</template>

<script>
import {ArrowDown} from '@element-plus/icons-vue'
import { computed, ref} from 'vue';
import { LOCALE_OPTIONS } from '@/lang'
import useLocale from '@/lang/provide'
export default {
    components:{ArrowDown},
    setup() {

        const localeOptions = ref(LOCALE_OPTIONS);
        const { changeLocale, currentLocale } = useLocale()
        const locale = computed({
            get() {
                return currentLocale.value
            },
            set(value) {
                changeLocale(value)
            }
        });
        const handleLocale = (index) => {
            locale.value =index;
        }
        return {
            localeOptions,locale,handleLocale
        }
    }
}
</script>

<style lang="stylus" scoped>
.locale{
    padding-right:1rem;
    .el-dropdown{
        vertical-align:middle;
        .el-icon{
            vertical-align:bottom;
        }
    }
}

</style>
