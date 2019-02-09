#if DEBUG
//	#define	DISP_COLLIDER		// �R���C�_�[�̃f�o�b�O�\�� �ʓr ColliderVisualizer.cs ���K�v(https://github.com/tomoriaki/collider-visualizer)
//	#define	DISP_TRIGGER		// �Փ˔���̃v�����g
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
		private bool m_bIsEnableScene;                     // �e�q�t�����L���ȃV�[�����ǂ���

		// AVRControllerButtons �� CM3D2.MaidOyako.Plugin.cs �ŃZ�b�g�����
		public AVRController m_thisController;				// ���C�h���񂪂��������̃R���g���[���[(this���t���Ă�����̃R���g���[���[�ł�����)
		public AVRController m_inputController;			// ����p�̃R���g���[���[
		private OvrGripCollider m_inputGrip;				// ����p�̃R���g���[���[�̉���Grab�ɕt���Ă���R���|�[�l���g
		private FieldInfo m_grippingTrans;					// ����p�̃R���g���[���[�Œ͂�ł���I�u�W�F�N�g���擾���邽�߂�FieldInfo
		private bool m_isVrMenu = false;					// VRMenu���g�p���Ă��邩�ǂ���
		private List<Transform> m_grippingListVRM;			// VRMenu�̃R���g���[���[
		private Maid m_maid;								// �e�q�t�����̃��C�h����
		private Transform m_maid_trans;					// ��������^�[�Q�b�g(���C�h����)
		private Transform m_trans_dummy;
		private FieldInfo m_bBackHand;						// ���������̃R���g���[���[���̃n���h���[�h���擾���邽�߂�FieldInfo
		private FieldInfo m_attachSideL;					// �鉾�R�}���h���j���[�����E�ǂ���ɕt���Ă��邩���擾���邽�߂�FieldInfo
		private bool m_isHideYotogiCommandMen;				// �鉾�R�}���h���j���[����������
		private FieldInfo m_ctrlBehNowFI;					// ���������̃R���g���[���[���̌��݂�ControllerBehavior���擾���邽�߂�FieldInfo
		private AVRControllerBehavior m_ctrlBehNow;		// m_ctrlBehNowFI�Ŏ擾����ControllerBehavior

		private List<Transform> m_handObjList = new List<Transform>();                  // �R���g���[���[�̕\���p�I�u�W�F�N�g �������Ă���Ԃ����͔�A�N�e�B�u�ɂ���
		private List<Maid> m_addColMaid = new List<Maid>();                             // �R���C�_�[��t�������C�h����̃��X�g
		private Dictionary<Transform, int> m_grabDic = new Dictionary<Transform, int>();// �^�b�`��͂ݗp�̃I�u�W�F�N�g�ƌ��̃��C���[�ԍ��̎���
		
		public bool IsControllerConfirm { get; private set; }   // ����̐e�q�t�����͂ŃR���g���[���[�̖������m�肵�����ǂ���
		public bool IsInputController { get; private set; }     // ���͗p�̃R���g���[���[�ɂȂ������ǂ���
		public bool IsOyako { get; private set; }               // �e�q�t�������ǂ���
		public bool IsGripping { get; private set; }            // �e�q�t�����Ƀ��C�h���񂪒͂�ňړ�����

		private void Start()
		{
#if DISP_TRIGGER
			gameObject.layer = LayerMask.NameToLayer( "OvrGrabHand" );
#endif // DISP_TRIGGER
			// VRMenu�����邩�𒲂ׂ�
			_findVRMenu();
		}

		private void OnLevelWasLoaded( int level )
		{
			// �e�q�t�����Ȃ�A����
			if ( IsOyako )
			{
				_oyakoEnd();
			}
			foreach ( var maid in m_addColMaid )
			{
				// �R���C�_�[�������Ă���
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
			// COM�̖鉾�R�}���h���j���[�֘A���N���A����
			_clrYotogiCommandMenu();

			// �V�[�������L���ɂ���V�[�����ƈ�v���邩
			m_bIsEnableScene = Config.Instance.sceneNameList.Contains( GameMain.Instance.GetNowSceneName() );
#if DEBUG
			m_bIsEnableScene = true;
#endif // DEBUG
		}

		private void Update()
		{
			// �L���ȃV�[���łȂ���Ή������Ȃ�
			if ( !m_bIsEnableScene ) { return; }

			// ���͗p�̃R���g���[���[�ɂȂ����ꍇ�́A�ȍ~�������Ȃ�
			// ����͐e�q�t������R���g���[���[�̕��œ��͗p�R���g���[���[�̓��͂𒲂ׂē���
			if ( IsInputController ) { return; }

			// �R���g���[���[�̖����m��O�̂ݎ��g�����͗p�Ƃ��đI�����ꂽ���𔻒肷��
			if ( !IsControllerConfirm )
			{
				IsInputController = _isOyakoTrg( m_thisController );
			}

			// ��������̃R���g���[���[�ő��삷��
			bool bTrg = _isOyakoTrg( m_inputController );
			if ( bTrg )
			{
				IsControllerConfirm = true;
				bTrg = true;
			}

			// ���͂�������
			if ( bTrg )
			{
				// �e�q�t�����Ȃ�A����
				if ( IsOyako )
				{
					_oyakoEnd();
				}
				// �e�q�t�����łȂ��Ȃ�A�J�n
				else
				{
					Maid nearMaid = null;
					float minDist = float.MaxValue;
					var pos = transform.position;

					// �R���g���[���[�Ɉ�ԋ߂����C�h����ɂ������l�ɂ���
					for ( int no = 0; no < GameMain.Instance.CharacterMgr.GetMaidCount(); no++ )
					{
						Maid maid = GameMain.Instance.CharacterMgr.GetMaid( no );
						if ( maid != null && maid.isActiveAndEnabled && maid.body0 != null && maid.body0.isLoadedBody )
						{
							// ���C�h����̐g�̃p�[�c���̈�ԋ߂��ʒu�ɂ���p�[�c�̋������擾
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

		// �K�v��GameObject����������΍��
		private void _makeObject()
		{
			m_trans_dummy = transform.Find( "oyako_trans_dummy_" );
			if ( m_trans_dummy == null )
			{
				m_trans_dummy = (new GameObject( "oyako_trans_dummy_" )).transform;
				m_trans_dummy.SetParent( transform, false );
			}
			
			// ����p�̃R���g���[���[�̎q���́uGrab�v�ɕt���Ă���R���|�[�l���g���擾���Ă���
			if ( m_inputController )
			{
				if ( m_inputGrip == null )
				{
					m_inputGrip = m_inputController.GetComponentInChildren<OvrGripCollider>();
					if ( m_inputGrip == null ) { MSG.Error( "OvrGripCollider ��������܂���B" ); }
				}

				if ( m_grippingTrans == null )
				{
					m_grippingTrans = typeof( OvrGripCollider ).GetField( "lock_object_trans_", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
					if ( m_grippingTrans == null ) { MSG.Error( "OvrGripCollider �� lock_object_trans_ �����݂��܂���B" ); }
				}
			}

			if ( m_thisController )
			{
				if ( m_ctrlBehNowFI == null )
				{
					m_ctrlBehNowFI = typeof( AVRController ).GetField( "m_CtrlBehNow", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
					if ( m_ctrlBehNowFI == null ) { MSG.Error( "AVRController �� m_ctrlBehNow �����݂��܂���B" ); }
				}
			}

			// COM�̖鉾�R�}���h���j���[��T��
			_findYotogiCommandMenu();
		}

		// ���C�h�̐e�q�t���J�n
		private void _oyakoStart( Maid maid )
		{
			if ( maid == null || maid.body0 == null || !maid.body0.isLoadedBody )
			{
				MSG.Error( "���C�h" + (maid ? maid.name : "") + "�͏���������Ă��܂���B" );
				return;
			}

			// �K�v�ȃI�u�W�F�N�g��������΍��
			_makeObject();

			m_maid = maid;

			// �͂ݗp�R���C�_�[�쐬
			{
				var mcc = MaidColliderCollect.AddColliderCollect( m_maid );
				if ( mcc )
				{
					var maidColliders = mcc.GetCollider( MaidColliderCollect.ColliderType.Grab );
					// �R���C�_�[�z�񂪖������A0�̏ꍇ�A�������̓V�[���J�n����ŏ��ɐe�q�t�����ꂽ�ꍇ
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
						// �R���C�_�[��ǉ��������C�h������o���Ă���
						m_addColMaid.Add( m_maid );
					}
				}
			}
			
			// ���C�h���񑀍�p��Transform���擾
			m_maid_trans = _getTargetTransform( m_maid.gameObject );

			if ( m_trans_dummy == null )		{ MSG.Error( "m_trans_dummy ��NULL�ł��B" ); }
			if ( m_maid_trans == null )			{ MSG.Error( "m_maid_trans ��NULL�ł��B" ); }
			if ( m_maid_trans.parent == null )	{ MSG.Error( "m_maid_trans.parent ��NULL�ł��B" ); }

			if ( m_trans_dummy && m_maid_trans && m_maid_trans.parent )
			{
				// �͂񂾎��Ƀ��C�h���񂪔�΂Ȃ��l�ɂ���
				_resetTransDummyPos();

				// �������Ă���Ԃ̓n���h�I�u�W�F�N�g���ז��Ȃ̂Ō����Ȃ��l�ɂ���
				_hideHandObject();

				// �e�q�t���J�n
				IsOyako = true;
				IsGripping = false;
				MSG.DebugPrint( m_maid.name + "�̐e�q�t���J�n" );
			}
		}

		// ���C�h�̐e�q�t���I��
		private void _oyakoEnd()
		{
			MSG.DebugPrint( m_maid.name + "�̐e�q�t���I��" );
			m_maid = null;
			m_maid_trans = null;
			IsOyako = false;
			IsGripping = false;

			// �����ɂ��Ă����I�u�W�F�N�g�𕜊�������
			_dispHandObject();
		}

		// �e�q�t���J�n/�I���̃R���g���[���[�̓��͂���������
		private bool _isOyakoTrg( AVRController controller )
		{
			if ( controller != null &&
				 controller.VRControllerButtons != null )
			{
				// �O���b�v��Ԃ�
				if ( controller.VRControllerButtons.GetPress( AVRControllerButtons.BTN.GRIP ) )
				{
					// �g���K�[�������ꂽ��
					if ( controller.VRControllerButtons.GetPressDown( AVRControllerButtons.BTN.TRIGGER ) )
					{
						return true;
					}
				}
			}
			return false;
		}

		// m_trans_dummy �̈ʒu/��]�����C�h����̈ʒu�Ƀ��Z�b�g����
		private void _resetTransDummyPos()
		{
			// �͂񂾎��Ƀ��C�h���񂪔�΂Ȃ��l�ɂ���
			m_trans_dummy.SetParent( transform, false );
			m_trans_dummy.SetParent( m_maid_trans.parent, true );       // ���[���h�ł̍��W���ێ����Ȃ���A���C�h����ƌZ��ɂȂ�悤�ɂ���
			m_trans_dummy.localPosition = m_maid_trans.localPosition;   // ���C�h����Ɠ����ʒu/��]�ֈړ�
			m_trans_dummy.localRotation = m_maid_trans.localRotation;
			m_trans_dummy.SetParent( transform, true );                 // ���̈ʒu/��]���ێ������܂܁A�R���g���[���[�̎q���ɖ߂�
		}

		// �e�q�t�����̍X�V����
		private void _onUpdate()
		{
			if ( IsOyako )
			{
				// �e�q�t�����Ƀ��C�h���񂪏������ꍇ�͉����������Ė߂�
				if ( m_maid && !m_maid.isActiveAndEnabled )
				{
					_oyakoEnd();
					return;
				}
				
				if ( m_maid_trans != null &&
					 m_trans_dummy != null )
				{
					bool bBeforeGripping = IsGripping;

					// �e�q�t�����Ă��郁�C�h���񂪒͂܂�Ă��邩?
					IsGripping = _isGripping( m_maid_trans ) || _isGrippingVRMenu( m_maid_trans );

					// �͂�ňړ�����Ă���Œ��͈ʒu���㏑�����Ȃ��l�ɂ���
					if ( !IsGripping )
					{
						// �����ꂽ�t���[���ł́A���̈ʒu/��]�Őe�q�t�����Ȃ���
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

		// ���C�h���񑀍�p��Transform���擾
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
		// �n���h�I�u�W�F�N�g���\���ɂ���
		private void _hideHandObject()
		{
			// ViveCameraRig(Clone)/Controller (left)/ �̉��ɂ���
			// HandCamera HandItem HandPlayer UI ����������
			// HandCamera HandItem HandPlayer UI �͎����̌Z��̂͂��c
			var transAry = transform.parent.GetComponentsInChildren<Transform>(false);

			foreach ( var trans in transAry )
			{
				var behaviours = trans.GetComponents<MonoBehaviour>();
				// �����_���[�ƃe�L�X�g�𖳌��ɂ���
				// GripMove��VRMenu�̈ړ����]���s�p�ӂɓ����Ă��܂�Ȃ��l�ɂ����������ɂ���
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

				// �����ɂ����I�u�W�F�N�g���L�����Ă���
				if ( ren || txt || gmv || mnu || vrm ) { m_handObjList.Add( trans ); }

				// �^�b�`��͂ݗp�̃I�u�W�F�N�g�̓��C���[��ς��Ĕ������Ȃ��l�ɂ��Ă���
				if ( trans.gameObject.layer == HAND_LAYER ||
					 trans.gameObject.layer == GRAB_LAYER )
				{
					m_grabDic.Add( trans, trans.gameObject.layer );
					trans.gameObject.layer = IGNORE_LAYER;
				}
			}

			// ���j���[�{�^���ɔ������Ă��܂�Ȃ��l�Ɍ��݂�ControllerBehavior���L���ȏꍇ�̂ݖ����ɂ���
			if ( m_ctrlBehNowFI != null )
			{
				AVRControllerBehavior ctrlBeh = (AVRControllerBehavior)m_ctrlBehNowFI.GetValue( m_thisController );
				if ( ctrlBeh && ctrlBeh.enabled )
				{
					ctrlBeh.enabled = false;
					m_ctrlBehNow = ctrlBeh;
				}
			}

			// COM�̖鉾�R�}���h���j���[���B��
			_hideYotogiCommandMenu();
		}

		// ��\���ɂ����n���h�I�u�W�F�N�g��\������
		private void _dispHandObject()
		{
			// �����ɂ����R���|�[�l���g��߂�
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

			// ���C���[�����ɖ߂�
			foreach ( var pair in m_grabDic )
			{
				pair.Key.gameObject.layer = pair.Value;
			}
			m_grabDic.Clear();

			// �����ɂ���ControllerBehavior��߂����A�V�[�����ς���Č��݂�ControllerBehavior��
			// �ς���Ă���ꍇ������̂œ��������ׂĂ���߂�
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

			// COM�̖鉾�R�}���h���j���[���B���Ă�����߂�
			_showYotogiCommandMenu();
		}

		// �����̒͂ݏ����� trans ��͂�ł��邩���ׂ�
		private bool _isGripping( Transform trans )
		{
			if ( m_inputGrip != null &&
				 m_grippingTrans != null )
			{
				// �����͂�ł���Ȃ�A����Transform���擾���āA��r����
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

		// VRMenu�����邩�𒲂ׂ�
		private void _findVRMenu()
		{
			var vrm = transform.parent.GetComponents<MonoBehaviour>().FirstOrDefault( c => c.GetType().Name.Contains( "VRMenuController" ) );
			if ( vrm )
			{
				// VRMenu��������
				if ( vrm.GetType().BaseType != null )
				{
					// ���A�S�R���g���[���Œ͂�ł���I�u�W�F�N�g�̃��X�g ���擾
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

		// VRMenu�� trans ��͂�ł��邩���ׂ�
		private bool _isGrippingVRMenu( Transform trans )
		{
			if ( m_isVrMenu )
			{
				return m_grippingListVRM.Contains( trans );
			}
			return false;
		}
		
		// COM�̖鉾�R�}���h���j���[�֘A���N���A����
		private void _clrYotogiCommandMenu()
		{
			m_bBackHand = null;
			m_attachSideL = null;
			m_isHideYotogiCommandMen = false;
		}
		// COM�̖鉾�R�}���h���j���[��T��
		private void _findYotogiCommandMenu()
		{
			// �K��CM�ł� command_menu �͑��݂��Ă����̂ŁA���̂܂܎g��
			if ( YotogiManager.instans &&
				 YotogiManager.instans.command_menu )
			{
				Type type = YotogiManager.instans.command_menu.GetType();
				
				m_bBackHand = m_bBackHand ?? type.GetField( m_thisController.m_bHandL ? "m_bBackHandL" : "m_bBackHandR", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
				m_attachSideL = m_attachSideL ?? type.GetField( "controller_attach_side_L_", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
			}
		}

		// COM�̖鉾�R�}���h���j���[���B��
		private void _hideYotogiCommandMenu()
		{
			if ( m_bBackHand != null &&
				 m_attachSideL != null &&
				 YotogiManager.instans.command_menu.gameObject.activeInHierarchy )
			{
				// ���������̃n���h���[�h���R�}���h���j���[��
				bool bBackHand = (bool)m_bBackHand.GetValue( YotogiManager.instans.command_menu );
				if ( bBackHand )
				{
					// ���j���[��\�����Ă���Ȃ�
					bool attachSideL = (bool)m_attachSideL.GetValue( YotogiManager.instans.command_menu );
					if ( attachSideL == m_thisController.m_bHandL )
					{
						// �鉾�R�}���h���j���[������
						YotogiManager.instans.command_menu.gameObject.SetActive( false );
						m_isHideYotogiCommandMen = true;
					}
				}
			}

			if ( m_ctrlBehNowFI != null )
			{

			}
		}

		// COM�̖鉾�R�}���h���j���[���B���Ă�����߂�
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

	// ���b�Z�[�W�\���N���X
	public class MSG
	{
		//�@�f�o�b�O�p�R���\�[���o�̓��\�b�h
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
 