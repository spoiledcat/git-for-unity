import * as tar from 'tar';
import * as asyncfile from 'async-file';
import { Readable } from 'stream';
import * as yaml from 'js-yaml';
import * as fs from 'fs';
import { ReplaySubject, Observable, Observer, Subject } from 'rxjs';
import { toArray, pluck, map, retryWhen } from 'rxjs/operators';
import * as p from 'path';
import * as sizeOf from "image-size";
import { execSync } from 'child_process';
import { readdir, Ignores, FileEntry } from './RecursiveReaddir';
import { copyFile, generateThumbnail, tmpDir } from './helpers';
import { readAllLines } from './read-lines';
import md5 = require('md5');

async function jsonHandler (relativeBasePath: string, sourceFile: string, targetdir: string, targetFile: string, version: string) {
    if (p.basename(sourceFile) !== 'package.json' || relativeBasePath === '') return false;

    const json = JSON.parse(await asyncfile.readTextFile(sourceFile));
    // set package.json version to equal the passed in version
    json['version'] = version;

    // adjust the version of dependencies to match what's in the filesystem, if there is such a thing
    let deps = json['dependencies'];
    for (let k in deps) {
        let dep = deps[k];
        if (dep.startsWith('file:')) {
            const dependencyJsonFile = p.join(p.dirname(sourceFile), (p.join(dep.substr(5), 'package.json')));
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

async function pngHandler (relativeBasePath: string, sourceFile: string, targetdir: string, targetFile: string, version: string) {
    const targetPreview = p.join(targetdir, 'preview.png');
    const size = sizeOf(sourceFile);
    await asyncfile.mkdirp(p.dirname(targetPreview));
    if (size.width > 128) {			
        // console.log(`Generating ${targetPreview} from ${sourceFile}`);
        await generateThumbnail(sourceFile).then(b => asyncfile.createWriteStream(targetPreview).write(b));
    } else {
        // console.log(`Copying ${sourceFile} to ${targetPreview}`);
        await copyFile(sourceFile, targetPreview);
    }
    // let the default handling code copy the original asset
    return false;
}

export class FileTreeWalker {
	private obs: Observable<FileEntry>;
	private _listener = new ReplaySubject<FileEntry>();
	private running = false;

	public constructor(public path: string, private ignores?: Ignores) {
		this.obs = new Observable((observer: Observer<FileEntry>) => {
			readdir(this.path, ignores || [], (error, file) => {
				if (error) {
					observer.error(error);
				} else if (file) {
					observer.next(file);
				} else {
					observer.complete();
				}
				return false;
			});
		});
	}

	public static walk(path: string, ignores?: Ignores) {
		return new FileTreeWalker(path, ignores).walk();
	}

	public static copy = (from: string, to: string, ignores?: Ignores) => {
		return new FileTreeWalker(from, ignores).copy(to);
	}

	private get listener() {
		if (!this.running) {
			this.obs.subscribe(val => { this._listener.next(val); }, err => this._listener.error(err), () => { this._listener.complete() });
			this.running = true;
		}
		return this._listener.asObservable();
	}

	public static getTempDir = () => tmpDir({ prefix: 'packaging-', unsafeCleanup: true, keep: true });

	public walk = () => {
		return this.listener;
	}

	public copy = (to: string) => {
		const ret = new Subject();

		this.listener.subscribe(async entry => {
			if (entry.isDir) {
				const relativeSourceDir = p.relative(this.path, entry.file);
				const targetDir = p.join(to, relativeSourceDir);
				await asyncfile.mkdirp(targetDir);
			} else {
				const relativeSourceDir = p.relative(this.path, p.dirname(entry.file));
				const targetDir = p.join(to, relativeSourceDir);
				const targetFilePath = p.join(targetDir, p.basename(entry.file));
				fs.copyFileSync(entry.file, targetFilePath);
			}
		}, err => ret.error(err), () => ret.complete());

		ret.subscribe();
		return ret.toPromise();
	}
}

const assetStorePackagingHandlers : {[key: string] : (relativeBasePath: string, sourceFile: string, targetdir: string, targetFile: string, version: string) => Promise<boolean>} = {
    ['.png']: pngHandler,
    ['.json']: jsonHandler
};

const packmanPackagingHandlers : {[key: string] : (relativeBasePath: string, sourceFile: string, targetdir: string, targetFile: string, version: string) => Promise<boolean>} = {
    ['.json']: jsonHandler
};

async function packageFileForAssetStore(extension: string, relativeBasePath: string, file: string, targetdir: string, targetasset: string, version: string) {
    let handled = false;
    if (assetStorePackagingHandlers[extension])
        handled = await assetStorePackagingHandlers[extension](relativeBasePath, file, targetdir, targetasset, version);
    if (!handled) {
        await asyncfile.mkdirp(p.dirname(targetasset));
        await copyFile(file, targetasset);
    }
    return targetasset;
}

async function packageFileForPackman(extension: string, relativeBasePath: string, file: string, targetdir: string, targetasset: string, version: string) {
    let handled = false;
    if (packmanPackagingHandlers[extension])
        handled = await packmanPackagingHandlers[extension](relativeBasePath, file, targetdir, targetasset, version);
    if (!handled) {
        //console.log(`Also copying ${file} to ${targetasset}`);
        await asyncfile.mkdirp(p.dirname(targetasset));
        await copyFile(file, targetasset);
    }
    return targetasset;
}

async function packageFile(sourcePath: string, baseInstallationPath: string, outputPath: string, f: FileEntry, version: string) {
    if (p.extname(f.file) !== '.meta') return {};

    const thefile = f.file.substr(0, f.file.lastIndexOf('.'));
    const metafile = f.file;

    if (!(await asyncfile.exists(thefile))) {
        return {};
    }

    const isDir = (await asyncfile.stat(thefile)).isDirectory();
    const installationPath = p.join(baseInstallationPath, p.relative(sourcePath, thefile));

    const meta = await asyncfile.readTextFile(metafile);

    const yamlmeta: { guid: string } = yaml.safeLoad(meta, { json: true });
    const targetdir = p.join(outputPath, yamlmeta.guid);
    const targetmeta = p.join(targetdir, 'asset.meta');
    const targetname = p.join(targetdir, 'pathname');
    const targetasset = p.join(targetdir, 'asset');

    await asyncfile.mkdirp(targetdir);
    await asyncfile.writeTextFile(targetname, installationPath.replace(/\\/g, '/'));

    await copyFile(metafile, targetmeta);
    if (!isDir) {
        await packageFileForAssetStore(p.extname(thefile), p.dirname(p.relative(sourcePath, thefile)), thefile, targetdir, targetasset, version);
    }

    return { thefile, targetasset };
}

export async function createPackmanPackageTree(walker: FileTreeWalker, outputPath: string, version: string)
{
    let promises : Promise<void>[] = [];
    await walker.walk().forEach(f => promises.push(createPackageTree(walker.path, outputPath, f, version)));
    await Promise.all(promises);
}

async function createPackageTree(sourcePath: string, outputPath: string, f: FileEntry, version: string) {
    if (f.isDir) return;

    if (p.extname(f.file) === '.meta') {

        const thefile = f.file.substr(0, f.file.lastIndexOf('.'));
    
        const thefileExists = await asyncfile.exists(thefile);
        if (!thefileExists) {
            return;
        }

        const isDir = (await asyncfile.stat(thefile)).isDirectory();
        const metafile = f.file;
        const targetdir = p.join(outputPath, "package", p.relative(sourcePath, p.dirname(thefile)));
        const targetmeta = p.join(targetdir, p.basename(metafile));

        // console.log(`Creating ${targetdir} and copying ${metafile} to ${targetmeta}`);

        await asyncfile.mkdirp(targetdir);
        await copyFile(metafile, targetmeta);
        if (isDir) {
            // console.log(`\tAlso creating ${p.join(targetdir, p.basename(thefile))}`);
            await asyncfile.mkdirp(p.join(targetdir, p.basename(thefile)));
        }
        else {
            const targetFile = p.join(targetdir, p.basename(thefile));
            await packageFileForPackman(p.extname(thefile), p.dirname(p.relative(sourcePath, thefile)), thefile, targetdir, targetFile, version);
        }
    } else if (!(await asyncfile.exists(f.file + '.meta'))) {
        const thefile = f.file;
        const targetdir = p.join(outputPath, "package", p.relative(sourcePath, p.dirname(thefile)));
        const targetFile = p.join(targetdir, p.basename(thefile));
        await packageFileForPackman(p.extname(thefile), p.dirname(p.relative(sourcePath, thefile)), thefile, targetdir, targetFile, version);
    }
}


export async function createTar(walker: FileTreeWalker): Promise<Readable> {
    const list = await walker.walk().pipe(
        pluck('file'),
        map(x => p.relative(walker.path, x)),
        toArray())
        .toPromise();

    console.log(`Tar-ing ${walker.path}...`);
    return tar.create({
        gzip: true,
        cwd: walker.path,
        noDirRecurse: true,
    },
        list
    )
}

/**
 * 
 * @param outputPath Directory to copy the files into
 * @param baseInstallationPath Base installation path for the unity package, recorded in the meta files (i.e., "Assets/Plugins" or "Packages/my.package")
 * @param version Version of the package, to be set in the package.json, if there is such a file.
 * @returns The list of package.json files they were found
 */
export async function createAssetStorePackageTree(walker: FileTreeWalker, outputPath: string, baseInstallationPath: string, version: string)
 : Promise<string | undefined>
{
    console.log(`Packaging ${walker.path} to be installed into ${baseInstallationPath}`);

    let packageJson : string | undefined = undefined;
    let promises : Promise<void>[] = [];
    await walker.walk().forEach(f => {
        promises.push(packageFile(walker.path, baseInstallationPath, outputPath, f, version)
            .then(async entry => {
                if (entry.thefile && entry.targetasset && p.basename(entry.thefile) === 'package.json' && p.dirname(p.relative(walker.path, entry.thefile)) === '') {
                    const contents = await asyncfile.readTextFile(entry.targetasset);
                    const filename = p.relative(walker.path, entry.thefile);
                    packageJson = contents;
                }
            }));
    });
    await Promise.all(promises);
    return packageJson;
}

export async function prepare(packageName: string, version: string, targetPath: string, sourcePath: string,
	baseInstallationPath: string, extrasPath: string | undefined,
	ignoreFile: string | undefined, isUnityProject: boolean) {

	let ignores: Ignores = [];

	if (ignoreFile) {
		if (!await asyncfile.exists(ignoreFile)) {
            throw new Error;
		} else {
			const ignoreData = await asyncfile.readTextFile(ignoreFile);
			ignores = await readAllLines(ignoreData);
		}
	}

	if (isUnityProject) {
		ignores = [ ...ignores, (file: FileEntry) => !(p.relative(sourcePath, file.file).startsWith("Assets")) ]
	}

	if (!await asyncfile.exists(targetPath)) {
		await asyncfile.mkdirp(targetPath);
	}

	if (extrasPath) {
		let combinedSourcesPath = await FileTreeWalker.getTempDir();
		await FileTreeWalker.copy(sourcePath, combinedSourcesPath);
		await FileTreeWalker.copy(extrasPath, combinedSourcesPath);
		sourcePath = combinedSourcesPath;
	}

	const sourceWalker = new FileTreeWalker(sourcePath, ignores);

	const tmpAssetStorePackagePath = await FileTreeWalker.getTempDir();
	const packageJson = await createAssetStorePackageTree(sourceWalker, tmpAssetStorePackagePath, baseInstallationPath, version);

	const tmpPackmanPackagePath = await FileTreeWalker.getTempDir();

	if (!isUnityProject) {
		await createPackmanPackageTree(sourceWalker, tmpPackmanPackagePath, version);
	}

	return { packageJson, assetStorePath: tmpAssetStorePackagePath, packmanPackagePath: tmpPackmanPackagePath };

}

export async function packageThings(packageName: string, version: string, targetPath: string,
	packageJson: string | undefined, tmpAssetStorePackagePath: string, tmpPackmanPackagePath: string) {

	
	const assetStoreZipPath: string = p.join(targetPath, `${packageName}-${version}.unitypackage`);
	const assetStoreZipMd5Path: string = p.join(targetPath, `${packageName}-${version}.unitypackage.md5`);
	const packmanPackageZipPath: string = p.join(targetPath, `${packageName}-${version}.tgz`);
	const packmanPackageZipMd5Path: string = p.join(targetPath, `${packageName}-${version}.tgz.md5`);
	const packmanPackageManifest: string = p.join(targetPath, `packages.json`);

	let packageManifest: { [key: string]: string } | undefined = undefined;
	if (packageJson) {
		packageManifest = {
			[p.basename(packmanPackageZipPath)]: JSON.parse(packageJson)
		};
	}
	
	await asyncfile
		.mkdirp(p.dirname(assetStoreZipPath))
		.then(() => createTar(new FileTreeWalker(tmpAssetStorePackagePath)))
		.then(tar => tar.pipe(asyncfile.createWriteStream(assetStoreZipPath)).on('finish', async () =>
			{
				console.log(`Finalizing ${assetStoreZipPath}...`)
				const hash = md5(await asyncfile.readFile(assetStoreZipPath));
				await asyncfile.writeTextFile(assetStoreZipMd5Path, hash);
				console.log(`${assetStoreZipPath} and ${assetStoreZipMd5Path} created`);
			})
	);

	await asyncfile
		.mkdirp(p.dirname(packmanPackageZipPath))
		.then(() => createTar(new FileTreeWalker(tmpPackmanPackagePath)))
		.then(tar => tar.pipe(asyncfile.createWriteStream(packmanPackageZipPath)).on('finish', async () =>
			{
				console.log(`Finalizing ${packmanPackageZipPath}...`)
				const hash = md5(await asyncfile.readFile(packmanPackageZipPath));
				await asyncfile.writeTextFile(packmanPackageZipMd5Path, hash);
				console.log(`${packmanPackageZipPath}, ${packmanPackageZipMd5Path} created`);

				if (packageManifest) {
					await asyncfile.writeTextFile(packmanPackageManifest, JSON.stringify(packageManifest));
					console.log(`${packmanPackageManifest} created`);
				}
			})
	);

	return { assetStorePath: tmpAssetStorePackagePath, packmanPackagePath: tmpPackmanPackagePath, packmanPackageManifest };
}