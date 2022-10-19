namespace Unity.Editor.Tasks
{
	using System;

	/// <summary>
	/// Processor that returns one output of type <typeparamref name="T"/>
	/// from one or more string inputs. <see cref="BaseOutputProcessor{T}.RaiseOnEntry(T)"/>
	/// will only be called once on this processor.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class FirstResultOutputProcessor<T> : BaseOutputProcessor<T>
	{
		private bool isSet = false;

		/// <summary>
		/// The first input that the converter can convert without throwing an exception will
		/// be the result of this output processor, all other inputs are ignored.
		/// </summary>
		public FirstResultOutputProcessor(Func<string, T> converter)
			: base(converter)
		{}

		/// <summary>
		/// The first input that the <paramref name="handler"/> returns true will be
		/// the result of this output processor, all other inputs will be ignored.
		/// </summary>
		/// <param name="handler"></param>
		public FirstResultOutputProcessor(FuncO<string, T, bool> handler = null)
			: base(handler)
		{}

		protected override void LineReceived(string line)
		{
			if (isSet) return;
			if (!ProcessLine(line, out var entry)) return;

			isSet = true;
			RaiseOnEntry(entry);
		}
	}
}
