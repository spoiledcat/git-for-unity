#!/usr/bin/env node_modules/.bin/ts-node

import * as asyncfile from 'async-file';
import * as md5 from 'md5';
import * as commandLineArgs from 'command-line-args';
import * as commandLineUsage from 'command-line-usage';
import * as p from 'path';
import { FileTreeWalker, createTar } from './TreeWalker';


const optionDefinitions = [
	{
		name: 'help',
		alias: 'h',
		description: 'Display this usage guide.',
	},
	{
		name: 'src',
		alias: 's',
		typeLabel: '{underline directory}',
		multiple: true,
		defaultOption: true,
		description: 'Path to the source assets to be packaged',
	},
	{
		name: 'name',
		alias: 'n',
		typeLabel: '{underline name}',
		description: 'Name of the package',
	},
	{
		name: 'version',
		alias: 'v',
		typeLabel: '{underline version}',
		description: 'Version of the package',
	},
	{
		name: 'out',
		alias: 'o',
		typeLabel: '{underline directory}',
		description: 'Where to save the zip and md5 files',
	}
];

const sections = [
	{
		header: 'Unity packager',
		content: 'Combines a tree of unity package assets into one single package',
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

	const sourcePaths: string[] = options.src || [];

	const packageName: string = options.name;
	const version: string = options.version || "0.0.0";
    const targetPath: string = p.resolve(options.out);
    const assetStoreZipPath: string = p.join(targetPath, `${packageName}-${version}.unitypackage`);
	const assetStoreZipMd5Path: string = p.join(targetPath, `${packageName}-${version}.unitypackage.md5`);

    const combined = await FileTreeWalker.getTempDir();

	for (let i = 0; i < sourcePaths.length; i++) {
		let sourcePath = sourcePaths[i];
		if (!(await asyncfile.exists(sourcePath)))
		{
			console.error(`${sourcePath} does not exist`);
			console.error(commandLineUsage(sections));
			process.exit(-1);
		}
		sourcePath = p.resolve(sourcePath);

		await FileTreeWalker.copy(sourcePath, combined);
	}

	await asyncfile
		.mkdirp(p.dirname(assetStoreZipPath))
		.then(() => createTar(new FileTreeWalker(combined)))
		.then(tar => tar.pipe(asyncfile.createWriteStream(assetStoreZipPath)).on('finish', async () =>
			{
				console.log(`Finalizing ${assetStoreZipPath}...`)
				const hash = md5(await asyncfile.readFile(assetStoreZipPath));
				await asyncfile.writeTextFile(assetStoreZipMd5Path, hash);
				console.log(`${assetStoreZipPath} and ${assetStoreZipMd5Path} created`);
			})
	);
})();
