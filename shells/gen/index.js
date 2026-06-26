const fs = require('fs');
const yaml = require('js-yaml');
const xml2js = require('xml2js');
const moment = require('moment');

const parser = new xml2js.Parser();

function readVersionDesc() {
    return new Promise((resolve, reject) => {
        const fileContents = fs.readFileSync('../../src/linker/linker.csproj', 'utf8');
        parser.parseString(fileContents, (error, result) => {
            resolve(
                { desc: result.Project.PropertyGroup[0].Description[0], version: result.Project.PropertyGroup[0].FileVersion[0] }
            );
        });
    });
}
function readYaml(path) {
    try {
        const fileContents = fs.readFileSync(path, 'utf8');
        return yaml.load(fileContents);
    } catch (e) {
        console.log(e);
    }
}
function writeYaml(path, data) {
    try {
        const yamlContent = yaml.dump(data);
        return fs.writeFileSync(path, yamlContent, 'utf8');
    } catch (e) {
        console.log(e);
    }
}
function readText(path) {
    try {
        const fileContents = fs.readFileSync(path, 'utf8');
        return fileContents;
    } catch (e) {
        console.log(e);
    }
}
function writeText(path, data) {
    try {
        return fs.writeFileSync(path, data, 'utf8');
    } catch (e) {
        console.log(e);
    }
}
function writeUpload(data, tagName) {
    const platforms = {
        'win': ['x86', 'x64', 'arm64'],
        'linux': ['x64', 'arm', 'arm64'],
        'linux-musl': ['x64', 'arm', 'arm64'],
        'osx': ['x64', 'arm64'],
    };
    for (let plat in platforms) {
        let archs = platforms[plat];
        for (let i = 0; i < archs.length; i++) {
            let arch = archs[i];

            data.jobs.build.steps.push({
                name: `upload-${plat}-${arch}-oss`,
                id: `upload-${plat}-${arch}-oss`,
                uses: 'lslzl3000/oss-action@v1.0.1',
                with: {
                    'region': 'oss-cn-shenzhen',
                    'key-id': '${{ secrets.ALIYUN_OSS_ID }}',
                    'key-secret': '${{ secrets.ALIYUN_OSS_SECRET }}',
                    'bucket': 'ide-qbcode',
                    'assets': `./public/publish-zip/linker-${plat}-${arch}.zip:/downloads/linker/${tagName}/linker-${plat}-${arch}.zip`,
                    'timeout': '720000'
                }
            });
            data.jobs.build.steps.push({
                name: `upload-${plat}-${arch}`,
                id: `upload-${plat}-${arch}`,
                uses: 'actions/upload-release-asset@master',
                env: {
                    'GITHUB_TOKEN': '${{ secrets.ACTIONS_TOKEN }}'
                },
                with: {
                    'upload_url': '${{ steps.create_release.outputs.upload_url }}',
                    'asset_path': `./public/publish-zip/linker-${plat}-${arch}.zip`,
                    'asset_name': `linker-${plat}-${arch}.zip`,
                    'asset_content_type': 'application/zip'
                }
            });
        };
    }
    data.jobs.build.steps.push({
        name: `upload-anywhere-oss`,
        id: `upload-anywhere-oss`,
        uses: 'lslzl3000/oss-action@v1.0.1',
        with: {
            'region': 'oss-cn-shenzhen',
            'key-id': '${{ secrets.ALIYUN_OSS_ID }}',
            'key-secret': '${{ secrets.ALIYUN_OSS_SECRET }}',
            'bucket': 'ide-qbcode',
            'assets': `./public/publish-zip/linker-anywhere.zip:/downloads/linker/${tagName}/linker-anywhere.zip`,
            'timeout': '720000'
        }
    });
    data.jobs.build.steps.push({
        name: `upload-anywhere`,
        id: `upload-anywhere`,
        uses: 'actions/upload-release-asset@master',
        env: {
            'GITHUB_TOKEN': '${{ secrets.ACTIONS_TOKEN }}'
        },
        with: {
            'upload_url': '${{ steps.create_release.outputs.upload_url }}',
            'asset_path': `./public/publish-zip/linker-anywhere.zip`,
            'asset_name': `linker-anywhere.zip`,
            'asset_content_type': 'application/zip'
        }
    });
    
    data.jobs.build.steps.push({
        name: `upload-version-oss`,
        id: `upload-version-oss`,
        uses: 'lslzl3000/oss-action@v1.0.1',
        with: {
            'region': 'oss-cn-shenzhen',
            'key-id': '${{ secrets.ALIYUN_OSS_ID }}',
            'key-secret': '${{ secrets.ALIYUN_OSS_SECRET }}',
            'bucket': 'ide-qbcode',
            'assets': `./version.txt:/downloads/linker/version.txt`,
            'timeout': '720000'
        }
    });
    
    
    data.jobs.build.steps.push({
        name: `upload-install-service-oss`,
        id: `upload-install-service-oss`,
        uses: 'lslzl3000/oss-action@v1.0.1',
        with: {
            'region': 'oss-cn-shenzhen',
            'key-id': '${{ secrets.ALIYUN_OSS_ID }}',
            'key-secret': '${{ secrets.ALIYUN_OSS_SECRET }}',
            'bucket': 'ide-qbcode',
            'assets': `./src/linker/linker.service:/downloads/linker/linker.service`,
            'timeout': '720000'
        }
    });
    data.jobs.build.steps.push({
        name: `upload-install-oss`,
        id: `upload-install-oss`,
        uses: 'lslzl3000/oss-action@v1.0.1',
        with: {
            'region': 'oss-cn-shenzhen',
            'key-id': '${{ secrets.ALIYUN_OSS_ID }}',
            'key-secret': '${{ secrets.ALIYUN_OSS_SECRET }}',
            'bucket': 'ide-qbcode',
            'assets': `./src/linker/linker-install.sh:/downloads/linker/linker-install.sh`,
            'timeout': '720000'
        }
    });

    
    data.jobs.build.steps.push({
        name: `upload-apk-oss`,
        id: `upload-apk-oss`,
        uses: 'lslzl3000/oss-action@v1.0.1',
        with: {
            'region': 'oss-cn-shenzhen',
            'key-id': '${{ secrets.ALIYUN_OSS_ID }}',
            'key-secret': '${{ secrets.ALIYUN_OSS_SECRET }}',
            'bucket': 'ide-qbcode',
            'assets': `./public/publish-zip/linker.apk:/downloads/linker/${tagName}/linker.apk`,
            'timeout': '720000'
        }
    });
    data.jobs.build.steps.push({
        name: `upload-apk`,
        id: `upload-apk`,
        uses: 'actions/upload-release-asset@master',
        env: {
            'GITHUB_TOKEN': '${{ secrets.ACTIONS_TOKEN }}'
        },
        with: {
            'upload_url': '${{ steps.create_release.outputs.upload_url }}',
            'asset_path': `./public/publish-zip/linker.apk`,
            'asset_name': `linker.apk`,
            'asset_content_type': 'application/apk'
        }
    });
    
}
function writeUploadIpk(data, tagName) {
    const platforms = ['x64', 'arm', 'arm64'];
    for (let i = 0; i < platforms.length; i++) {
        let arch = platforms[i];

        data.jobs.build.steps.push({
            name: `upload-${arch}-ipk-oss`,
            id: `upload-${arch}-ipk-oss`,
            uses: 'lslzl3000/oss-action@v1.0.1',
            with: {
                'region': 'oss-cn-shenzhen',
                'key-id': '${{ secrets.ALIYUN_OSS_ID }}',
                'key-secret': '${{ secrets.ALIYUN_OSS_SECRET }}',
                'bucket': 'ide-qbcode',
                'assets': `./public/publish-ipk/${arch}/linker-openwrt-${arch}.ipk:/downloads/linker/${tagName}/linker-openwrt-${arch}.ipk`,
                'timeout': '720000'
            }
        });
        data.jobs.build.steps.push({
            name: `upload-${arch}-ipk`,
            id: `upload-${arch}-ipk`,
            uses: 'actions/upload-release-asset@master',
            env: {
                'GITHUB_TOKEN': '${{ secrets.ACTIONS_TOKEN }}'
            },
            with: {
                'upload_url': '${{ steps.get_release.outputs.upload_url }}',
                'asset_path': `./public/publish-ipk/${arch}/linker-openwrt-${arch}.ipk`,
                'asset_name': `linker-openwrt-${arch}.ipk`,
                'asset_content_type': 'application/ipk'
            }
        });
        data.jobs.build.steps.push({
            name: `upload-${arch}-apk-oss`,
            id: `upload-${arch}-apk-oss`,
            uses: 'lslzl3000/oss-action@v1.0.1',
            with: {
                'region': 'oss-cn-shenzhen',
                'key-id': '${{ secrets.ALIYUN_OSS_ID }}',
                'key-secret': '${{ secrets.ALIYUN_OSS_SECRET }}',
                'bucket': 'ide-qbcode',
                'assets': `./public/publish-apk/${arch}/linker-openwrt-${arch}.apk:/downloads/linker/${tagName}/linker-openwrt-${arch}.apk`,
                'timeout': '720000'
            }
        });
        data.jobs.build.steps.push({
            name: `upload-${arch}-apk`,
            id: `upload-${arch}-apk`,
            uses: 'actions/upload-release-asset@master',
            env: {
                'GITHUB_TOKEN': '${{ secrets.ACTIONS_TOKEN }}'
            },
            with: {
                'upload_url': '${{ steps.get_release.outputs.upload_url }}',
                'asset_path': `./public/publish-apk/${arch}/linker-openwrt-${arch}.apk`,
                'asset_name': `linker-openwrt-${arch}.apk`,
                'asset_content_type': 'application/apk'
            }
        });
    };

    data.jobs.build.steps.push({
        name: `upload-docker-fpk-oss`,
        id: `upload-docker-fpk-oss`,
        uses: 'lslzl3000/oss-action@v1.0.1',
        with: {
            'region': 'oss-cn-shenzhen',
            'key-id': '${{ secrets.ALIYUN_OSS_ID }}',
            'key-secret': '${{ secrets.ALIYUN_OSS_SECRET }}',
            'bucket': 'ide-qbcode',
            'assets': `./public/publish-fpk/docker/linker-fnos-docker.fpk:/downloads/linker/${tagName}/linker-fnos-docker.fpk`,
            'timeout': '720000'
        }
    });
    data.jobs.build.steps.push({
        name: `upload-docker-fpk`,
        id: `upload-docker-fpk`,
        uses: 'actions/upload-release-asset@master',
        env: {
            'GITHUB_TOKEN': '${{ secrets.ACTIONS_TOKEN }}'
        },
        with: {
            'upload_url': '${{ steps.get_release.outputs.upload_url }}',
            'asset_path': `./public/publish-fpk/docker/linker-fnos-docker.fpk`,
            'asset_name': `linker-fnos-docker.fpk`,
            'asset_content_type': 'application/fpk'
        }
    });
    const platforms1 = ['x64','arm64'];
    for (let i = 0; i < platforms1.length; i++) {
        let arch = platforms1[i];

        const types = ['bin'];
        for (let j = 0; j < types.length; j++) { 
            const type = types[j];
            data.jobs.build.steps.push({
                name: `upload-${type}-fpk-${arch}-oss`,
                id: `upload-${type}-fpk-${arch}-oss`,
                uses: 'lslzl3000/oss-action@v1.0.1',
                with: {
                    'region': 'oss-cn-shenzhen',
                    'key-id': '${{ secrets.ALIYUN_OSS_ID }}',
                    'key-secret': '${{ secrets.ALIYUN_OSS_SECRET }}',
                    'bucket': 'ide-qbcode',
                    'assets': `./public/publish-fpk/${type}/${arch}/linker-fnos-${type}-${arch}.fpk:/downloads/linker/${tagName}/linker-fnos-${type}-${arch}.fpk`,
                    'timeout': '720000'
                }
            });
            data.jobs.build.steps.push({
                name: `upload-${type}-fpk-${arch}`,
                id: `upload-${type}-fpk-${arch}`,
                uses: 'actions/upload-release-asset@master',
                env: {
                    'GITHUB_TOKEN': '${{ secrets.ACTIONS_TOKEN }}'
                },
                with: {
                    'upload_url': '${{ steps.get_release.outputs.upload_url }}',
                    'asset_path': `./public/publish-fpk/${type}/${arch}/linker-fnos-${type}-${arch}.fpk`,
                    'asset_name': `linker-fnos-${type}-${arch}.fpk`,
                    'asset_content_type': 'application/fpk'
                }
            });
        }
    };
}


readVersionDesc().then((desc) => {

    const data = readYaml('../ymls/dotnet.yml');
    data.jobs.build.steps = data.jobs.build.steps.filter(c => c.uses != 'actions/upload-release-asset@master' && c.uses != 'lslzl3000/oss-action@v1.0.1');

    data.jobs.build.steps.filter(c => c.id == 'create_release')[0].with.body = desc.desc;
    data.jobs.build.steps.filter(c => c.id == 'create_release')[0].with.tag_name = `v${desc.version}`;
    data.jobs.build.steps.filter(c => c.id == 'create_release')[0].with.release_name = `v${desc.version}.\${{ steps.date.outputs.today }}`;

    fs.writeFileSync('../../version.txt', `v${desc.version}\n${moment().format('YYYY-MM-DD HH:mm:ss')}\n${desc.desc}`, 'utf8');

    writeUpload(data, `v${desc.version}`);
    writeYaml('../../.github/workflows/dotnet.yml', data);



    let publishText = readText('../ymls/publish-docker.sh');
    while (publishText.indexOf('{{version}}') >= 0) {
        publishText = publishText.replace('{{version}}', desc.version);
    }
    writeText('../publish-docker.sh', publishText);

    let dockerText = readText('../ymls/docker.yml');
    while (dockerText.indexOf('{{version}}') >= 0) {
        dockerText = dockerText.replace('{{version}}', desc.version);
    }
    writeText('../../.github/workflows/docker.yml', dockerText);



    let nugetText = readText('../ymls/nuget.yml');
    while (nugetText.indexOf('{{version}}') >= 0) {
        nugetText = nugetText.replace('{{version}}', desc.version);
    }
    writeText('../../.github/workflows/nuget.yml', nugetText);



    let publishIpkText = readText('../ymls/publish-ipk.sh');
    while (publishIpkText.indexOf('{{version}}') >= 0) {
        publishIpkText = publishIpkText.replace('{{version}}', desc.version);
    }
    writeText('../publish-ipk.sh', publishIpkText);

    let publishFpkText = readText('../ymls/publish-fpk.sh');
    while (publishFpkText.indexOf('{{version}}') >= 0) {
        publishFpkText = publishFpkText.replace('{{version}}', desc.version);
    }
    writeText('../publish-fpk.sh', publishFpkText);

    const installData = readYaml('../ymls/install.yml');
    writeUploadIpk(installData, `v${desc.version}`);
    writeYaml('../../.github/workflows/install.yml', installData);


});