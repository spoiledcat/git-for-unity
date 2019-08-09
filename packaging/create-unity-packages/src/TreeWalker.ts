import * as asyncfile from 'async-file';
import * as fs from 'fs';
import { ReplaySubject, Observable, Observer, Subject } from 'rxjs';
import * as p from 'path';
import { readdir, Ignores, FileEntry } from './RecursiveReaddir';
import { tmpDir } from './helpers';

export class FileTreeWalker {
	createTar(): any {
		throw new Error("Method not implemented.");
	}
	private obs: Observable<FileEntry>;
	private _listener = new ReplaySubject<FileEntry>();
	private running = false;

	public constructor(public path: string, ignores?: Ignores) {
		this.obs = new Observable((observer: Observer<FileEntry>) => {
			readdir(this.path, ignores || [], (error, file) => {
				if (error) {
					observer.error(error);
				} else if (file) {
					observer.next(file);
				} else {
					observer.complete();
				}
				return false;
			});
		});
	}

	public static walk(path: string, ignores?: Ignores) {
		return new FileTreeWalker(path, ignores).walk();
	}

	public static copy = (from: string, to: string, ignores?: Ignores) => {
		return new FileTreeWalker(from, ignores).copy(to);
	}

	private get listener() {
		if (!this.running) {
			this.obs.subscribe(val => { this._listener.next(val); }, err => this._listener.error(err), () => { this._listener.complete() });
			this.running = true;
		}
		return this._listener.asObservable();
	}

	public static getTempDir = () => tmpDir({ prefix: 'packaging-', unsafeCleanup: true, keep: true });

	public walk = () => {
		return this.listener;
	}

	public copy = (to: string) => {
		const ret = new Subject();

		this.listener.subscribe(async entry => {
			if (entry.isDir) {
				const relativeSourceDir = p.relative(this.path, entry.file);
				const targetDir = p.join(to, relativeSourceDir);
				await asyncfile.mkdirp(targetDir);
			} else {
				const relativeSourceDir = p.relative(this.path, p.dirname(entry.file));
				const targetDir = p.join(to, relativeSourceDir);
				const targetFilePath = p.join(targetDir, p.basename(entry.file));
				fs.copyFileSync(entry.file, targetFilePath);
			}
		}, err => ret.error(err), () => ret.complete());

		ret.subscribe();
		return ret.toPromise();
	}
}

