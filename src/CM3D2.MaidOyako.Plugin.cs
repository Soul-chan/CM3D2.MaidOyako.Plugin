using System.Collections;
using UnityEngine;
using UnityInjector;
using UnityInjector.Attributes;

namespace CM3D2.MaidOyako.Plugin
{
	[PluginName("MaidOyako")]
	[PluginVersion("1.0.1.0")]
	public class MaidOyako : PluginBase
	{
		// ConfigDataからのデータパス取得用
		public static string ConfigXmlPath { get; private set; }
		// インストール済かどうか
		public bool IsInstalled { get { return m_leftInstalled && m_rightInstalled; } }

		private bool m_leftInstalled = false;
		private bool m_rightInstalled = false;

		public MaidOyako()
		{
			ConfigXmlPath = DataPath + @"\" + Name + ".xml";
		}
		
		private void Start()
		{
			// VRモード時のみ
			if ( !GameMain.Instance.VRMode)
			{
				DestroyImmediate(this);
				return;
			}
		}

		private void OnLevelWasLoaded(int level)
		{
			StopAllCoroutines();
			if ( !IsInstalled )
			{
				StartCoroutine( "InstallMaidOyakoControllerCo" );
			}
		}

		// 親子付けコントローラーのインストール試行コルーチン
		private IEnumerator InstallMaidOyakoControllerCo()
		{
			MSG.DebugPrint( "親子付けインストール開始" );
			while ( !IsInstalled )
			{
				tryInstallMaidOyakoController();
				if ( IsInstalled )
				{
					MSG.DebugPrint( "親子付けインストール完了" );
					break;
				}
				yield return new WaitForSeconds(0.5f);
			}
		}

		// 親子付けコントローラーのインストール試行
		private void tryInstallMaidOyakoController()
		{
			if (GameMain.Instance.VRFamily == GameMain.VRFamilyType.HTC)
			{
				// 「GameMain.Instance.OvrMgr」がついているのは「__GameMain__」
				// 「ovrObj.left_controller.controller」がついているのは
				// 「__GameMain__/ViveCameraRig(Clone)/Controller (left)」
				OvrMgr.OvrObject ovrObj = GameMain.Instance.OvrMgr.ovr_obj;
				if (ovrObj != null)
				{
					 m_leftInstalled = addOyakoController( ovrObj. left_controller.controller.transform, ovrObj.right_controller.controller.transform,  m_leftInstalled );
					m_rightInstalled = addOyakoController( ovrObj.right_controller.controller.transform, ovrObj. left_controller.controller.transform, m_rightInstalled );
				}
			}
			else
			if (GameMain.Instance.VRFamily == GameMain.VRFamilyType.Oculus)
			{
				OVRCameraRig componentInParent = GameMain.Instance.OvrMgr.OvrCamera.gameObject.GetComponentInParent<OVRCameraRig>();

				if (componentInParent != null)
				{
					 m_leftInstalled = addOyakoController( componentInParent. leftHandAnchor, componentInParent.rightHandAnchor,  m_leftInstalled );
					m_rightInstalled = addOyakoController( componentInParent.rightHandAnchor, componentInParent. leftHandAnchor, m_rightInstalled );
				}
			}
			MSG.DebugPrint( " left:" + m_leftInstalled + " right:" + m_rightInstalled );
		}

		// 親子付けコントローラーのコンポーネントを付与する
		// myHand : コンポーネントを付与するコントローラーのTransform
		// otherHand : 反対側のコントローラーのTransform
		// bInstalled : インストール済かどうか、インストール済なら何もしない
		private bool addOyakoController( Transform myHand, Transform otherHand, bool bInstalled )
		{
			if ( myHand != null && otherHand != null && !bInstalled )
			{
				// controller に直接 AddComponent するのではなく、Grabと同じように
				// 子供を作ってそれに AddComponent する
				GameObject oyakoObj = null;
				Transform oyakoTrans = myHand.Find( "YotogiMaidOyako" );
				if ( oyakoTrans == null )
				{
					oyakoObj = new GameObject( "YotogiMaidOyako" );
					oyakoObj.transform.SetParent( myHand, false );
				}
				else
				{
					oyakoObj = oyakoTrans.gameObject;
				}

				MaidOyakoController ctrl = oyakoObj.GetComponent<MaidOyakoController>() ??
										   oyakoObj.AddComponent<MaidOyakoController>();

				// AVRController を取得する
				ctrl. m_thisController = ctrl. m_thisController ??    myHand.gameObject.GetComponent<AVRController>();
				ctrl.m_inputController = ctrl.m_inputController ?? otherHand.gameObject.GetComponent<AVRController>();
				
				return (ctrl.m_thisController != null && ctrl.m_inputController != null);
			}
			return bInstalled;
		}
	}
}
