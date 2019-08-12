import * as fs from 'fs';
import * as asyncfile from 'async-file';
import * as p from 'path';
import * as minimatch from 'minimatch';
import { from } from 'rxjs';

export type FileEntry = { file: string, isDir: boolean, stats: fs.Stats };
type IgnoreFunction = (file: FileEntry) => boolean;
export type Ignores = ReadonlyArray<string | IgnoreFunction>;
type Callback = (error?: Error, file?: FileEntry) => boolean;

const patternMatcher = (pattern: string): IgnoreFunction => {
  return (file: FileEntry) => {
    const minimatcher = new minimatch.Minimatch(pattern, { matchBase: true, dot: true });
    const ret = (!minimatcher.negate || !file.isDir) && minimatcher.match(file.file);
    // if (ret) {
    //   console.log(`Ignoring ${file.file} because ${pattern}`);
    // }
    return ret;
  };
};
const toMatcherFunction = (ignoreEntry: string | IgnoreFunction) => {
  if (typeof ignoreEntry == "function") {
    return ignoreEntry;
  }
  else {
    return patternMatcher(ignoreEntry);
  }
};

export function readdir(path: string, ignores: Ignores, callback: Callback) {
  const ignoredFuncs = ignores.map(toMatcherFunction);
  let pending = 0;
  let done = false;
  return recursiveReadDir(path, ignoredFuncs,
    count => {
      if (done) return;
      if (count === -2)
        done = true;
      else {
        pending += count;
        done = pending <= 0;
      }
      if (done)
        callback();
    },
    file => {
      if (done) return true;
      done = callback(undefined, file);
      if (done)
        callback();
      return done;
    },
    err => {
      if (done) return true;
      done = callback(err);
      if (done)
        callback();
      return done;
    });
}

const recursiveReadDir = (path: string, ignores: IgnoreFunction[],
            onProcessing: (count: number) => void,
            onFile: (file: FileEntry) => boolean,
            onError: (Error: Error) => boolean) => {

  onProcessing(1);

  fs.readdir(path, (err, files) => {
    if (err) {
		onError(err);
		return;
    }

    let pending = files.length;
    let stop = false;

    from(files).subscribe(async file => {
      if (stop) return;

      var filePath = p.join(path, file);
      try {

        const stats = await asyncfile.stat(filePath);
        const entry = { file: filePath, isDir: stats.isDirectory(), stats };
        if (!ignores.some((matcher) => matcher(entry))) {
          if (stats.isDirectory()) {
            stop = onFile(entry);
            if (!stop)
              recursiveReadDir(filePath, ignores, onProcessing, onFile, onError);
          } else {
            stop = onFile(entry);
          }
        }
      } catch(err) {
        stop = onError(err);
      }
      
      
      pending--;
      if (!pending)
        onProcessing(-1);
      else if (stop)
        onProcessing(-2);
    });
  });
}
