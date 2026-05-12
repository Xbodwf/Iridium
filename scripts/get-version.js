const fs = require('fs');
const path = require('path');

/**
 * @param {string} [adofaiVer] - ADOFAI version string, e.g. "2.10.0" or "2.9.8"
 *                                 If omitted, reads from BuildInfo.cs (first match).
 */
function getVersionInfo(adofaiVer) {
    try {
        const infoPath = path.join(__dirname, '..', 'Info.json');
        const infoContent = fs.readFileSync(infoPath, 'utf8');
        const info = JSON.parse(infoContent);

        const baseVersion = info.Version || '1.0.0';
        const displayName = info.DisplayName || 'Iridium';

        const vmPath = path.join(__dirname, '..', 'VersionManager.cs');
        const vmContent = fs.readFileSync(vmPath, 'utf8');

        const typeMatch = vmContent.match(/public\s+static\s+VersionType\s+Type\s*=>\s*VersionType\.(\w+)\s*;/);
        const minorMatch = vmContent.match(/public\s+const\s+int\s+MinorVersion\s*=\s*(\d+)\s*;/);

        const vtype = typeMatch ? typeMatch[1].toLowerCase() : 'release';
        const minor = minorMatch ? minorMatch[1] : '0';

        const versionParts = baseVersion.split('.');
        const major = parseInt(versionParts[0]) || 1;
        const minor_version = parseInt(versionParts[1]) || 0;
        const patch = parseInt(versionParts[2]) || 0;

        let releaseNumber;
        if (major === 1) {
            releaseNumber = minor_version * 10 + patch + 1;
        } else {
            releaseNumber = major * 100 + minor_version * 10 + patch + 1;
        }

        // Resolve ADOFAI version: parameter > env > BuildInfo.cs
        if (!adofaiVer) {
            adofaiVer = process.env.GAME_VERSION || '';
        }
        if (!adofaiVer) {
            const biPath = path.join(__dirname, '..', 'BuildInfo.cs');
            const biContent = fs.readFileSync(biPath, 'utf8');
            const biMatch = biContent.match(/AdofaiVersion\s*=\s*"([^"]+)"/);
            adofaiVer = biMatch ? biMatch[1] : 'unknown';
        }
        // Strip leading 'v' if present
        adofaiVer = adofaiVer.replace(/^v/, '');

        let versionTag;
        let releaseName;
        let tagName;

        if (vtype === 'release') {
            versionTag = `${baseVersion}+adofai${adofaiVer}`;
            releaseName = `${baseVersion}+adofai${adofaiVer}`;
            tagName = `r${releaseNumber}`;
        } else {
            versionTag = `${baseVersion}_${vtype}${minor}+adofai${adofaiVer}`;
            releaseName = `${baseVersion}_${vtype}${minor}+adofai${adofaiVer}`;
            tagName = `r${releaseNumber}_${vtype}${minor}`;
        }

        return {
            VERSION_TAG: versionTag,
            RELEASE_NAME: releaseName,
            TAG_NAME: tagName,
            RELEASE_NUMBER: releaseNumber
        };

    } catch (error) {
        console.error('Error reading version info:', error.message);
        return {
            VERSION_TAG: '1.0.0',
            RELEASE_NAME: 'Iridium 1.0.0',
            TAG_NAME: 'r1',
            RELEASE_NUMBER: 1
        };
    }
}

if (require.main === module) {
    // Accept adofai version as first CLI arg
    const versionInfo = getVersionInfo(process.argv[2]);
    // Strip +adofai suffix for release metadata (used in release name/tag)
    const baseTag = versionInfo.VERSION_TAG.replace(/\+adofai.*$/, '');
    console.log(`VERSION_TAG=${versionInfo.VERSION_TAG}`);
    console.log(`RELEASE_NAME=${versionInfo.RELEASE_NAME}`);
    console.log(`TAG_NAME=${versionInfo.TAG_NAME}`);
    console.log(`BASE_TAG=${baseTag}`);
}

module.exports = getVersionInfo;