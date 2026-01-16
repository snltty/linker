<template>
    <div class="logger-setting-wrap flex flex-column h-100" ref="wrap">
        <el-tabs type="border-card" class="h-100 w-100">
            <AccessShow value="LoggerShow">
                <el-tab-pane :label="$t('logger.list')" class="h-100">
                    <div class="inner h-100 flex flex-column flex-nowrap">
                        <div class="head flex">
                            <div>
                                <el-select v-model="state.type" @change="loadData" size="small" class="mgr-1" style="width: 6rem;">
                                    <el-option :value="-1" label="all"></el-option>
                                    <el-option :value="0" label="debug"></el-option>
                                    <el-option :value="1" label="info"></el-option>
                                    <el-option :value="2" label="warning"></el-option>
                                    <el-option :value="3" label="error"></el-option>
                                    <el-option :value="4" label="fatal"></el-option>
                                </el-select>
                            </div>
                            <el-button type="warning" size="small" :loading="state.loading" @click="clearData">{{$t('logger.clear')}}</el-button>
                            <el-button size="small" :loading="state.loading" @click="loadData">{{$t('logger.refresh')}}</el-button>
                            <span class="flex-1"></span>
                        </div>
                        <div class="body flex-1 relative">
                            <div class="absolute">
                                <el-table stripe border :data="state.page.List" size="small" width="100%" height="100%" @row-click="handleRowClick" :row-class-name="tableRowClassName">
                                    <el-table-column type="index" width="50" />
                                    <el-table-column prop="Type" :label="$t('logger.level')" width="80">
                                        <template #default="scope">
                                            <span>{{state.types[scope.row.Type]}} </span>
                                        </template>
                                    </el-table-column>
                                    <el-table-column prop="Time" :label="$t('logger.time')" width="160"></el-table-column>
                                    <el-table-column prop="content" :label="$t('logger.content')"></el-table-column>
                                </el-table>
                            </div>
                        </div>
                        <div class="pages t-c">
                            <div class="page-wrap">
                                <el-pagination small :total="state.page.Count"
                                v-model:currentPage="state.page.Page" :page-size="state.page.Size" 
                                :pager-count="globalData.isPc?7:3"
                                :layout="globalData.isPc?'total,prev, pager, next':'prev, pager, next'"
                                @current-change="handlePageChange" background 
                            >
                                </el-pagination>
                            </div>
                        </div>
                    </div>
                </el-tab-pane>
            </AccessShow>
            <AccessShow value="LoggerLevel">
                <el-tab-pane :label="$t('common.setting')">
                    <Setting></Setting>
                </el-tab-pane>
            </AccessShow>
        </el-tabs>
    </div>
    <el-dialog class="options-center" title="" destroy-on-close v-model="state.show" width="98%" top="2vh">
        <div>
            <textarea class="logger-content">{{ state.content }}</textarea>
        </div>
    </el-dialog>
</template>

<script>
import { reactive,computed } from '@vue/reactivity'
import { getLogger, clearLogger } from '@/apis/logger'
import { onMounted } from '@vue/runtime-core'
import Setting from './Setting.vue'
import {  ref } from 'vue'
import { injectGlobalData } from '@/provide'
export default {
    components: { Setting },
    setup() {
        const globalData = injectGlobalData();
        const wrap = ref(null);
        const state = reactive({
            loading: true,
            type:-1,
            page: { Page: 1, Size: 20, Count: 0, List: [] },
            types: ['debug', 'info', 'warning', 'error', 'fatal'],

            show:false,
            content:''
        })
        const loadData = () => {
            state.loading = true;
            getLogger({
                Page : state.page.Page,
                Size : state.page.Size,
                Type : state.type
            }).then((res) => {
                state.loading = false;
                res.List.map(c => {
                    c.content = c.Content.substring(0, 50);
                });
                state.page = res;
            }).catch((err) => {
                console.log(err);
                state.loading = false;
            });
        }
        const handlePageChange = (page)=>{
            if(page){
                state.page.Page = page;
                loadData();
            }
        }
        const clearData = () => {
            state.loading = true;
            clearLogger().then(() => {
                state.loading = false;
                loadData();
            }).catch(() => {
                state.loading = false;
            });
        }

        const tableRowClassName = ({ row, rowIndex }) => {
            return `type-${row.Type}`;
        }
        const handleRowClick = (row, column, event) => {
            state.show = true;
            state.content = row.Content;
        }

        onMounted(()=>{
            loadData();
        });

        return {
            globalData,wrap,state, loadData, clearData, tableRowClassName, handleRowClick,handlePageChange
        }
    }
}
</script>
<style lang="stylus" scoped>
.pages {
    padding: 1rem 0 0 1rem;
}

.page-wrap{
        display:inline-block;
    }

.logger-setting-wrap {
    padding: 1rem;
    box-sizing: border-box;

    .inner {
        padding: 1rem;
        box-sizing: border-box;
    }

    .head {
        margin-bottom: 1rem;
    }
}
.logger-content {
    width: 100%;
    height: 40rem;
    box-sizing: border-box;
    padding: 1rem;
    margin-top:1rem;
    background: #f5f5f5;
    border:1px solid #eee;
    border-radius: 4px;
    font-size: 1.2rem;
    resize: none;
    outline: none;
    overflow: auto;
    white-space:nowrap;
}
</style>
<style  lang="stylus">
.logger-setting-wrap {
    .el-table {
        .type-0 {
            color: blue;
        }

        .type-1 {
            color: #333;
        }

        .type-2 {
            color: #cd9906;
        }

        .type-3 {
            color: red;
        }

        .type-4 {
            color: red;
            font-weight: bold;
        }
    }
}
</style>