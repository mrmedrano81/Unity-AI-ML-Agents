using UnityEngine;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Drakkar
{
#if ODIN_INSPECTOR
	[HideMonoScript]
#endif
	[DefaultExecutionOrder(97)]
	public class DrakkarUpdaterPost : MonoBehaviour
	{
		#region UNITY STUFF
		private void Update()
		{
		#if DRAKKAR
			if (DrakkarUpdater.instance.WaitForColdstart && !DrakkarUpdater.instance.started && !ColdStart.Ready)
				return;
		#endif
			DrakkarUpdater.instance.processUpdatePost();
		}

		private void LateUpdate()
		{
		#if DRAKKAR
			if (DrakkarUpdater.instance.WaitForColdstart && !DrakkarUpdater.instance.started && !ColdStart.Ready)
				return;
		#endif
			DrakkarUpdater.instance.processLateUpdatePost();
		}
#endregion
	}
}
