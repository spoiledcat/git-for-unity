// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Unity.Editor.Tasks.Logging
{
    public abstract class LogAdapterBase
    {
	    public abstract void Info(string context, string message);

	    public abstract void Debug(string context, string message);

	    public abstract void Trace(string context, string message);

	    public abstract void Warning(string context, string message);

	    public abstract void Error(string context, string message);
    }
}
