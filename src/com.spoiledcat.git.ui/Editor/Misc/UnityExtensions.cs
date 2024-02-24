using UnityObject=UnityEngine.Object;

namespace Unity.VersionControl.Git.Extensions
{
	public static class UnityExtensions
	{

		public static T Ref<T>(this T obj) where T : UnityObject => obj != null ? obj : null;
	}
}