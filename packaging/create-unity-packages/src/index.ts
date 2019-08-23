#!/usr/bin/env node_modules/.bin/ts-node

import * as asyncfile from 'async-file';
import * as commandLineArgs from 'command-line-args';
import * as p from 'path';
import { readAllLines } from './read-lines';
import { FileTreeWalker } from './TreeWalker';
import { optionDefinitions, validateOptionPath, validateOption } from './cmdlineoptions';
import { PackageType, PackageFile } from './packager';
import { UnityPackager } from './unityPackager';
import { PackmanPackager } from './packmanPackager';
import { UpmPackager } from './upmPackager';
import { Ignores } from './RecursiveReaddir';

interface ParsedOptions {
	sourcePath: string;
	targetPath: string;
	packageName: string;
	version: string;
	baseInstallationPath: string | undefined;
	skipPackaging: boolean;
	ignores: Ignores;
	doUnityPackage: boolean;
	doPackmanPackage: boolean;
	doUpmPackage: boolean;
	tmpPath: string | undefined;
	other: string[];
}

async function parseCommandLine() : Promise<ParsedOptions> {
	const options = commandLineArgs(optionDefinitions);
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
				if (!packages[PackageType.Source]) packages[PackageType.Source] = [];
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
				if (!packages[PackageType.Source]) packages[PackageType.Source] = [];
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
		const tmpUpmSourceTree = p.join(tmpBuildDir, parsed.packageName);
		// do the upm package first. it always has a -preview suffix
		let versionMetadataIndex = parsed.version.indexOf('+');
		parsed.version = parsed.version.substring(0, versionMetadataIndex > 0 ? versionMetadataIndex : undefined ) + "-preview";

		let packageJson = await upmPackager.prepare(parsed.sourcePath, parsed.version, parsed.ignores, tmpUpmSourceTree);

		if (parsed.skipPackaging) {
			if (!packages[PackageType.Source]) packages[PackageType.Source] = [];
			packages[PackageType.Source].push({ type: PackageType.Source, path: tmpUpmSourceTree });
		} else {

			let files = await upmPackager.package(tmpUpmSourceTree, parsed.targetPath, parsed.packageName, parsed.version);
			let upmPackageFile: PackageFile = files.find(x => x.type === PackageType.PackmanPackage)!;
			files.map(x => {
				if (!packages[x.type]) packages[x.type] = [];
				packages[x.type].push(x);
			});

			if (packageJson) {
				const upmManifestFile: string = p.join(parsed.targetPath, `manifest-${parsed.packageName}.json`);
				let upmManifest: { [key: string]: {} } = {};

				// this adds multiple dependent packages to a single manifest
				const json = JSON.parse(await asyncfile.readTextFile(packageJson));
				let deps = json['dependencies'];
				for (let dep in deps) {
					const dependencyJsonFile = p.join(parsed.targetPath, `manifest-${dep}.json`);
					if (await asyncfile.exists(dependencyJsonFile)) {
						const depmanifest: { [key: string]: {} } = JSON.parse(await asyncfile.readTextFile(dependencyJsonFile));
						for (let entry in depmanifest) {
							upmManifest[entry] = depmanifest[entry];
						}
					}
				}

				upmManifest[p.basename(upmPackageFile.path)] = JSON.parse(await asyncfile.readTextFile(packageJson));
				await asyncfile.writeTextFile(upmManifestFile, JSON.stringify(upmManifest, undefined, 2));

				// save the upm manifest file
				packages[PackageType.Manifest] = { type: PackageType.Manifest, path: upmManifestFile};
			}			
		}
	}

	await asyncfile.writeTextFile(manifest, JSON.stringify(packages, undefined, 2));
})();
