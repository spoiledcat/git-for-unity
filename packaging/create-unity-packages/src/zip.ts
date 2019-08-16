#!/usr/bin/env node_modules/.bin/ts-node

import * as asyncfile from 'async-file';
import * as commandLineArgs from 'command-line-args';
import * as commandLineUsage from 'command-line-usage';
import * as p from 'path';
import { readAllLines } from './read-lines';
import { FileTreeWalker } from './TreeWalker';
import { PackageFile, PackageType, PackageFileList, createZip } from './packager';
import { UnityPackager } from './unityPackager';
import { PackmanPackager } from './packmanPackager';

import { Ignores } from "./RecursiveReaddir";
import { OptionDefinition, Section } from "command-line-usage";
import { pluck, map, toArray } from 'rxjs/operators';
import { readFile, readFileSync } from 'fs';

export interface ParsedOptions {
	sourcePath: string;
	targetPath: string;
	packageName: string;
	version: string;
	baseInstallationPath: string | undefined;
	skipPackaging: boolean;
	ignores: Ignores;
	doUnityPackage: boolean;
	doPackmanPackage: boolean;
}

const optionDefinitions : OptionDefinition[] = [
	{
		name: 'help',
		alias: 'h',
		description: 'Display this usage guide.',
	},
	{
		name: 'out',
		alias: 'o',
        type: String,
		description: 'Where to put the stuff',
	},
	{
		name: 'src',
		alias: 's',
        type: String,
		description: 'Zip this thing',
	},
	{
		name: 'name',
		alias: 'n',
		type: String,
		description: 'Name of the package',
	},
	{
		name: 'version',
		alias: 'v',
		type: String,
		description: 'Version of the package',
	},
	{
		name: 'unity',
		alias: 'u',
		type: Boolean,
		description: 'Create a unity package',
	}
];

const sections : Section[] = [
	{
		header: 'Zip',
		content: 'Zip stuff up and also create an md5 for it',
	},
	{
		header: 'Options',
		optionList: optionDefinitions,
	},
];


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

async function parseCommandLine() {
	const options = commandLineArgs(optionDefinitions);

	const parsed = {
		packageName: validateOption(options, "name"),
		sourcePath: (await validateOptionPath(options, "src"))!,
		targetPath: validateOption(options, "out"),
		version: validateOption(options, "version"),
		doUnityPackage: options.unity === true
	};

	parsed.targetPath = p.resolve(parsed.targetPath);
	if (!await asyncfile.exists(parsed.targetPath)) {
		await asyncfile.mkdirp(parsed.targetPath);
	}

	return parsed;
}

(async () => {

	const parsed = await parseCommandLine();

	const packages: PackageFileList = {};
    if (parsed.doUnityPackage) {
        const packager = new UnityPackager();
        let files = await packager.package(parsed.sourcePath, parsed.targetPath, parsed.packageName, parsed.version);
        files.map(x => packages[x.type] = x);
    } else {
        const packager = new PackmanPackager();
        let files = await packager.package(parsed.sourcePath, parsed.targetPath, parsed.packageName, parsed.version);
        files.map(x => packages[x.type] = x);

        const packmanPackageManifest: string = p.join(parsed.targetPath, `packages.json`);
        let packageManifest: { [key: string]: string[] } = {};
        let packageKey = p.basename(p.basename(packages[PackageType.PackmanPackage].path));
        let json = await FileTreeWalker.walk(parsed.sourcePath, [entry => p.basename(entry.file) !== 'package.json'])
            .pipe<string, string, string[]>(
                pluck('file'),
                map(file => JSON.parse(readFileSync(file, 'utf8'))),
                toArray()
            ).toPromise();
            
        packageManifest[packageKey] = json;

        await asyncfile.writeTextFile(packmanPackageManifest, JSON.stringify(packageManifest, undefined, 2));
        packages[PackageType.Manifest] = { type: PackageType.Manifest, path: packmanPackageManifest};
    }

	console.log(JSON.stringify(packages, undefined, 2));
})();


