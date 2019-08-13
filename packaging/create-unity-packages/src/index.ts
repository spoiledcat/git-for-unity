#!/usr/bin/env node_modules/.bin/ts-node

import * as asyncfile from 'async-file';
import * as commandLineArgs from 'command-line-args';
import * as commandLineUsage from 'command-line-usage';
import * as p from 'path';
import { readAllLines } from './read-lines';
import { FileTreeWalker } from './TreeWalker';
import { optionDefinitions, sections, ParsedOptions } from './cmdlineoptions';
import { PackageType, PackageFileList, PackageFile } from './packager';
import { UnityPackager } from './unityPackager';
import { PackmanPackager } from './packmanPackager';
import { UpmPackager } from './upmPackager';

async function validateOptionPath(options: commandLineArgs.CommandLineOptions, argName: string, optional: boolean = false) {

	if (optional)
	{
		if ((!options[argName] || options[argName] === '')) return undefined;
		return p.resolve(options[argName]);
	}

	validateOption(options, argName);

	if (!(await asyncfile.exists(options[argName]))) {
		console.error(`Bad parameter ${argName}: ${options[argName]} does not exist`);
		console.error(commandLineUsage(sections));
		process.exit(-1);
	}
	return p.resolve(options[argName]);
}

function validateOption(options: commandLineArgs.CommandLineOptions, argName: string, optional: boolean = false) {
	if (!options[argName] || options[argName] === '')
	{
		console.error(`Missing argument: ${argName}`);
		console.error(commandLineUsage(sections));
		process.exit(-1);
	}
	return options[argName] as string;
}

async function parseCommandLine() : Promise<ParsedOptions> {
	const options = commandLineArgs(optionDefinitions);
	const extras = await validateOptionPath(options, "extras", true);
	const ignoreFile = await validateOptionPath(options, "ignores", true);

	const parsed : ParsedOptions = {
		packageName: validateOption(options, "name"),
		sourcePath: (await validateOptionPath(options, "src"))!,
		targetPath: validateOption(options, "out"),
		version: validateOption(options, "version"),
		baseInstallationPath: validateOption(options, "installPath", true),
		skipPackaging: options.skip === true,
		ignores: [],
		doUnityPackage: !options.skipUnity,
		doPackmanPackage: !options.skipPackage,
		doUpmPackage: !options.skipUpm,
		tmpPath: (await validateOptionPath(options, 'tmp', true)),
		other: []
	};

	if (extras) {
		let combinedSourcesPath = await FileTreeWalker.getTempDir();
		await FileTreeWalker.copy(parsed.sourcePath, combinedSourcesPath);
		await FileTreeWalker.copy(extras, combinedSourcesPath);
		parsed.sourcePath = combinedSourcesPath;
	}

	if (ignoreFile) {
		const ignoreData = await asyncfile.readTextFile(ignoreFile);
		parsed.ignores = await readAllLines(ignoreData);
	}

	parsed.targetPath = p.resolve(parsed.targetPath);
	if (!await asyncfile.exists(parsed.targetPath)) {
		await asyncfile.mkdirp(parsed.targetPath);
	}

	return parsed;
}

(async () => {

	const parsed = await parseCommandLine();

	let tmpBuildDir = parsed.tmpPath || p.join(p.dirname(parsed.targetPath), 'obj');
	if (!p.isAbsolute(tmpBuildDir)) {
		tmpBuildDir = p.resolve(tmpBuildDir);
	}

	parsed.tmpPath = tmpBuildDir;

	console.log(parsed);

	const packmanPackager = new PackmanPackager();
	const upmPackager = new UpmPackager();
	const unityPackager = new UnityPackager();

	let packages: { [key: string] : any, manifest?: PackageFile } = {};
	const manifest: string = p.join(parsed.targetPath, `manifest.json`);
	if (await asyncfile.exists(manifest)) {
		packages = JSON.parse(await asyncfile.readTextFile(manifest));
	}

	if (parsed.doPackmanPackage || parsed.doUnityPackage) {
		const tmpPackmanSourceTree = p.join(parsed.skipPackaging ? parsed.targetPath : tmpBuildDir, 'package', parsed.packageName);

		// unity and package use the same source
		await packmanPackager.prepare(parsed.sourcePath, parsed.version, parsed.ignores, tmpPackmanSourceTree);

		if (parsed.doUnityPackage) {
			const tmpUnitySourceTree = parsed.skipPackaging ? p.join(parsed.targetPath, "unitypackage") : await FileTreeWalker.getTempDir();

			await unityPackager.prepareSource(tmpPackmanSourceTree, tmpUnitySourceTree, parsed.baseInstallationPath);

			if (parsed.skipPackaging) {
				packages[PackageType.Source].push({ type: PackageType.Source, path: tmpUnitySourceTree });
			} else {
				let files = await unityPackager.package(tmpUnitySourceTree, parsed.targetPath, parsed.packageName, parsed.version);
				files.map(x => {
					if (!packages[x.type]) packages[x.type] = [];
					packages[x.type].push(x);
				});
			}
		}

		if (parsed.doPackmanPackage) {
			if (parsed.skipPackaging) {
				packages[PackageType.Source].push({ type: PackageType.Source, path: tmpPackmanSourceTree });
			} else {
				let files = await packmanPackager.package(tmpPackmanSourceTree, parsed.targetPath, parsed.packageName, parsed.version);
				files.map(x => {
					if (!packages[x.type]) packages[x.type] = [];
					packages[x.type].push(x);
				});
			}
		}
	}


	if (parsed.doUpmPackage) {
		const tmpUpmSourceTree = p.join(parsed.skipPackaging ? parsed.targetPath : tmpBuildDir, parsed.packageName);
		// do the upm package first. it always has a -preview suffix
		let versionMetadataIndex = parsed.version.indexOf('+');
		parsed.version = parsed.version.substring(0, versionMetadataIndex > 0 ? versionMetadataIndex : undefined ) + "-preview";

		let packageJson = await upmPackager.prepare(parsed.sourcePath, parsed.version, parsed.ignores, tmpUpmSourceTree);

		if (parsed.skipPackaging) {
			packages[PackageType.Source].push({ type: PackageType.Source, path: tmpUpmSourceTree });
		} else {

			let files = await upmPackager.package(tmpUpmSourceTree, parsed.targetPath, parsed.packageName, parsed.version);
			let upmPackageFile: PackageFile = files.find(x => x.type === PackageType.PackmanPackage)!;
			files.map(x => {
				if (!packages[x.type]) packages[x.type] = [];
				packages[x.type].push(x);
			});

			if (packageJson) {
				const upmManifest: string = p.join(parsed.targetPath, `packages.json`);
				let packageManifest: { [key: string]: string } = {};
				if (await asyncfile.exists(upmManifest)) {
					packageManifest = JSON.parse(await asyncfile.readTextFile(upmManifest));
				}
			
				packageManifest[p.basename(upmPackageFile.path)] = JSON.parse(await asyncfile.readTextFile(packageJson));
				await asyncfile.writeTextFile(upmManifest, JSON.stringify(packageManifest));
				packages[PackageType.Manifest] = { type: PackageType.Manifest, path: upmManifest};
			}			
		}
	}

	await asyncfile.writeTextFile(manifest, JSON.stringify(packages));
})();


