import { Ignores } from "./RecursiveReaddir";
import { OptionDefinition, Section } from "command-line-usage";
import commandLineArgs = require("command-line-args");
import commandLineUsage = require("command-line-usage");
import * as asyncfile from 'async-file';
import * as p from 'path';

export async function validateOptionPath(options: commandLineArgs.CommandLineOptions, argName: string, optional: boolean = false) {

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

export function validateOption(options: commandLineArgs.CommandLineOptions, argName: string, optional: boolean = false) {
	if (!options[argName] || options[argName] === '')
	{
		console.error(`Missing argument: ${argName}`);
		console.error(commandLineUsage(sections));
		process.exit(-1);
	}
	return options[argName] as string;
}

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
	doUpmPackage: boolean;
	tmpPath: string | undefined;
	other: string[];
}

export const optionDefinitions : OptionDefinition[] = [
	{
		name: 'help',
		alias: 'h',
		description: 'Display this usage guide.',
	},
	{
		name: 'src',
		alias: 's',
		type: String,
		description: 'Path to the sources to be packaged.',
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
		description: 'Name of the package',
	},
	{
		name: 'version',
		alias: 'v',
		type: String,
		description: 'Version of the package',
	},
	{
		name: 'ignores',
		alias: 'i',
		type: String,
		description: 'Path to file with globs of things to ignore, like a .gitignore or an .npmignore',
	},
	{
		name: 'installPath',
		alias: 't',
		type: String,
		description: 'Installation path for unity packages (set in the meta files)',
	},
	{
		name: 'skip',
		alias: 'k',
		type: Boolean,
		defaultValue: false,
		description: 'Skip all packaging and just prepare the source tree for packaging'
	},
	{
		name: 'skipUnity',
		alias: 'u',
		type: Boolean,
		defaultValue: false,
		description: 'Skip packaging for the asset store (.unitypackage)'
	},
	{
		name: 'skipPackage',
		alias: 'p',
		type: Boolean,
		defaultValue: false,
		description: 'Skip packaging for package manager (zip with the sources inside a "package name" folder )'
	},
	{
		name: 'skipUpm',
		alias: 'm',
		type: Boolean,
		defaultValue: false,
		description: 'Skip packaging for upm (.tgz with the sources inside a "package" folder)'
	},
	{
		name: 'tmp',
		typeLabel: '{underline directory}',
		description: 'Temporary directory to store combined artifacts into while processing multiple packages',
	},
];

export const sections : Section[] = [
	{
		header: 'Unity packager',
		content: 'Takes a source tree, and creates packman and asset store packages',
	},
	{
		header: 'Options',
		optionList: optionDefinitions,
	},
];
