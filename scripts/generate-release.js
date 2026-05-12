const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

function getVersionInfo() {
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
        // 获取所有 tag，按版本排序
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
            // 获取从上一个 tag 到 HEAD 的所有 commit
            logCommand = `git log ${lastTag}..HEAD --oneline`;
        } else {
            // 如果没有上一个 tag，获取所有 commit
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
        // 移除注释行
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
    
    // 如果没有 CHANGELOG，认为没有变化
    if (!current) return false;
    
    // 如果没有 backup，认为有变化
    if (!backup) return true;
    
    // 比较内容（忽略空白差异）
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
    
    // 检查并添加 CHANGELOG
    if (includeChangelog && hasChangelogChanged()) {
        const changelog = getChangelog();
        if (changelog) {
            changelogSection = `#### 更新日志 / Changelog

${changelog}

---`;
        }
    }
    
    // 添加 commit 历史
    if (includeCommits) {
        const commits = getCommitLogSinceLastRelease();
        if (commits && commits.length > 0) {
            const commitList = commits.map(c => `- \`${c.hash}\` ${c.message}`).join('\n');
            const lastTag = getLastReleaseTag();
            const commitRange = lastTag ? `(${lastTag}...HEAD)` : '(all commits)';
            
            commitSection = `#### 提交历史 / Commits ${commitRange}

${commitList}`;
        }
    }
    
    const body = `## Iridium Mod Release

${changelogSection}

### 中文版本说明

**版本:** ${versionTag}
**提交:** ${commitSha}
**构建日期:** ${buildDate}

${commitSection}

#### 安装方法

### English Release Notes

**Version:** ${versionTag}
**Commit:** ${commitSha}
**Build Date:** ${buildDate}

${commitSection}

#### Installation

1. Download the attached zip file
2. Extract to your A Dance of Fire and Ice Mods folder
3. Launch the game and enjoy!

---

This release was automatically built by GitHub Actions.
此版本由 GitHub Actions 自动构建。`;
    
    return body;
}

// 如果是直接执行，输出版本信息
if (require.main === module) {
    const versionInfo = getVersionInfo();
    console.log(`VERSION_TAG=${versionInfo.VERSION_TAG}`);
    console.log(`RELEASE_NAME=${versionInfo.RELEASE_NAME}`);
    console.log(`TAG_NAME=${versionInfo.TAG_NAME}`);
}

module.exports = { 
    getVersionInfo, 
    getCommitLog, 
    getCommitLogSinceLastRelease,
    getChangelog,
    getChangelogBackup,
    hasChangelogChanged,
    generateReleaseBody 
};