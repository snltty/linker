import { commandPing } from '../../../../apis/command'
import { injectGlobalData } from '@/views/provide';
export default {
    field() {
        return {}
    },
    state: {
        command: {
            showCommand: false,
            items: []
        }
    },
    init() {
    }
}