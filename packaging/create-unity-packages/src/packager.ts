import { FileTreeWalker } from "./TreeWalker";
import * as p from 'path';
import * as asyncfile from 'async-file';
import { copyFile } from './helpers';
import { Ignores } from "./RecursiveReaddir";
import { execSync } from "child_process";
import archiver = require("archiver");

export type PackageHandlerFunction = (baseSourcePath: string, sourceFile: string,
    baseTargetPath: string, targetFile: string,
    baseInstallationPath: string, version: string) => Promise<boolean>;

export type PackageHandler = { [key: string] : PackageHandlerFunction };

export enum PackageType {
    Source = "source",
    PackmanSource = "package",
    PackmanPackage = "upm",
    UnityPackage = "unity",
    UpmManifest = "upmmanifest",
    Manifest = "manifest"
}

export type PackageFile = { type: PackageType, path: string, md5Path?: string, md5Hash?: string };
export type PackageFileList = { [key: string] : PackageFile };

export async function createTar(sourcePath: string, archiveFile: string, relativeTargetPath?: string) {
    return new Promise<void>(async (resolve, reject) => {
        const arch = archiver('tar', { gzip: true }).directory(sourcePath + '/', relativeTargetPath || false);
        arch.pipe(asyncfile.createWriteStream(archiveFile))
            .on('close', () => resolve());
        arch.finalize();
    });
    //return archiver('tar', { gzip: true }).directory(walker.path, relativeTargetPath || false);
}

export async function createZip(sourcePath: string, archiveFile: string, relativeTargetPath?: string) {
    // console.log(`Zipping ${sourcePath + '/'} with relative ${relativeTargetPath || false}`);
    //return archiver('zip').directory(sourcePath + '/', relativeTargetPath || false);
    return new Promise<void>(async (resolve, reject) => {
        const arch = archiver('zip').directory(sourcePath + '/', relativeTargetPath || false);
        arch.pipe(asyncfile.createWriteStream(archiveFile))
            .on('close', () => resolve());
        arch.finalize();
    });
}

const defaultHandlers : PackageHandler = {
    ['.json']: jsonHandler
};

export abstract class Packager {
    protected get DefaultHandlers(): PackageHandler { return defaultHandlers; }
    
    private fileHandlers: PackageHandler = {};
    protected get Handlers(): PackageHandler {
        return this.fileHandlers;
    }
    protected set Handlers(value: PackageHandler) {
        this.fileHandlers = value;
    }

    protected constructor() {
        this.Handlers = this.DefaultHandlers;
    }

    public async prepareSource(sourcePath: string, outputPath: string, baseInstallationPath?: string) {
    }

    /**
     * @param sourcePath Source directory to copy files from
     * @param outputPath Directory to copy the files into
     * @param ignores: Entries to skip when walking the source tree
     * @param version Version of the package, to be set in the package.json, if there is such a file. package.json will also include information about the commit and repo
     * @returns The processed package.json file, if there was one
     */
    public async prepare(sourcePath: string, version: string, ignores: Ignores, outputPath: string, baseInstallationPath?: string) : Promise<string | undefined> {
        let promises : Promise<void>[] = [];
        let handlers = this.Handlers;
        await FileTreeWalker.walk(sourcePath, ignores).forEach(f => promises.push(Packager.runPackageHandler(handlers, sourcePath, f.file, outputPath, p.relative(sourcePath, f.file), baseInstallationPath || '', version)));
        await Promise.all(promises);
    
        const packageJson = p.join(outputPath, 'package.json');
        if ((await asyncfile.exists(packageJson)))
            return packageJson;
        return undefined
    }

    public async package(sourcePath: string, targetPath: string, packageName: string, version: string) : Promise<PackageFile[]>{
        return [];
    }

    protected static async runPackageHandler(handlers: PackageHandler,
        baseSourcePath: string, sourceFile: string,
        baseTargetPath: string, targetFile: string,
        baseInstallationPath: string, version: string) {
        let handled = false;

        const isDir = (await asyncfile.stat(sourceFile)).isDirectory();
        const extension = isDir ? '' : p.extname(sourceFile);

        let target = targetFile || p.relative(baseSourcePath, sourceFile);
        if (!p.isAbsolute(target))
            target = p.join(baseTargetPath, target);

        if (handlers[extension])
            handled = await handlers[extension](baseSourcePath, sourceFile, baseTargetPath, target, baseInstallationPath, version);

        if (!handled) {
            // only create directories if there's a meta file for them or if they have content
            if (isDir) {
                if ((await asyncfile.readdir(sourceFile)).length == 0 && !(await asyncfile.exists(sourceFile + ".meta")))
                    return;
                await asyncfile.mkdirp(target);
            } else {
                await asyncfile.mkdirp(p.dirname(target));
                await copyFile(sourceFile, target);
            }
        }
    }
}

async function jsonHandler (baseSourcePath: string, sourceFile: string,
    baseTargetPath: string, targetFile: string,
    baseInstallationPath: string, version: string) : Promise<boolean>
{
    if (p.basename(sourceFile) !== 'package.json') return false;

    const relativeSourcePath = p.relative(baseSourcePath, sourceFile);

    const json = JSON.parse(await asyncfile.readTextFile(sourceFile));
    // set package.json version to equal the passed in version
    json['version'] = version;

    // adjust the version of dependencies to match what's in the filesystem, if there is such a thing
    let deps = json['dependencies'];
    for (let k in deps) {
        let dep = deps[k];
        if (dep.startsWith('file:')) {
            const dependencyJsonFile = p.join(baseTargetPath, p.dirname(relativeSourcePath), (p.join(dep.substr(5), 'package.json')));
            if (!await asyncfile.exists(dependencyJsonFile)) {
                dep = '';
            } else {
                const dependencyJson = JSON.parse(await asyncfile.readTextFile(dependencyJsonFile));
                const dependencyJsonVersion = `^${dependencyJson['version']}`;
                dep = dependencyJsonVersion;
            }
            deps[k] = dep;
        }
    }
    json['dependencies'] = deps;


    /* add entry like this
      "repository": {
            "type": "git",
            "url": "[url]",
            "revision": "[commit]"
          }
    */
    const remotes = execSync('git remote -v').toString().trim().split('\n').map(x => x.trim()).map(x => x.split('\t')).filter(x => x[1].endsWith('(fetch)'));
    if (remotes.length > 0) {
        const commit = execSync('git rev-parse HEAD').toString().trim();
        let origin = remotes.find(x => x[0] === 'origin');
        if (!origin)
            origin = remotes[0];
        const url = origin[1].split(' ')[0];
        json['repository'] = {
            type: 'git',
            url: url,
            revision: commit
        };
    }

    const contents = JSON.stringify(json);
    // console.log(`Writing ${targetFile}`);
    await asyncfile.writeTextFile(targetFile, contents)
    return true;
}
