import * as tmp from 'tmp';
import { promisify } from 'util';
import * as fs from 'fs';

const imageThumbnail = require('image-thumbnail');

export const generateThumbnail = async (imagePath: string) => {
	const thumbnail : Buffer = await imageThumbnail(imagePath);
	return thumbnail;
}

const tmpDirSync = (config: tmp.Options, callback?: (err: Error, path: string) => void): void => {
	if (callback)
		return tmp.dir({...config, ...{ unsafeCleanup: true } }, (e, p, _) => callback(e, p));
	return tmp.dir(config);
}

const tmpFileSync = (config: tmp.Options, callback?: (err: Error, path: string) => void): void => {
	if (callback)
		return tmp.file({...config, ...{ unsafeCleanup: true } }, (e, p, _) => callback(e, p));
	return tmp.file(config);
}

export const tmpDir = promisify(tmpDirSync);
export const copyFile = promisify(fs.copyFile);
