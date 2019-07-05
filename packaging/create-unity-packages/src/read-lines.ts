import * as asyncfile from 'async-file';
import { from, Observable } from "rxjs";
import { map, toArray, filter } from 'rxjs/operators';

export const readLines = (data: string) => from(data.split('\n')).pipe(map(x => x.trim()), filter(x => x.length > 0));
export const readAllLines = async (data: string) => readLines(data).pipe(filter(x => !x.startsWith('#')), toArray()).toPromise();

export const readLinesFromFile = (file: string) => {

	const readStream = asyncfile.createReadStream(file);
	return new Observable<string>(s => {
		let line: string = '';
		readStream
			.on('data', (chunk : Uint8Array) => {
				let data = chunk.toString();
				let endOfLine = data.lastIndexOf('\n');
				let hasExtra = endOfLine >= 0 && endOfLine < data.length - 1;
				let extraLine = '';
				if (hasExtra) {
					extraLine = data.substr(endOfLine + 1);
					data = data.substr(0, endOfLine);
				}
	
				let lines = data.replace(/\r/g, '').split('\n');
				if (lines.length > 0 && line.length > 0) {
					lines[0] = `${line}${lines[0]}`;
					line = '';
				}
	
				if (hasExtra) {
					line = extraLine;
				}
	
				for (let l of lines) {
					s.next(l);
				}
			})
			.on('close', () => {
				if (line.length > 0)
					s.next(line);
				s.complete();
			});
	
		return () => readStream.close();
	})

}
