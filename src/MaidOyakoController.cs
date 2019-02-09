#if DEBUG
//	#define	DISP_COLLIDER		// コライダーのデバッグ表示 別途 ColliderVisualizer.cs が必要(https://github.com/tomoriaki/collider-visualizer)
//	#define	DISP_TRIGGER		// 衝突判定のプリント
#endif // DEBUG
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Linq;
using System;

namespace CM3D2.MaidOyako.Plugin
{
	class MaidOyakoController : MonoBehaviour
	{
		private bool m_bIsEnableScene;                     // 親子付けが有効なシーンかどうか

		// AVRControllerButtons は CM3D2.MaidOyako.Plugin.cs でセットされる
		public AVRController m_thisController;				// メイドさんがくっつく方のコントローラー(thisが付いている方のコントローラーでもある)
		public AVRController m_inputController;			// 操作用のコントローラー
		private OvrGripCollider m_inputGrip;				// 操作用のコントローラーの下のGrabに付いているコンポーネント
		private FieldInfo m_grippingTrans;					// 操作用のコントローラーで掴んでいるオブジェクトを取得するためのFieldInfo
		private bool m_isVrMenu = false;					// VRMenuを使用しているかどうか
		private List<Transform> m_grippingListVRM;			// VRMenuのコントローラー
		private Maid m_maid;								// 親子付け中のメイドさん
		private Transform m_maid_trans;					// くっつけるターゲット(メイドさん)
		private Transform m_trans_dummy;
		private FieldInfo m_bBackHand;						// くっつく方のコントローラー側のハンドモードを取得するためのFieldInfo
		private FieldInfo m_attachSideL;					// 夜伽コマンドメニューが左右どちらに付いているかを取得するためのFieldInfo
		private bool m_isHideYotogiCommandMen;				// 夜伽コマンドメニューをけしたか
		private FieldInfo m_ctrlBehNowFI;					// くっつく方のコントローラー側の現在のControllerBehaviorを取得するためのFieldInfo
		private AVRControllerBehavior m_ctrlBehNow;		// m_ctrlBehNowFIで取得したControllerBehavior

		private List<Transform> m_handObjList = new List<Transform>();                  // コントローラーの表示用オブジェクト くっついている間これらは非アクティブにする
		private List<Maid> m_addColMaid = new List<Maid>();                             // コライダーを付けたメイドさんのリスト
		private Dictionary<Transform, int> m_grabDic = new Dictionary<Transform, int>();// タッチや掴み用のオブジェクトと元のレイヤー番号の辞書
		
		public bool IsControllerConfirm { get; private set; }   // 初回の親子付け入力でコントローラーの役割が確定したかどうか
		public bool IsInputController { get; private set; }     // 入力用のコントローラーになったかどうか
		public bool IsOyako { get; private set; }               // 親子付け中かどうか
		public bool IsGripping { get; private set; }            // 親子付け中にメイドさんが掴んで移動中か

		private void Start()
		{
#if DISP_TRIGGER
			gameObject.layer = LayerMask.NameToLayer( "OvrGrabHand" );
#endif // DISP_TRIGGER
			// VRMenuがあるかを調べる
			_findVRMenu();
		}

		private void OnLevelWasLoaded( int level )
		{
			// 親子付け中なら、解除
			if ( IsOyako )
			{
				_oyakoEnd();
			}
			foreach ( var maid in m_addColMaid )
			{
				// コライダーを消しておく
				MaidColliderCollect.RemoveColliderAll( maid );

				var trans = _getTargetTransform( maid.gameObject );
				if ( trans )
				{
					trans.localPosition = new Vector3();
					trans.localRotation = new Quaternion();
				}
			}
			m_addColMaid.Clear();

			IsControllerConfirm = false;
			IsInputController = false;
			IsOyako = false;
			IsGripping = false;
			m_maid = null;
			m_maid_trans = null;
			// COMの夜伽コマンドメニュー関連をクリアする
			_clrYotogiCommandMenu();

			// シーン名が有効にするシーン名と一致するか
			m_bIsEnableScene = Config.Instance.sceneNameList.Contains( GameMain.Instance.GetNowSceneName() );
#if DEBUG
			m_bIsEnableScene = true;
#endif // DEBUG
		}

		private void Update()
		{
			// 有効なシーンでなければ何もしない
			if ( !m_bIsEnableScene ) { return; }

			// 入力用のコントローラーになった場合は、以降何もしない
			// 操作は親子付けするコントローラーの方で入力用コントローラーの入力を調べて動く
			if ( IsInputController ) { return; }

			// コントローラーの役割確定前のみ自身が入力用として選択されたかを判定する
			if ( !IsControllerConfirm )
			{
				IsInputController = _isOyakoTrg( m_thisController );
			}

			// もう一方のコントローラーで操作する
			bool bTrg = _isOyakoTrg( m_inputController );
			if ( bTrg )
			{
				IsControllerConfirm = true;
				bTrg = true;
			}

			// 入力があった
			if ( bTrg )
			{
				// 親子付け中なら、解除
				if ( IsOyako )
				{
					_oyakoEnd();
				}
				// 親子付け中でないなら、開始
				else
				{
					Maid nearMaid = null;
					float minDist = float.MaxValue;
					var pos = transform.position;

					// コントローラーに一番近いメイドさんにくっつく様にする
					for ( int no = 0; no < GameMain.Instance.CharacterMgr.GetMaidCount(); no++ )
					{
						Maid maid = GameMain.Instance.CharacterMgr.GetMaid( no );
						if ( maid != null && maid.isActiveAndEnabled && maid.body0 != null && maid.body0.isLoadedBody )
						{
							// メイドさんの身体パーツ内の一番近い位置にあるパーツの距離を取得
							float dist = maid.GetComponentsInChildren<Transform>()
							.Min( t => (pos - t.transform.position).sqrMagnitude );
							if ( minDist > dist )
							{
								nearMaid = maid;
								minDist = dist;
							}
						}
					}

					if ( nearMaid != null )
					{
						_oyakoStart( nearMaid );
					}
				}
			}

			_onUpdate();
		}

		// 必要なGameObject等が無ければ作る
		private void _makeObject()
		{
			m_trans_dummy = transform.Find( "oyako_trans_dummy_" );
			if ( m_trans_dummy == null )
			{
				m_trans_dummy = (new GameObject( "oyako_trans_dummy_" )).transform;
				m_trans_dummy.SetParent( transform, false );
			}
			
			// 操作用のコントローラーの子供の「Grab」に付いているコンポーネントを取得しておく
			if ( m_inputController )
			{
				if ( m_inputGrip == null )
				{
					m_inputGrip = m_inputController.GetComponentInChildren<OvrGripCollider>();
					if ( m_inputGrip == null ) { MSG.Error( "OvrGripCollider が見つかりません。" ); }
				}

				if ( m_grippingTrans == null )
				{
					m_grippingTrans = typeof( OvrGripCollider ).GetField( "lock_object_trans_", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
					if ( m_grippingTrans == null ) { MSG.Error( "OvrGripCollider に lock_object_trans_ が存在しません。" ); }
				}
			}

			if ( m_thisController )
			{
				if ( m_ctrlBehNowFI == null )
				{
					m_ctrlBehNowFI = typeof( AVRController ).GetField( "m_CtrlBehNow", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
					if ( m_ctrlBehNowFI == null ) { MSG.Error( "AVRController に m_ctrlBehNow が存在しません。" ); }
				}
			}

			// COMの夜伽コマンドメニューを探す
			_findYotogiCommandMenu();
		}

		// メイドの親子付け開始
		private void _oyakoStart( Maid maid )
		{
			if ( maid == null || maid.body0 == null || !maid.body0.isLoadedBody )
			{
				MSG.Error( "メイド" + (maid ? maid.name : "") + "は初期化されていません。" );
				return;
			}

			// 必要なオブジェクトが無ければ作る
			_makeObject();

			m_maid = maid;

			// 掴み用コライダー作成
			{
				var mcc = MaidColliderCollect.AddColliderCollect( m_maid );
				if ( mcc )
				{
					var maidColliders = mcc.GetCollider( MaidColliderCollect.ColliderType.Grab );
					// コライダー配列が無いか、0個の場合、もしくはシーン開始から最初に親子付けされた場合
					if ( maidColliders == null ||
						 maidColliders.Count == 0 ||
						 !m_addColMaid.Contains( m_maid ) )
					{
						maidColliders = mcc.AddCollider( MaidColliderCollect.ColliderType.Grab );
						for ( int cnt = 0; cnt < maidColliders.Count; cnt++ )
						{
							ColliderEvent action = maidColliders[cnt].gameObject.AddComponent<ColliderEvent>();
							action.onMouseUp = m_maid.OnMouseUp;
#if DISP_COLLIDER
							maidColliders[cnt].gameObject.AddComponent<HC.Debug.ColliderVisualizer>().Initialize( HC.Debug.ColliderVisualizer.VisualizerColorType.Red, "", 8 );
#endif // DISP_COLLIDER
						}
						// コライダーを追加したメイドさんを覚えておく
						m_addColMaid.Add( m_maid );
					}
				}
			}
			
			// メイドさん操作用のTransformを取得
			m_maid_trans = _getTargetTransform( m_maid.gameObject );

			if ( m_trans_dummy == null )		{ MSG.Error( "m_trans_dummy がNULLです。" ); }
			if ( m_maid_trans == null )			{ MSG.Error( "m_maid_trans がNULLです。" ); }
			if ( m_maid_trans.parent == null )	{ MSG.Error( "m_maid_trans.parent がNULLです。" ); }

			if ( m_trans_dummy && m_maid_trans && m_maid_trans.parent )
			{
				// 掴んだ時にメイドさんが飛ばない様にする
				_resetTransDummyPos();

				// くっついている間はハンドオブジェクトが邪魔なので見えない様にする
				_hideHandObject();

				// 親子付け開始
				IsOyako = true;
				IsGripping = false;
				MSG.DebugPrint( m_maid.name + "の親子付け開始" );
			}
		}

		// メイドの親子付け終了
		private void _oyakoEnd()
		{
			MSG.DebugPrint( m_maid.name + "の親子付け終了" );
			m_maid = null;
			m_maid_trans = null;
			IsOyako = false;
			IsGripping = false;

			// 無効にしていたオブジェクトを復活させる
			_dispHandObject();
		}

		// 親子付け開始/終了のコントローラーの入力があったか
		private bool _isOyakoTrg( AVRController controller )
		{
			if ( controller != null &&
				 controller.VRControllerButtons != null )
			{
				// グリップ状態で
				if ( controller.VRControllerButtons.GetPress( AVRControllerButtons.BTN.GRIP ) )
				{
					// トリガーが引かれたら
					if ( controller.VRControllerButtons.GetPressDown( AVRControllerButtons.BTN.TRIGGER ) )
					{
						return true;
					}
				}
			}
			return false;
		}

		// m_trans_dummy の位置/回転をメイドさんの位置にリセットする
		private void _resetTransDummyPos()
		{
			// 掴んだ時にメイドさんが飛ばない様にする
			m_trans_dummy.SetParent( transform, false );
			m_trans_dummy.SetParent( m_maid_trans.parent, true );       // ワールドでの座標を維持しながら、メイドさんと兄弟になるようにする
			m_trans_dummy.localPosition = m_maid_trans.localPosition;   // メイドさんと同じ位置/回転へ移動
			m_trans_dummy.localRotation = m_maid_trans.localRotation;
			m_trans_dummy.SetParent( transform, true );                 // その位置/回転を維持したまま、コントローラーの子供に戻る
		}

		// 親子付け中の更新処理
		private void _onUpdate()
		{
			if ( IsOyako )
			{
				// 親子付け中にメイドさんが消えた場合は解除だけして戻る
				if ( m_maid && !m_maid.isActiveAndEnabled )
				{
					_oyakoEnd();
					return;
				}
				
				if ( m_maid_trans != null &&
					 m_trans_dummy != null )
				{
					bool bBeforeGripping = IsGripping;

					// 親子付けしているメイドさんが掴まれているか?
					IsGripping = _isGripping( m_maid_trans ) || _isGrippingVRMenu( m_maid_trans );

					// 掴んで移動されている最中は位置を上書きしない様にする
					if ( !IsGripping )
					{
						// 離されたフレームでは、その位置/回転で親子付けしなおす
						if ( bBeforeGripping )
						{
							_resetTransDummyPos();
						}
						else
						{
							m_maid_trans.position = m_trans_dummy.position;
							m_maid_trans.rotation = m_trans_dummy.rotation;
						}
					}
				}
			}
		}

		// メイドさん操作用のTransformを取得
		private Transform _getTargetTransform(GameObject obj)
		{
			if (obj == null || obj.transform == null)
			{
				return null;
			}
			for (Transform i = obj.transform; !(i == null) && !(i.parent == null); i = i.parent)
			{
				if (i.parent.gameObject.name == "PhotoPrefab" ||
					i.parent.gameObject.name == "AllOffset" )
				{
					return i;
				}
			}
			return null;
		}

		private static readonly int HAND_LAYER = LayerMask.NameToLayer( "TouchHand" );
		private static readonly int GRAB_LAYER = LayerMask.NameToLayer( "OvrGrabHand" );
		private static readonly int IGNORE_LAYER = LayerMask.NameToLayer( "Ignore Raycast" );
		// ハンドオブジェクトを非表示にする
		private void _hideHandObject()
		{
			// ViveCameraRig(Clone)/Controller (left)/ の下にある
			// HandCamera HandItem HandPlayer UI を消したい
			// HandCamera HandItem HandPlayer UI は自分の兄弟のはず…
			var transAry = transform.parent.GetComponentsInChildren<Transform>(false);

			foreach ( var trans in transAry )
			{
				var behaviours = trans.GetComponents<MonoBehaviour>();
				// レンダラーとテキストを無効にする
				// GripMoveやVRMenuの移動や回転が不用意に動いてしまわない様にこれらも無効にする
				var ren = trans.GetComponent<Renderer>();
				var txt = trans.GetComponent<Text>();
				var gmv = behaviours.FirstOrDefault( c => c.GetType().Name.Contains( "GripMoveController" ) );
				var mnu = behaviours.FirstOrDefault( c => c.GetType().Name.Contains( "MenuTool" ) );
				var vrm = behaviours.FirstOrDefault( c => c.GetType().Name.Contains( "VRMenuController" ) );
				
				if ( ren ) { ren.enabled = false; }
				if ( txt ) { txt.enabled = false; }
				if ( gmv ) { gmv.enabled = false; }
				if ( mnu ) { mnu.enabled = false; }
				if ( vrm ) { vrm.enabled = false; }

				// 無効にしたオブジェクトを記憶しておく
				if ( ren || txt || gmv || mnu || vrm ) { m_handObjList.Add( trans ); }

				// タッチや掴み用のオブジェクトはレイヤーを変えて反応しない様にしておく
				if ( trans.gameObject.layer == HAND_LAYER ||
					 trans.gameObject.layer == GRAB_LAYER )
				{
					m_grabDic.Add( trans, trans.gameObject.layer );
					trans.gameObject.layer = IGNORE_LAYER;
				}
			}

			// メニューボタンに反応してしまわない様に現在のControllerBehaviorが有効な場合のみ無効にする
			if ( m_ctrlBehNowFI != null )
			{
				AVRControllerBehavior ctrlBeh = (AVRControllerBehavior)m_ctrlBehNowFI.GetValue( m_thisController );
				if ( ctrlBeh && ctrlBeh.enabled )
				{
					ctrlBeh.enabled = false;
					m_ctrlBehNow = ctrlBeh;
				}
			}

			// COMの夜伽コマンドメニューを隠す
			_hideYotogiCommandMenu();
		}

		// 非表示にしたハンドオブジェクトを表示する
		private void _dispHandObject()
		{
			// 無効にしたコンポーネントを戻す
			foreach ( var trans in m_handObjList )
			{
				var behaviours = trans.GetComponents<MonoBehaviour>();
				var ren = trans.GetComponent<Renderer>();
				var txt = trans.GetComponent<Text>();
				var gmv = behaviours.FirstOrDefault( c => c.GetType().Name.Contains( "GripMoveController" ) );
				var mnu = behaviours.FirstOrDefault( c => c.GetType().Name.Contains( "MenuTool" ) );
				var vrm = behaviours.FirstOrDefault( c => c.GetType().Name.Contains( "VRMenuController" ) );

				if ( ren ) { ren.enabled = true; }
				if ( txt ) { txt.enabled = true; }
				if ( gmv ) { gmv.enabled = true; }
				if ( mnu ) { mnu.enabled = true; }
				if ( vrm ) { vrm.enabled = true; }
			}
			m_handObjList.Clear();

			// レイヤーを元に戻す
			foreach ( var pair in m_grabDic )
			{
				pair.Key.gameObject.layer = pair.Value;
			}
			m_grabDic.Clear();

			// 無効にしたControllerBehaviorを戻すが、シーンが変わって現在のControllerBehaviorが
			// 変わっている場合があるので同じか調べてから戻す
			if ( m_ctrlBehNowFI != null &&
				 m_ctrlBehNow != null )
			{
				AVRControllerBehavior ctrlBeh = (AVRControllerBehavior)m_ctrlBehNowFI.GetValue( m_thisController );
				if ( ctrlBeh && !ctrlBeh.enabled &&
					 ctrlBeh == m_ctrlBehNow )
				{
					ctrlBeh.enabled = true;
				}
				m_ctrlBehNow = null;
			}

			// COMの夜伽コマンドメニューを隠していたら戻す
			_showYotogiCommandMenu();
		}

		// 公式の掴み処理で trans を掴んでいるか調べる
		private bool _isGripping( Transform trans )
		{
			if ( m_inputGrip != null &&
				 m_grippingTrans != null )
			{
				// 何か掴んでいるなら、そのTransformを取得して、比較する
				if ( m_inputGrip.grip )
				{
					Transform grippingTrans = (Transform)m_grippingTrans.GetValue( m_inputGrip );
					if ( grippingTrans == trans )
					{
						return true;
					}
				}
			}
			return false;
		}

		// VRMenuがあるかを調べる
		private void _findVRMenu()
		{
			var vrm = transform.parent.GetComponents<MonoBehaviour>().FirstOrDefault( c => c.GetType().Name.Contains( "VRMenuController" ) );
			if ( vrm )
			{
				// VRMenuがあった
				if ( vrm.GetType().BaseType != null )
				{
					// 今、全コントローラで掴んでいるオブジェクトのリスト を取得
					var grippingListField = vrm.GetType().BaseType.GetField( "grippingList", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public );
					if ( grippingListField != null )
					{
						m_grippingListVRM = (List<Transform>)grippingListField.GetValue( vrm );
						if ( m_grippingListVRM != null )
						{
							m_isVrMenu = true;
						}
					}
				}
			}
		}

		// VRMenuで trans を掴んでいるか調べる
		private bool _isGrippingVRMenu( Transform trans )
		{
			if ( m_isVrMenu )
			{
				return m_grippingListVRM.Contains( trans );
			}
			return false;
		}
		
		// COMの夜伽コマンドメニュー関連をクリアする
		private void _clrYotogiCommandMenu()
		{
			m_bBackHand = null;
			m_attachSideL = null;
			m_isHideYotogiCommandMen = false;
		}
		// COMの夜伽コマンドメニューを探す
		private void _findYotogiCommandMenu()
		{
			// 幸いCMでも command_menu は存在していたので、そのまま使う
			if ( YotogiManager.instans &&
				 YotogiManager.instans.command_menu )
			{
				Type type = YotogiManager.instans.command_menu.GetType();
				
				m_bBackHand = m_bBackHand ?? type.GetField( m_thisController.m_bHandL ? "m_bBackHandL" : "m_bBackHandR", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
				m_attachSideL = m_attachSideL ?? type.GetField( "controller_attach_side_L_", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
			}
		}

		// COMの夜伽コマンドメニューを隠す
		private void _hideYotogiCommandMenu()
		{
			if ( m_bBackHand != null &&
				 m_attachSideL != null &&
				 YotogiManager.instans.command_menu.gameObject.activeInHierarchy )
			{
				// くっつく方のハンドモードがコマンドメニューで
				bool bBackHand = (bool)m_bBackHand.GetValue( YotogiManager.instans.command_menu );
				if ( bBackHand )
				{
					// メニューを表示しているなら
					bool attachSideL = (bool)m_attachSideL.GetValue( YotogiManager.instans.command_menu );
					if ( attachSideL == m_thisController.m_bHandL )
					{
						// 夜伽コマンドメニューを消す
						YotogiManager.instans.command_menu.gameObject.SetActive( false );
						m_isHideYotogiCommandMen = true;
					}
				}
			}

			if ( m_ctrlBehNowFI != null )
			{

			}
		}

		// COMの夜伽コマンドメニューを隠していたら戻す
		private void _showYotogiCommandMenu()
		{
			if ( m_isHideYotogiCommandMen )
			{
				if ( YotogiManager.instans &&
					 YotogiManager.instans.command_menu )
				{
					YotogiManager.instans.command_menu.gameObject.SetActive( true );
				}
				m_isHideYotogiCommandMen = false;
			}
		}

		////////////////////////////////////////////////////
		public static string GetHierarchyPath( Transform self )
		{
			string path = self.gameObject.name;
			Transform parent = self.parent;
			while ( parent != null )
			{
				path = parent.name + "/" + path;
				parent = parent.parent;
			}
			return path;
		}

#if DISP_TRIGGER
		private void OnTriggerEnter( Collider other )
		{
			MSG.DebugPrint( "Enter:" + Time.frameCount + " " + other.name + " ID:" + other.gameObject.GetInstanceID() );
			MSG.DebugPrint( GetHierarchyPath( other.transform ) );
		}

		private void OnTriggerExit( Collider other )
		{
		}

		private void OnTriggerStay( Collider other )
		{
		}
#endif // DISP_TRIGGER
	}

	// メッセージ表示クラス
	public class MSG
	{
		//　デバッグ用コンソール出力メソッド
		[Conditional( "DEBUG" )]
		public static void DebugPrint( string msg )
		{
			UnityEngine.Debug.LogWarning( "[MaidOyako] " + msg );
		}
		public static void Warning( string msg )
		{
			UnityEngine.Debug.LogWarning( "[MaidOyako] " + msg );
		}
		public static void Error( string msg )
		{
			UnityEngine.Debug.LogError( "[MaidOyako] " + msg );
		}
	}
}
 