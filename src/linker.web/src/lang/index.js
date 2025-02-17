import { createI18n } from 'vue-i18n'
import en from './en-us'
import cn from './zh-cn'

export const LOCALE_OPTIONS = {
    'zh-CN': '简体中文',
    'en-US': 'English',
};
const i18n = createI18n({
    locale: localStorage.getItem('locale-lang') || navigator.language || navigator.browserLanguage,
    fallbackLocale: 'zh-CN',
    legacy: false,
    allowComposition: true,
    messages: {
        'en-US': en,
        'zh-CN': cn
    }
})
export default i18n