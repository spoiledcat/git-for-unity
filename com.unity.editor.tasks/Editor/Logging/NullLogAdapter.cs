// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Unity.Editor.Tasks.Logging
{
    public class NullLogAdapter : LogAdapterBase
    {
	    public override void Info(string context, string message)
        {
        }

	    public override void Debug(string context, string message)
        {
        }

	    public override void Trace(string context, string message)
        {
        }

	    public override void Warning(string context, string message)
        {
        }

	    public override void Error(string context, string message)
        {
        }
    }
}
