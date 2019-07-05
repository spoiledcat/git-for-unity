import * as asyncfile from 'async-file';
import { readAllLines, readLines, readLinesFromFile } from '../src/read-lines';
import { expect, assert } from 'chai';
import { toArray, takeUntil, filter, takeWhile } from 'rxjs/operators';
import { promisify } from 'util';
import * as chaiAsPromised from "chai-as-promised";
import * as chai from "chai";

const setTimeoutPromise = promisify(setTimeout);
chai.use(chaiAsPromised);

describe('readLinesFromFile', () => {
	it('should return an observable value for every line', async () => {
		const txt = await readLinesFromFile('tests/small-text.txt').pipe(toArray()).toPromise();
		expect(txt).to.eql(['One line', 'two line', 'more line!', '']);
	}),
	it('should return an observable value for every line regardless of eol', async () => {
		const expected = (await asyncfile.readTextFile('tests/text_eol_mix.txt')).replace(/\r/g, '').split('\n');
		const actual = await readLinesFromFile('tests/text_eol_mix.txt').pipe(toArray()).toPromise();
		expect(actual).to.eql(expected);
	}),
	it('should return an observable value for every line regardless of file size', async () => {
		const expected = (await asyncfile.readTextFile('tests/text_eol_crlf.txt')).replace(/\r/g, '').split('\n');
		const actual = await readLinesFromFile('tests/text_eol_crlf.txt').pipe(toArray()).toPromise();
		expect(actual).to.eql(expected);
	}),
	it('doesnt read everything if interrupted', async () => {
		const actual = await readLinesFromFile('tests/text_eol_crlf.txt').pipe(takeWhile(x => x !== 'SPLIT'), toArray()).toPromise();
		expect(actual).to.eql(['One line', 'two line', 'more line!']);
	})
});
