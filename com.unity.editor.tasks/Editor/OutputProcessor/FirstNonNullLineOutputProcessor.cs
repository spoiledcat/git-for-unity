namespace Unity.Editor.Tasks
{
	using System;

	/// <summary>
	/// Returns the first non-null, non-empty (after trim) input.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class FirstNonNullOutputProcessor<T> : FirstResultOutputProcessor<T>
	{
		public FirstNonNullOutputProcessor(Func<string, T> converter)
			: base(converter)
		{}

		public FirstNonNullOutputProcessor(FuncO<string, T, bool> handler = null)
			: base(handler)
		{}

		protected override bool ProcessLine(string line, out T result)
		{
			result = default;

			if (string.IsNullOrEmpty(line))
				return false;

			line = line.Trim();

			return base.ProcessLine(line, out result);
		}
	}
}
