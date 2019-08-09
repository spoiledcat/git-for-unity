import * as asyncfile from 'async-file';
import * as yaml from 'js-yaml';
import * as p from 'path';
import * as sizeOf from "image-size";
import { copyFile, generateThumbnail, tmpDir } from './helpers';
import md5 = require('md5');
import { Packager, PackageType, createZip, createTar, PackageFile, PackageHandler } from './packager';
import { FileTreeWalker } from './TreeWalker';
import { debug } from 'util';

export class UnityPackager extends Packager {

    static handlers: PackageHandler = {
        ['.png']: UnityPackager.pngHandler,
        ['.meta']: UnityPackager.metaHandler,
        ['']: async (baseSourcePath: string, sourceFile: string,
                     baseTargetPath: string, targetFile: string,
                     baseInstallationPath: string, version: string) => true,
    };

    public constructor() {
        super();
        super.Handlers = UnityPackager.handlers;
    }

    public async prepareSource(sourcePath: string, outputPath: string, baseInstallationPath?: string) {
        super.prepare(sourcePath, '', [entry => !entry.isDir && p.extname(entry.file) !== '.meta'], outputPath, baseInstallationPath);
    }

    public async package(sourcePath: string, targetPath: string, packageName: string, version: string) : Promise<PackageFile[]>{

        const packagePath: string = p.join(targetPath, `${packageName}-${version}.unitypackage`);
        const packageMd5Path: string = p.join(targetPath, `${packageName}-${version}.unitypackage.md5`);

        await asyncfile
            .mkdirp(p.dirname(packagePath))
            .then(() => createZip(sourcePath, packagePath))
            .then(async () => {
                const hash = md5(await asyncfile.readFile(packagePath));
                await asyncfile.writeTextFile(packageMd5Path, hash);
            });

        return [
            { type: PackageType.UnityPackage, path: packagePath, md5Path: packageMd5Path }
        ];
    }

    static async metaHandler (baseSourcePath: string, sourceFile: string,
        baseTargetPath: string, targetFile: string,
        baseInstallationPath: string, version: string) : Promise<boolean>
    {
        const thefile = sourceFile.substr(0, sourceFile.lastIndexOf('.'));
    
        if (!(await asyncfile.exists(thefile))) {
            return true;
        }
    
        const isDir = (await asyncfile.stat(thefile)).isDirectory();
        const relativeSourcePath = p.relative(baseSourcePath, thefile);
        const installationPath = p.join(baseInstallationPath, relativeSourcePath);

        const meta = await asyncfile.readTextFile(sourceFile);
        const yamlmeta: { guid: string } = yaml.safeLoad(meta, { json: true });
        const targetdir = p.join(baseTargetPath, yamlmeta.guid);
        const targetmeta = p.join(targetdir, 'asset.meta');
        const targetname = p.join(targetdir, 'pathname');
        const targetasset = p.join(targetdir, 'asset');
    
        await asyncfile.mkdirp(targetdir);
        await asyncfile.writeTextFile(targetname, installationPath.replace(/\\/g, '/'));
    
        await copyFile(sourceFile, targetmeta);
    
        if (!isDir) {
            await Packager.runPackageHandler(UnityPackager.handlers, baseSourcePath, thefile, baseTargetPath, targetasset, baseInstallationPath, version);
        }
    
        return true;
    }

    static async pngHandler (baseSourcePath: string, sourceFile: string,
        baseTargetPath: string, targetFile: string,
        baseInstallationPath: string, version: string) : Promise<boolean>
    {
        const targetPreview = p.join(p.dirname(targetFile), 'preview.png');
    
        if (!p.isAbsolute(targetPreview))
            targetFile = p.join(baseTargetPath, targetPreview);
    
        const size = sizeOf(sourceFile);
        await asyncfile.mkdirp(p.dirname(targetPreview));
    
        if (size.width > 128) {			
            // console.log(`Generating ${targetPreview} from ${sourceFile}`);
            await generateThumbnail(sourceFile).then(b => asyncfile.createWriteStream(targetPreview).write(b));
        } else {
            // console.log(`Copying ${sourceFile} to ${targetPreview}`);
            await copyFile(sourceFile, targetPreview);
        }
        // let the default handling code copy the original asset
        return false;
    }
}
