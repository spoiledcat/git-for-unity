#!/usr/bin/env node_modules/.bin/ts-node

import * as asyncfile from 'async-file';
import * as commandLineArgs from 'command-line-args';
import { validateOptionPath } from './cmdlineoptions';
import { OptionDefinition } from 'command-line-usage';

interface ParsedOptions {
	target: string;
	source: string;
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
		description: 'Target manifest file to update.',
	},
	{
		name: 'src',
		alias: 's',
		type: String,
		description: 'Source manifest file with entries to add to target manifest',
	}
];

async function parseCommandLine() : Promise<ParsedOptions> {
	const options = commandLineArgs(optionDefinitions);

	const parsed : ParsedOptions = {
		target: (await validateOptionPath(options, "out"))!,
		source: (await validateOptionPath(options, "src"))!,
	};

	return parsed;
}

(async () => {

	const parsed = await parseCommandLine();

	const source: { [key: string]: {} } = JSON.parse(await asyncfile.readTextFile(parsed.source));
	const target: { [key: string]: {} } = JSON.parse(await asyncfile.readTextFile(parsed.target));
	
	for (let dep in source) {
		if (!target[dep]) {
			target[dep] = source[dep];
			console.log(`Added ${dep} to manifest`);
		}
	}

	await asyncfile.writeTextFile(parsed.target, JSON.stringify(target, undefined, 2));
    console.log(`Manifest ${parsed.source} updated`);
})();
