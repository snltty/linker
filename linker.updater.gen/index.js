const fs = require('fs');
const yaml = require('js-yaml');
const xml2js = require('xml2js');
const parser = new xml2js.Parser();

const path = '../.github/workflows/dotnet.yml';
const projPath = '../linker/linker.csproj';

function readVersionDesc() {
    return new Promise((resolve, reject) => {
        const fileContents = fs.readFileSync(projPath, 'utf8');
        parser.parseString(fileContents, (error, result) => {
            resolve(
                { desc: result.Project.PropertyGroup[0].Description[0], version: result.Project.PropertyGroup[0].FileVersion[0] }
            );
        });
    });
}

function readYaml() {
    try {
        const fileContents = fs.readFileSync(path, 'utf8');
        return yaml.load(fileContents);
    } catch (e) {
        console.log(e);
    }
}
function writeYaml(data) {
    try {
        const yamlContent = yaml.dump(data);
        return fs.writeFileSync(path, yamlContent, 'utf8');
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
}

readVersionDesc().then((desc) => {

    const data = readYaml();
    data.jobs.build.steps = data.jobs.build.steps.filter(c => c.uses != 'actions/upload-release-asset@master' && c.uses != 'tvrcgo/oss-action@v0.1.1');

    data.jobs.build.steps.filter(c => c.id == 'create_release')[0].with.body = desc.desc;
    data.jobs.build.steps.filter(c => c.id == 'create_release')[0].with.tag_name = `v${desc.version}`;
    data.jobs.build.steps.filter(c => c.id == 'create_release')[0].with.release_name = `v${desc.version}.\${steps.date.outputs.today}`;

    fs.writeFileSync('../version.txt', `v${desc.version}\n${new Date().toISOString().split('T')[0]}\n${desc.desc}`, 'utf8');

    writeUpload(data);
    writeYaml(data);
});