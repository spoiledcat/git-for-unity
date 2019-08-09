#!/usr/bin/env node_modules/.bin/ts-node

import * as asyncfile from 'async-file';
import * as commandLineArgs from 'command-line-args';
import * as commandLineUsage from 'command-line-usage';
import * as p from 'path';
import { readAllLines } from './read-lines';
import { FileTreeWalker } from './TreeWalker';
import { optionDefinitions, sections, ParsedOptions } from './cmdlineoptions';
import { PackageFile, PackageType, PackageFileList } from './packager';
import { UnityPackager } from './unityPackager';
import { PackmanPackager } from './packmanPackager';

async function validateOptionPath(options: commandLineArgs.CommandLineOptions, argName: string, optional: boolean = false) {

	if (optional && !options[argName]) return undefined;

	validateOption(options, argName);

	if (!(await asyncfile.exists(options[argName]))) {
		console.error(`Bad parameter ${argName}: ${options[argName]} does not exist`);
		console.error(commandLineUsage(sections));
		process.exit(-1);
	}
	return p.resolve(options[argName]);
}

function validateOption(options: commandLineArgs.CommandLineOptions, argName: string, optional: boolean = false) {
	if (!options[argName])
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
		doPackmanPackage: !options.skipPackman,
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
	
	const tmpPackmanSourceTree = parsed.skipPackaging ? p.join(parsed.targetPath, 'package', parsed.packageName) : await FileTreeWalker.getTempDir();
	const tmpUnitySourceTree = parsed.skipPackaging ? p.join(parsed.targetPath, "unitypackage") : await FileTreeWalker.getTempDir();

	const packmanPackager = new PackmanPackager();
	const unityPackager = new UnityPackager();

	const packageJson = await packmanPackager.prepare(parsed.sourcePath, parsed.version, parsed.ignores, tmpPackmanSourceTree);

	if (parsed.doUnityPackage) {
		await unityPackager.prepareSource(tmpPackmanSourceTree, tmpUnitySourceTree, parsed.baseInstallationPath);
	}

	console.log(tmpPackmanSourceTree);
	console.log(tmpUnitySourceTree);

	const packages: PackageFileList = {};
	if (parsed.skipPackaging) {
		if (parsed.doPackmanPackage)
			packages[PackageType.Source] = { type: PackageType.Source, path: tmpPackmanSourceTree };
		if (parsed.doUnityPackage)
			packages[PackageType.Source] = { type: PackageType.Source, path: tmpUnitySourceTree };
	} else {
		if (parsed.doPackmanPackage) {
			let files = await packmanPackager.package(tmpPackmanSourceTree, parsed.targetPath, parsed.packageName, parsed.version);
			files.map(x => packages[x.type] = x);

			if (packageJson) {
				const packmanPackageManifest: string = p.join(parsed.targetPath, `packages.json`);
				let packageManifest: { [key: string]: string } | undefined = undefined;
					packageManifest = {
					[p.basename(p.basename(packages[PackageType.PackmanPackage].path))]: JSON.parse(await asyncfile.readTextFile(packageJson))
				};
				await asyncfile.writeTextFile(packmanPackageManifest, JSON.stringify(packageManifest));
				packages[PackageType.Manifest] = { type: PackageType.Manifest, path: packmanPackageManifest};
			}
		}

		if (parsed.doUnityPackage) {
			let files = await unityPackager.package(tmpUnitySourceTree, parsed.targetPath, parsed.packageName, parsed.version);
			files.map(x => packages[x.type] = x);
		}
	}

	console.log(JSON.stringify(packages));
})();


