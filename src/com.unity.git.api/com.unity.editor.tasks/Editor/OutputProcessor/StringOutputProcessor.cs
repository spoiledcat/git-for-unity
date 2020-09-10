namespace Unity.Editor.Tasks
{
	using System.Collections.Generic;
	using System.Text;

	/// <summary>
	/// Aggregates multiple string entries into one result, excluding null entries.
	/// </summary>
	public class StringOutputProcessor : BaseOutputProcessor<string>
	{
		private readonly StringBuilder sb = new StringBuilder();

		protected override bool ProcessLine(string line, out string result)
		{
			base.ProcessLine(line, out result);

			if (line == null)
				return false;

			sb.AppendLine(line);
			return true;
		}

		public override string Result { get { return sb.ToString(); } }
	}

    /// <summary>
	/// Aggregates multiple string entries into a list of string result, excluding null entries.
	/// </summary>
	public class StringListOutputProcessor : BaseOutputListProcessor<string>
	{
		protected override bool ProcessLine(string line, out List<string> result)
		{
			base.ProcessLine(line, out result);

			if (line == null)
				return false;

			return true;
		}
	}
}
