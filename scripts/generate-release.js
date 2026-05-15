const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

/**
 * @param {string} [projectDir] - Project directory relative to repo root, e.g. "main" or "frontline".
 *                                 If omitted, defaults to "main".
 */
function getVersionInfo(projectDir) {
    try {
        projectDir = projectDir || 'main';

        const infoPath = path.join(__dirname, '..', projectDir, 'Info.json');
        const infoContent = fs.readFileSync(infoPath, 'utf8');
        const info = JSON.parse(infoContent);

        const baseVersion = info.Version || '1.0.0';
        const displayName = info.DisplayName || 'Iridium';

        const vmPath = path.join(__dirname, '..', projectDir, 'VersionManager.cs');
        const vmContent = fs.readFileSync(vmPath, 'utf8');

        const typeMatch = vmContent.match(/public\s+static\s+VersionType\s+Type\s*=>\s*VersionType\.(\w+)\s*;/);
        const minorMatch = vmContent.match(/public\s+const\s+int\s+MinorVersion\s*=\s*(\d+)\s*;/);

        const vtype = typeMatch ? typeMatch[1].toLowerCase() : 'release';
        const minor = minorMatch ? minorMatch[1] : '0';

        let versionTag;
        let releaseName;
        let tagName;

        if (vtype === 'release') {
            versionTag = baseVersion;
            releaseName = `${displayName} ${baseVersion}`;
            tagName = `v${baseVersion}`;
        } else {
            versionTag = `${baseVersion}-${vtype}${minor}`;
            releaseName = `${displayName} ${baseVersion} ${vtype}${minor}`;
            tagName = `v${baseVersion}-${vtype}${minor}`;
        }

        return {
            VERSION_TAG: versionTag,
            RELEASE_NAME: releaseName,
            TAG_NAME: tagName
        };

    } catch (error) {
        console.error('Error reading version info:', error.message);
        return {
            VERSION_TAG: '1.0.0',
            RELEASE_NAME: 'Iridium 1.0.0',
            TAG_NAME: 'v1.0.0'
        };
    }
}

/**
 * 获取上一个 release 的 tag
 */
function getLastReleaseTag() {
    try {
        const tags = execSync('git tag --sort=-version:refname', { encoding: 'utf8' }).trim();
        if (!tags) return null;
        
        const tagList = tags.split('\n').filter(t => t.startsWith('v'));
        return tagList.length > 0 ? tagList[0] : null;
    } catch (error) {
        return null;
    }
}

/**
 * 获取从上一个 release 到现在的所有 commit
 */
function getCommitLogSinceLastRelease() {
    try {
        const lastTag = getLastReleaseTag();
        let logCommand;
        
        if (lastTag) {
            logCommand = `git log ${lastTag}..HEAD --oneline`;
        } else {
            logCommand = 'git log --oneline';
        }
        
        const log = execSync(logCommand, { encoding: 'utf8' }).trim();
        if (!log) return [];
        
        const commits = log.split('\n').map(line => {
            const [hash, ...rest] = line.split(' ');
            return {
                hash: hash,
                message: rest.join(' ')
            };
        });
        return commits;
    } catch (error) {
        console.error('Error reading commit log:', error.message);
        return [];
    }
}

/**
 * 获取最近的 commit（fallback）
 */
function getCommitLog(limit = 20) {
    try {
        const log = execSync(`git log --oneline -${limit}`, { encoding: 'utf8' }).trim();
        const commits = log.split('\n').map(line => {
            const [hash, ...rest] = line.split(' ');
            return {
                hash: hash,
                message: rest.join(' ')
            };
        });
        return commits;
    } catch (error) {
        console.error('Error reading commit log:', error.message);
        return [];
    }
}

/**
 * 读取 CHANGELOG.md 内容
 */
function getChangelog() {
    try {
        const changelogPath = path.join(__dirname, '..', '.github', 'workflows', 'CHANGELOG.md');
        if (!fs.existsSync(changelogPath)) {
            return null;
        }
        
        const content = fs.readFileSync(changelogPath, 'utf8');
        const lines = content.split('\n');
        const cleanedLines = lines.filter(line => !line.trim().startsWith('<!--') && !line.trim().startsWith('-->'));
        const cleanedContent = cleanedLines.join('\n').trim();
        
        return cleanedContent || null;
    } catch (error) {
        return null;
    }
}

/**
 * 读取 CHANGELOG.md.backup 内容
 */
function getChangelogBackup() {
    try {
        const backupPath = path.join(__dirname, '..', '.github', 'workflows', 'CHANGELOG.md.backup');
        if (!fs.existsSync(backupPath)) {
            return null;
        }
        
        const content = fs.readFileSync(backupPath, 'utf8');
        const lines = content.split('\n');
        const cleanedLines = lines.filter(line => !line.trim().startsWith('<!--') && !line.trim().startsWith('-->'));
        const cleanedContent = cleanedLines.join('\n').trim();
        
        return cleanedContent || null;
    } catch (error) {
        return null;
    }
}

/**
 * 检查 CHANGELOG 是否有变化
 */
function hasChangelogChanged() {
    const current = getChangelog();
    const backup = getChangelogBackup();
    
    if (!current) return false;
    if (!backup) return true;
    
    return current.trim() !== backup.trim();
}

/**
 * 生成 Release Body
 */
function generateReleaseBody(versionTag, commitSha, options = {}) {
    const buildDate = new Date().toISOString().split('T')[0];
    const { includeChangelog = true, includeCommits = true } = options;
    
    let changelogSection = '';
    let commitSection = '';
    
    if (includeChangelog && hasChangelogChanged()) {
        const changelog = getChangelog();
        if (changelog) {
            changelogSection = `#### 更新日志 / Changelog

${changelog}

---`;
        }
    }
    
    if (includeCommits) {
        const commits = getCommitLogSinceLastRelease();
        if (commits.length > 0) {
            commitSection = `#### 提交记录 / Commits

| Hash | Message |
|------|---------|
${commits.map(c => `| \`${c.hash}\` | ${c.message} |`).join('\n')}

---`;
        }
    }
    
    const body = `## ${versionTag}

**构建日期 / Build Date**: ${buildDate}
**提交 / Commit**: \`${commitSha}\`

${changelogSection}
${commitSection}
> 💡 包含两个版本：\`Iridium_*+adofai2.9.8.zip\` (v2.9.8) 和 \`Iridium_*+adofai2.10.0.zip\` (v2.10.0)
`.trim();
    
    return body;
}

module.exports = {
    getVersionInfo,
    getLastReleaseTag,
    getCommitLogSinceLastRelease,
    getCommitLog,
    getChangelog,
    getChangelogBackup,
    hasChangelogChanged,
    generateReleaseBody
};