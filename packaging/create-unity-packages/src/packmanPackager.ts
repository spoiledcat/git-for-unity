import * as asyncfile from 'async-file';
import * as p from 'path';
import { Ignores } from './RecursiveReaddir';
import md5 = require('md5');
import { FileTreeWalker } from './TreeWalker';
import { Packager, PackageType, createTar, createZip, PackageFile } from './packager';

export class PackmanPackager extends Packager {

    public constructor() {
        super();
    }

    public async package(sourcePath: string, targetPath: string, packageName: string, version: string) : Promise<PackageFile[]>{

        const tgzPath: string = p.join(targetPath, `${packageName}-${version}.tgz`);
        const tgzMd5Path: string = p.join(targetPath, `${packageName}-${version}.tgz.md5`);

        const zipPath: string = p.join(targetPath, `${packageName}-${version}.zip`);
        const zipMd5Path: string = p.join(targetPath, `${packageName}-${version}.zip.md5`);

        await asyncfile
            .mkdirp(p.dirname(tgzPath))
            .then(() => createTar(new FileTreeWalker(sourcePath), 'package'))
            .then(async archiver => {
                archiver
                    .pipe(asyncfile.createWriteStream(tgzPath))
                    .on('close', async () =>
                    {
                        const hash = md5(await asyncfile.readFile(tgzPath));
                        await asyncfile.writeTextFile(tgzMd5Path, hash);
                    });
                await archiver.finalize();
            }
        );

        await asyncfile
            .mkdirp(p.dirname(zipPath))
            .then(() => createZip(sourcePath, zipPath, p.dirname(sourcePath) === packageName ? packageName : undefined))
            .then(async () => {
                const hash = md5(await asyncfile.readFile(zipPath));
                await asyncfile.writeTextFile(zipMd5Path, hash);
        });

        return [
            { type: PackageType.PackmanPackage, path: tgzPath, md5Path: tgzMd5Path }, 
            { type: PackageType.PackmanSource, path: zipPath, md5Path: zipMd5Path }
        ];
    }
}