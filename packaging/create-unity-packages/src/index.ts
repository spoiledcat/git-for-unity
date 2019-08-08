#!/usr/bin/env node_modules/.bin/ts-node

import * as asyncfile from 'async-file';
import * as md5 from 'md5';
import * as commandLineArgs from 'command-line-args';
import * as commandLineUsage from 'command-line-usage';
import * as p from 'path';
import { Ignores, FileEntry } from './RecursiveReaddir';
import { readAllLines } from './read-lines';
import { FileTreeWalker, createAssetStorePackageTree, createPackmanPackageTree, createTar, packageThings, prepare } from './TreeWalker';

const optionDefinitions = [
	{
		name: 'help',
		alias: 'h',
		description: 'Display this usage guide.',
	},
	{
		name: 'out',
		alias: 'o',
		typeLabel: '{underline directory}',
		description: 'Where to save the zip and md5 files',
	},
	{
		name: 'name',
		alias: 'n',
		type: String,
		multiple: true, 
		description: 'Name of the package',
	},
	{
		name: 'version',
		alias: 'v',
		type: String,
		multiple: true, 
		description: 'Version of the package',
	},
	{
		name: 'src',
		alias: 's',
		multiple: true, 
		type: String,
		description: 'Path(s) to the source assets to be packaged. If you pass more than one, you must pass an equal amount to extras, ignores, installPath and project',
	},
	{
		name: 'extras',
		alias: 'e',
		multiple: true, 
		type: String,
		description: 'Path to extra files to also be packaged (recursively',
	},
	{
		name: 'ignores',
		alias: 'i',
		multiple: true, 
		type: String,
		description: 'Path to file with globs of things to ignore, like a .gitignore or an .npmignore',
	},
	{
		name: 'installPath',
		alias: 't',
		multiple: true, 
		type: String,
		description: 'Installation path for unity packages (set in the meta files)',
	},
	{
		name: 'project',
		alias: 'u',
		type: Boolean,
		multiple: true, 
		description: 'Set if the source is a Unity project',
	},
	{
		name: 'skip',
		alias: 'k',
		type: Boolean,
		description: 'Skip packaging and just copy the files to the outpath'
	}
];

const sections = [
	{
		header: 'Unity packager',
		content: 'Takes a Unity (packman) package source tree, and packages it into a .unitypackage or a packman zip package',
	},
	{
		header: 'Options',
		optionList: optionDefinitions,
	},
];

(async () => {

	const options = commandLineArgs(optionDefinitions);

	if (!options.name)
	{
		console.error("Missing argument: name");
		console.error(commandLineUsage(sections));
		process.exit(-1);
	}
	
	if (!options.out)
	{
		console.error("Missing argument: out");
		console.error(commandLineUsage(sections));
		process.exit(-1);
	}

	if (!options.version)
	{
		console.error("Missing argument: version");
		console.error(commandLineUsage(sections));
		process.exit(-1);
	}
	if (!options.src)
	{
		console.error("Missing argument: src");
		console.error(commandLineUsage(sections));
		process.exit(-1);
	}

	const packageNames: string[] = options.name;
	const versions: string[] = options.version || [];
	const sourcePaths: string[] = options.src || [];
	const extraPaths: string[] = options.extras || [];
	const ignoreFiles: string[] = options.ignores || [];
	const projectFlags: boolean[] = options.project || [];
	const installPaths: string[] = options.installPath || [];

	const skipPackaging: boolean = options.skip || false;

	if (packageNames.length > 1) {
		if (packageNames.length != extraPaths.length ||
			packageNames.length != sourcePaths.length || 
			packageNames.length != versions.length || 
			packageNames.length != ignoreFiles.length || 
			packageNames.length != projectFlags.length || 
			packageNames.length != installPaths.length
		)
		{
			console.error(`Invalid number of arguments, should be equal to ${packageNames.length}`);
			console.error(commandLineUsage(sections));
			process.exit(-1);
		}
	}

	const targetPath: string = p.resolve(options.out);

	const packages : {[key:number] : { assetStorePath: string, packmanPackagePath: string, packmanPackageManifest: string | undefined } } = [];

	for (let i = 0; i < packageNames.length; i++)
	{
		let sourcePath = sourcePaths[i];
		if (!(await asyncfile.exists(sourcePath)))
		{
			console.error(`${sourcePath} does not exist`);
			console.error(commandLineUsage(sections));
			process.exit(-1);
		}

		sourcePath = p.resolve(sourcePath);
		const packageName: string = packageNames[i];
		const version: string = versions[i] || "0.0.0";
		const baseInstallationPath: string = installPaths[i] || p.join('Packages', packageName);
		const extrasPath: string | undefined = extraPaths[i] ? p.resolve(extraPaths[i]) : undefined;
		const ignoreFile = ignoreFiles[i] || undefined;
		const isUnityProject: boolean = projectFlags[i] || false;
	
		let result = await prepare(packageName, version, targetPath, sourcePath, baseInstallationPath, extrasPath, ignoreFile, isUnityProject);

		if (skipPackaging) {

			let targetDir1 = p.join(targetPath, `${packageName}-${version}`, 'unitypackage');
			await asyncfile.mkdirp(targetDir1);
			FileTreeWalker.copy(result.assetStorePath, targetDir1);
			let targetDir2 = p.join(targetPath, `${packageName}-${version}`);
			await asyncfile.mkdirp(targetDir2);
			FileTreeWalker.copy(result.packmanPackagePath, targetDir2);
			packages[i] = { assetStorePath: targetDir1, packmanPackagePath: targetDir2, packmanPackageManifest: undefined };

		} else {
			packages[i] = await packageThings(packageName, version, targetPath, result.packageJson, result.assetStorePath, result.packmanPackagePath);
		}
	}


	for (let i = 0; i < packageNames.length; i++) {
		const packageName: string = packageNames[i];
		const version: string = versions[i] || "0.0.0";
		const result = packages[i];

		console.log(`${packageName} ${version} ${result.assetStorePath} ${result.packmanPackagePath} ${result.packmanPackageManifest || ''} `);
	}
 
})();


