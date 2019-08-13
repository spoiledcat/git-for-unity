import * as asyncfile from 'async-file';
import * as p from 'path';
import md5 = require('md5');
import { Packager, PackageType, createTar, PackageFile } from './packager';

export class UpmPackager extends Packager {

    public constructor() {
        super();
    }

    public async package(sourcePath: string, targetPath: string, packageName: string, version: string) : Promise<PackageFile[]>{

        const tgzPath: string = p.join(targetPath, `${packageName}-${version}.tgz`);
        const tgzMd5Path: string = p.join(targetPath, `${packageName}-${version}.tgz.md5`);

        await asyncfile.mkdirp(targetPath);

        let ret: PackageFile[] = [];

        await createTar(sourcePath, tgzPath, 'package');
        let hash = md5(await asyncfile.readFile(tgzPath));
        await asyncfile.writeTextFile(tgzMd5Path, hash);
        ret.push({ type: PackageType.PackmanPackage, path: tgzPath, md5Path: tgzMd5Path, md5Hash: hash });

        return ret;
    }
}