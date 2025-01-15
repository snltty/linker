const fs = require('fs');
const yaml = require('js-yaml');
const xml2js = require('xml2js');
const moment = require('moment');

const parser = new xml2js.Parser();

function readVersionDesc() {
    return new Promise((resolve, reject) => {
        const fileContents = fs.readFileSync('../src/linker/linker.csproj', 'utf8');
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
function writeUpload(data) {
    const tagName = data.jobs.build.steps.filter(c => c.id == 'create_release')[0].with.tag_name;
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
                uses: 'tvrcgo/oss-action@v0.1.1',
                with: {
                    'region': 'oss-cn-shenzhen',
                    'key-id': '${{ secrets.ALIYUN_OSS_ID }}',
                    'key-secret': '${{ secrets.ALIYUN_OSS_SECRET }}',
                    'bucket': 'ide-qbcode',
                    'asset-path': `./public/publish-zip/linker-${plat}-${arch}.zip`,
                    'target-path': `/downloads/linker/${tagName}/linker-${plat}-${arch}.zip`
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
        name: `upload-windows-route`,
        id: `upload-windows-route`,
        uses: 'actions/upload-release-asset@master',
        env: {
            'GITHUB_TOKEN': '${{ secrets.ACTIONS_TOKEN }}'
        },
        with: {
            'upload_url': '${{ steps.create_release.outputs.upload_url }}',
            'asset_path': `./public/publish-zip/linker-windows-route.zip`,
            'asset_name': `linker-windows-route.zip`,
            'asset_content_type': 'application/zip'
        }
    });
    data.jobs.build.steps.push({
        name: `upload-windows-route-oss`,
        id: `upload-windows-route-oss`,
        uses: 'tvrcgo/oss-action@v0.1.1',
        with: {
            'region': 'oss-cn-shenzhen',
            'key-id': '${{ secrets.ALIYUN_OSS_ID }}',
            'key-secret': '${{ secrets.ALIYUN_OSS_SECRET }}',
            'bucket': 'ide-qbcode',
            'asset-path': `./public/publish-zip/linker-windows-route.zip`,
            'target-path': `/downloads/linker/${tagName}/linker-windows-route.zip`
        }
    });

    data.jobs.build.steps.push({
        name: `upload-version-oss`,
        id: `upload-version-oss`,
        uses: 'tvrcgo/oss-action@v0.1.1',
        with: {
            'region': 'oss-cn-shenzhen',
            'key-id': '${{ secrets.ALIYUN_OSS_ID }}',
            'key-secret': '${{ secrets.ALIYUN_OSS_SECRET }}',
            'bucket': 'ide-qbcode',
            'asset-path': `./public/version.txt`,
            'target-path': `/downloads/linker/version.txt`
        }
    });

    data.jobs.build.steps.push({
        name: `upload-install-service-oss`,
        id: `upload-install-service-oss`,
        uses: 'tvrcgo/oss-action@v0.1.1',
        with: {
            'region': 'oss-cn-shenzhen',
            'key-id': '${{ secrets.ALIYUN_OSS_ID }}',
            'key-secret': '${{ secrets.ALIYUN_OSS_SECRET }}',
            'bucket': 'ide-qbcode',
            'asset-path': `./src/linker/linker.service`,
            'target-path': `/downloads/linker/linker.service`
        }
    });
    data.jobs.build.steps.push({
        name: `upload-install-oss`,
        id: `upload-install-oss`,
        uses: 'tvrcgo/oss-action@v0.1.1',
        with: {
            'region': 'oss-cn-shenzhen',
            'key-id': '${{ secrets.ALIYUN_OSS_ID }}',
            'key-secret': '${{ secrets.ALIYUN_OSS_SECRET }}',
            'bucket': 'ide-qbcode',
            'asset-path': `./src/linker/linker-install.sh`,
            'target-path': `/downloads/linker/linker-install.sh`
        }
    });

}

readVersionDesc().then((desc) => {

    const data = readYaml('../ymls/dotnet.yml');
    data.jobs.build.steps = data.jobs.build.steps.filter(c => c.uses != 'actions/upload-release-asset@master' && c.uses != 'tvrcgo/oss-action@v0.1.1');

    data.jobs.build.steps.filter(c => c.id == 'create_release')[0].with.body = desc.desc;
    data.jobs.build.steps.filter(c => c.id == 'create_release')[0].with.tag_name = `v${desc.version}`;
    data.jobs.build.steps.filter(c => c.id == 'create_release')[0].with.release_name = `v${desc.version}.\${{ steps.date.outputs.today }}`;

    fs.writeFileSync('../version.txt', `v${desc.version}\n${moment().format('YYYY-MM-DD HH:mm:ss')}\n${desc.desc}`, 'utf8');

    writeUpload(data);
    writeYaml('../.github/workflows/dotnet.yml', data);

    let publishText = readText('../ymls/publish-docker.sh');
    while (publishText.indexOf('{{version}}') >= 0) {
        publishText = publishText.replace('{{version}}', desc.version);
    }
    writeText('../publish-docker.sh', publishText);


    let dockerText = readText('../ymls/docker.yml');
    while (dockerText.indexOf('{{version}}') >= 0) {
        dockerText = dockerText.replace('{{version}}', desc.version);
    }
    writeText('../.github/workflows/docker.yml', dockerText);


    let nugetText = readText('../ymls/nuget.yml');
    while (nugetText.indexOf('{{version}}') >= 0) {
        nugetText = nugetText.replace('{{version}}', desc.version);
    }
    writeText('../.github/workflows/nuget.yml', nugetText);
});