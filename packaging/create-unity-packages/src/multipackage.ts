#!/usr/bin/env node_modules/.bin/ts-node

import * as tar from 'tar';
import * as asyncfile from 'async-file';
import * as md5 from 'md5';
import * as commandLineArgs from 'command-line-args';
import * as commandLineUsage from 'command-line-usage';
import { Readable } from 'stream';
import * as yaml from 'js-yaml';
import * as fs from 'fs';
import { ReplaySubject, defer, pipe, Observable, Observer, TeardownLogic, Subject, generate, of, from } from 'rxjs';
import { toArray, take, pluck, map, filter, first, takeWhile, skipWhile } from 'rxjs/operators';
import * as p from 'path';
import * as sizeOf from "image-size";
import { exec, execSync } from 'child_process';
import { readdir, Ignores, FileEntry } from './RecursiveReaddir';
import { readAllLines, readLines, readLinesFromFile } from './read-lines';
import { copyFile, generateThumbnail, tmpDir } from './helpers';

class TreeWalker {
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
		return new TreeWalker(path, ignores).walk();
	}

	public static copy = (from: string, to: string, ignores?: Ignores) => {
		return new TreeWalker(from, ignores).copy(to);
	}

	private get listener() {
		if (!this.running) {
			this.obs.subscribe(val => { this._listener.next(val); }, err => this._listener.error(err), () => { this._listener.complete() });
			this.running = true;
		}
		return this._listener.asObservable();
	}

	public static getTempDir = () => tmpDir({ prefix: 'packaging-', unsafeCleanup: true });

	private walk = () => {
		return this.listener;
	}

	private copy = (to: string) => {
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

	public async preparePackage(packageName: string, version: string) {

		const tmpAssetStorePackage = await TreeWalker.getTempDir();
		const tmpUnityPackage = await TreeWalker.getTempDir();

		console.log(`Packaging ${this.path} ${packageName}`);

		let packageJson = '';

		let promises : Promise<void>[] = [];
		await this.walk().forEach(f => {
			promises.push(this.packageFile(packageName, tmpAssetStorePackage, f, version)
				.then(async entry => {
					if (entry.thefile && entry.targetasset && p.basename(entry.thefile) === 'package.json') {
						packageJson = await asyncfile.readTextFile(entry.targetasset);
					}
				}));
		});
		await Promise.all(promises);

		promises = [];
		await this.walk().forEach(f => promises.push(this.createPackageTree(tmpUnityPackage, f)
			.then(async entry => {
				if (entry.thefile && entry.targetasset && p.basename(entry.thefile) === 'package.json') {
					await asyncfile.writeFile(entry.targetasset, packageJson);
				}
			})));

		await Promise.all(promises);
		return { assetStorePackage: tmpAssetStorePackage, unityPackage: tmpUnityPackage, packageJson };
	}

	private async createPackageTree(tmp: string, f: FileEntry) {
		if (f.isDir) return {};

		const thefile = f.file.substr(0, f.file.lastIndexOf('.'));
		const isDir = (await asyncfile.stat(thefile)).isDirectory();
		
		if (!(await asyncfile.exists(thefile))) {
			return {};
		}

		// if (!isDir)
		// 	console.log(`Packaging ${p.relative(this.path, thefile)}`);

		const metafile = f.file;
		const targetdir = p.join(tmp, "package", p.relative(this.path, isDir ? thefile : p.dirname(thefile)));
		const targetmeta = p.join(targetdir, p.basename(metafile));
		const targetasset = p.join(targetdir, p.basename(thefile));

		await asyncfile.mkdirp(targetdir);
		await copyFile(metafile, targetmeta);
		if (isDir)
			await asyncfile.mkdirp(thefile);
		else
			await copyFile(thefile, targetasset);

		return { thefile, targetasset };
	}

	private async packageFile(packageName: string, tmp: string, f: FileEntry, version: string) {
		if (f.isDir) return {};

		const thefile = f.file.substr(0, f.file.lastIndexOf('.'));
		const isDir = (await asyncfile.stat(thefile)).isDirectory();
		const metafile = f.file;
		const installationPath = p.join("Packages", packageName, p.relative(this.path, thefile));

		if (!(await asyncfile.exists(thefile))) {
			return {};
		}

		// if (!isDir)
		// 	console.log(`Packaging ${p.relative(this.path, thefile)}`);

		const meta = await asyncfile.readTextFile(metafile);

		const yamlmeta: { guid: string } = yaml.safeLoad(meta, { json: true });
		const targetdir = p.join(tmp, yamlmeta.guid);
		const targetmeta = p.join(targetdir, 'asset.meta');
		const targetname = p.join(targetdir, 'pathname');
		const targetasset = p.join(targetdir, 'asset');

		await asyncfile.mkdirp(targetdir);
		await asyncfile.writeTextFile(targetname, installationPath.replace(/\\/g, '/'));

		await copyFile(metafile, targetmeta);
		if (!isDir) {
			await this.packageFileByType(p.extname(thefile), thefile, targetdir, targetasset, version);
		}

		return { thefile, targetasset };
	}

	public async createTar(): Promise<Readable> {
		const list = await this.walk().pipe(
			pluck('file'),
			map(x => p.relative(this.path, x)),
			toArray())
			.toPromise();

		console.log(`Tar-ing ${this.path}...`);
		return tar.create({
			gzip: true,
			cwd: this.path,
			noDirRecurse: true,
		},
			list
		)
	}

	private static handlers : {[key: string] : (file: string, targetdir: string, targetasset: string, version: string) => Promise<boolean>} = {
		async ['.png'](file: string, targetdir: string, version: string) {
			const targetpreview = p.join(targetdir, 'preview.png');
			const size = sizeOf(file);
			if (size.width > 128) {			
				await generateThumbnail(file).then(b => asyncfile.createWriteStream(targetpreview).write(b));
			} else {
				await copyFile(file, targetpreview);
			}
			// let the default handling code copy the original asset
			return false;
		},
		async ['.json'](file: string, targetdir: string, targetasset: string, version: string) {
			if (p.basename(file) !== 'package.json') return false;

			const json = JSON.parse(await asyncfile.readTextFile(file));
			json['version'] = version;
			let deps = json['dependencies'];
			for (let k in deps) {
				let dep = deps[k];
				if (dep.startsWith('file:')) {
					const dependencyJsonFile = p.join(p.dirname(file), (p.join(dep.substr(5), 'package.json')));
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

			const assetContent = JSON.stringify(json);
			await asyncfile.writeTextFile(targetasset, assetContent)
			return true;
		}
	};

	private async packageFileByType(extension: string, file: string, targetdir: string, targetasset: string, version: string) {
		let handled = false;
		if (TreeWalker.handlers[extension])
			handled = await TreeWalker.handlers[extension](file, targetdir, targetasset, version);
		if (!handled) {
			await copyFile(file, targetasset);
		}
		return targetasset;
	}
}

const optionDefinitions = [
	{
		name: 'help',
		alias: 'h',
		description: 'Display this usage guide.',
	},
	{
		name: 'path1',
		typeLabel: '{underline directory}',
		description: 'Path to the source assets to be packaged',
	},
	{
		name: 'path2',
		typeLabel: '{underline directory}',
		description: 'Path to the source assets to be packaged',
	},
	{
		name: 'extras1',
		typeLabel: '{underline directory}',
		description: 'Path to extra files to also be packages',
	},
	{
		name: 'extras2',
		typeLabel: '{underline directory}',
		description: 'Path to extra files to also be packages',
	},
	{
		name: 'name',
		typeLabel: '{underline name}',
		description: 'Name of the package',
	},
	{
		name: 'version',
		typeLabel: '{underline version}',
		description: 'Version of the package',
	},
	{
		name: 'out',
		typeLabel: '{underline directory}',
		description: 'Where to save the zip and md5 files',
	},
	{
		name: 'ignores1',
		description: 'Path to file with globs of things to ignore, like a .gitignore or an .npmignore',
	},
	{
		name: 'ignores2',
		description: 'Path to file with globs of things to ignore, like a .gitignore or an .npmignore',
	},
];

const sections = [
	{
		header: 'Unity packager',
		content: 'Takes a Unity (packman) package source tree, and packages it into a .unitypackage and a packman zip package, with manifest',
	},
	{
		header: 'Options',
		optionList: optionDefinitions,
	},
];

const options = commandLineArgs(optionDefinitions);

async function doPackage(path: any, ignoreFile: any, extras: any, targetPath: string, version: string, suffix: string) {
	let sourcePath: string = p.resolve(path);
    const extrasPath: string | undefined = extras ? p.resolve(extras) : undefined;
    const packageName = p.basename(sourcePath);

	let ignores: string[] = [];

	if (ignoreFile) {
		if (!await asyncfile.exists(ignoreFile)) {
			console.error(`Cannot find ignore file at ${ignoreFile}`);
			console.error(commandLineUsage(sections));
			process.exit(-1);
		} else {
			const ignoreData = await asyncfile.readTextFile(ignoreFile);
			ignores = await readAllLines(ignoreData);
		}
	}

	if (!await asyncfile.exists(targetPath)) {
		await asyncfile.mkdirp(targetPath);
	}

	if (extrasPath) {
		let combinedSourcesPath = await TreeWalker.getTempDir();
		await TreeWalker.copy(sourcePath, combinedSourcesPath);
		await TreeWalker.copy(extrasPath, combinedSourcesPath);
		sourcePath = combinedSourcesPath;
	}

	const { assetStorePackage, unityPackage, packageJson } = await new TreeWalker(sourcePath, ignores).preparePackage(packageName, version);

    return { targetPath, packageJson, assetStorePackage, unityPackage };
}

(async () => {

	if (!options.path1
        || !(await asyncfile.exists(options.path1))
        || !options.path2
		|| !(await asyncfile.exists(options.path2))
		|| !options.name
		|| !options.out
	) {
		console.error(commandLineUsage(sections));
		process.exit(-1);
	}

	const packageName: string = options.name;
	const version: string = options.version || "0.0.0";
    const suffix: string = options.suffix || "";
    const targetPath: string = p.resolve(options.out);
    const assetStoreZipPath: string = p.join(targetPath, `${packageName}-${version}.unitypackage`);
	const assetStoreZipMd5Path: string = p.join(targetPath, `${packageName}-${version}.unitypackage.md5`);
	const unityPackageZipPath: string = p.join(targetPath, `${packageName}-${version}.tgz`);
	const unityPackageZipMd5Path: string = p.join(targetPath, `${packageName}-${version}.tgz.md5`);
	const unityPackageManifest: string = p.join(targetPath, `packages.json`);

    let packageJson1 : string;
    let assetStorePackage1 : string;
    let unityPackage1 : string;
    let packageJson2 : string;
    let assetStorePackage2 : string;
    let unityPackage2 : string;

    {
        let { packageJson, assetStorePackage, unityPackage } = await doPackage(options.path1, options.ignores1, options.extras1, targetPath, version, suffix);
        packageJson1 = packageJson;
        assetStorePackage1 = assetStorePackage;
        unityPackage1 = unityPackage;
    }

    {
        let { packageJson, assetStorePackage, unityPackage } = await doPackage(options.path2, options.ignores2, options.extras2, targetPath, version, suffix);
        packageJson2 = packageJson;
        assetStorePackage2 = assetStorePackage;
        unityPackage2 = unityPackage;
    }

    const combined = await TreeWalker.getTempDir();
    
    await TreeWalker.copy(assetStorePackage1, combined);
    await TreeWalker.copy(assetStorePackage2, combined);

	await asyncfile
		.mkdirp(p.dirname(assetStoreZipPath))
		.then(() => new TreeWalker(combined).createTar())
		.then(tar => tar.pipe(asyncfile.createWriteStream(assetStoreZipPath)).on('finish', async () =>
			{
				console.log(`Finalizing ${assetStoreZipPath}...`)
				const hash = md5(await asyncfile.readFile(assetStoreZipPath));
				await asyncfile.writeTextFile(assetStoreZipMd5Path, hash);
				console.log(`${assetStoreZipPath} and ${assetStoreZipMd5Path} created`);
			})
	);

	// await asyncfile
	// 	.mkdirp(p.dirname(unityPackageZipPath))
	// 	.then(() => new TreeWalker(unityPackage).createTar())
	// 	.then(tar => tar.pipe(asyncfile.createWriteStream(unityPackageZipPath)).on('finish', async () =>
	// 		{
	// 			console.log(`Finalizing ${unityPackageZipPath}...`)
	// 			const hash = md5(await asyncfile.readFile(unityPackageZipPath));
	// 			await asyncfile.writeTextFile(unityPackageZipMd5Path, hash);
	// 			await asyncfile.writeTextFile(unityPackageManifest, JSON.stringify(packageManifest));
	// 			console.log(`${unityPackageZipPath}, ${unityPackageZipMd5Path}, ${unityPackageManifest} created`);
	// 		})
    //);


})();
