import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

const FeatureList = [
    {
        title: '跨平台、易操作、且安全',
        Svg: require('@site/static/img/undraw_docusaurus_mountain.svg').default,
        description: (
            <>
                <code>.NET8</code>，高性能<code>MemoryPack</code>序列化，易操作web管理页面，所有通信均SSL加密
            </>
        ),
    },
    {
        title: '打洞、中继、和穿透',
        Svg: require('@site/static/img/undraw_docusaurus_tree.svg').default,
        description: (
            <>
                3TCP打洞 + 1UDP打洞 + 1TCP端口映射连接 + 1TCP服务器中继连接 + 1TCP+UDP服务器穿透穿透
            </>
        ),
    },
    {
        title: 'TCP、UDP、什么P',
        Svg: require('@site/static/img/undraw_docusaurus_react.svg').default,
        description: (
            <>
                虚拟网卡组网，端口转发访问，均支持TCP+UDP及其上层协议
            </>
        ),
    },
];

function Feature({ Svg, title, description }) {
    return (
        <div className={clsx('col col--4')}>
            <div style={{ border: '1px solid #ddd' }}>
                <div className="text--center">
                    <Svg className={styles.featureSvg} role="img" />
                </div>
                <div className="text--center padding-horiz--md">
                    <Heading as="h3">{title}</Heading>
                    <p>{description}</p>
                </div>
            </div>
        </div>
    );
}

export default function HomepageFeatures() {
    return (
        <section className={styles.features}>
            <div className="container">
                <div className="row">
                    {FeatureList.map((props, idx) => (
                        <Feature key={idx} {...props} />
                    ))}
                </div>
            </div>
        </section>
    );
}
