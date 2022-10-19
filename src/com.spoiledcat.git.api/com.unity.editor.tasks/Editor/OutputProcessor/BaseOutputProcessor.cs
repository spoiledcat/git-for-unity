// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Collections.Generic;

namespace Unity.Editor.Tasks
{
	using Logging;

	public interface IOutputProcessor
	{
		void Process(string line);
	}

	public interface IOutputProcessor<T> : IOutputProcessor
	{
		event Action<T> OnEntry;
		T Result { get; }
	}

	public interface IOutputProcessor<TData, T> : IOutputProcessor<T>
	{
		new event Action<TData> OnEntry;
	}

	public class BaseOutputProcessor<T> : IOutputProcessor<T>
	{
		public delegate T3 FuncO<in T1, T2, out T3>(T1 arg1, out T2 out1);

		private Func<string, T> converter;
		private readonly FuncO<string, T, bool> handler;

		private static readonly bool IsString = typeof(T) == typeof(string);

		private ILogging logger;
		public event Action<T> OnEntry;

		public BaseOutputProcessor() {}

		public BaseOutputProcessor(FuncO<string, T, bool> handler)
		{
			this.handler = handler;
		}

		public BaseOutputProcessor(Func<string, T> converter)
		{
			this.converter = converter;
		}

		public void Process(string line)
		{
			LineReceived(line);
		}

		protected virtual void LineReceived(string line)
		{
			if (ProcessLine(line, out var entry))
				RaiseOnEntry(entry);
		}

		protected virtual bool ProcessLine(string line, out T result)
		{
			result = default;

			if (handler != null)
			{
				if (handler(line, out result))
					return true;
				return false;
			}

			if (converter != null)
			{
				// if there's a converter, all results are valid
				try
				{
					result = converter(line);
					return true;
				}
				catch
				{
					return false;
				}
			}

			// if T is string, no conversion is needed, result is valid
			if (IsString)
			{
				result = (T)(object)line;
				return true;
			}
			return false;
		}

		protected void RaiseOnEntry(T entry)
		{
			Result = entry;
			OnEntry?.Invoke(entry);
		}

		public virtual T Result { get; protected set; }
		protected ILogging Logger { get { return logger = logger ?? LogHelper.GetLogger(GetType()); } }
	}

	public class BaseOutputProcessor<TData, T> : BaseOutputProcessor<T>, IOutputProcessor<TData, T>
	{
		public new event Action<TData> OnEntry;
		private Func<string, TData> converter;
		private readonly FuncO<string, TData, bool> handler;
		private static readonly bool IsString = typeof(TData) == typeof(string);

		public BaseOutputProcessor() {}

		public BaseOutputProcessor(FuncO<string, TData, bool> handler)
		{
			this.handler = handler;
		}

		public BaseOutputProcessor(Func<string, TData> converter)
		{
			this.converter = converter;
		}

		protected override void LineReceived(string line)
		{
			if (ProcessLine(line, out var entry))
				RaiseOnEntry(entry);
		}

		protected virtual bool ProcessLine(string line, out TData result)
		{
			result = default;

			if (handler != null)
			{
				if (handler(line, out result))
					return true;
				return false;
			}

			if (converter != null)
			{
				// if there's a converter, all results are valid
				try
				{
					result = converter(line);
					return true;
				}
				catch
				{
					return false;
				}
			}

			// if T is string, no conversion is needed, result is valid
			if (IsString)
			{
				result = (TData)(object)line;
				return true;
			}
			return false;
		}

		protected virtual void RaiseOnEntry(TData entry)
		{
			OnEntry?.Invoke(entry);
		}
	}

	public class BaseOutputListProcessor<T> : BaseOutputProcessor<T, List<T>>
	{
		public BaseOutputListProcessor() { }

		public BaseOutputListProcessor(FuncO<string, T, bool> handler) : base(handler)
		{}

		public BaseOutputListProcessor(Func<string, T> converter) : base(converter)
		{}

		protected override void RaiseOnEntry(T entry)
		{
			if (Result == null)
			{
				Result = new List<T>();
			}
			Result.Add(entry);
			base.RaiseOnEntry(entry);
		}
	}
}
