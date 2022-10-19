// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Collections.Generic;

namespace Unity.Editor.Tasks
{
	public partial class FuncTask<T> : TaskBase<T>
	{}

	public partial class FuncTask<T, TResult> : TaskBase<T, TResult>
	{}

	public partial class FuncListTask<T> : DataTaskBase<T, List<T>>
	{}

	public partial class FuncListTask<T, TData, TResult> : DataTaskBase<T, TData, List<TResult>>
	{}
}
