import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import HomepageFeatures from '@site/src/components/HomepageFeatures';

import Heading from '@theme/Heading';
import styles from './index.module.css';

function HomepageHeader() {
    const { siteConfig } = useDocusaurusContext();
    return (
        <header className={clsx('hero hero--primary', styles.heroBanner)}>
            <div className="container">
                <Heading as="h1" className="hero__title">
                    {siteConfig.title}
                </Heading>
                <p className="hero__subtitle">{siteConfig.tagline}</p>
                <div className={styles.buttons}>
                    <Link
                        className="button button--secondary button--lg button--warning"
                        to="/docs/home">
                        文档
                    </Link>
                    <Link
                        className="button button--secondary button--lg button--info"
                        to="https://linker.snltty.com" style={{ marginLeft: '10px'} }>
                        官网
                    </Link>
                    <Link
                        className="button button--secondary button--lg"
                        to="https://github.com/snltty/linker" style={{ marginLeft: '10px'} }>
                        开源
                    </Link>
                    <Link
                        className="button button--secondary button--lg button--outline"
                        to="https://afdian.com/a/snltty" style={{ marginLeft: '10px',color:'#fff'} }>
                        捐助
                    </Link>
                </div>
            </div>
        </header>
    );
}

export default function Home() {
    const { siteConfig } = useDocusaurusContext();
    return (
        <Layout
            title={`${siteConfig.title}`}
            description="linker、海内皆隔壁，天涯若比邻。">
            <HomepageHeader />
            <main>
                <HomepageFeatures />
            </main>
        </Layout>
    );
}
