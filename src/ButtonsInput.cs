using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CM3D2.MaidOyako.Plugin
{
	// 複数ボタンの入力判定
	class ButtonsInput
	{
		private AVRControllerButtons.BTN m_btn1 = AVRControllerButtons.BTN.GRIP;
		private AVRControllerButtons.BTN m_btn2 = AVRControllerButtons.BTN.GRIP;
		private Func<AVRController, bool> m_isTrg = null;
		private float m_pressStart = 0f;    // ダブルクリック判定用の1回目に押したときの時間
		public float DblInterval { get; set; } // ダブルクリックと判定する間隔

		// ボタンの組み合わせを指定して生成する
		// 使用可能なボタンは AVRControllerButtons.BTN の文字列↓のみ
		// VIRTUAL_MENU, VIRTUAL_L_CLICK, VIRTUAL_R_CLICK, VIRTUAL_GRUB, MENU, TRIGGER, STICK_PAD, GRIP
		// それを「+」で連結する
		// 同じボタンの場合はダブルクリック、違うボタンの場合は最初のボタンを押しながら次のボタンを押す
		public ButtonsInput( string btnCombination, AVRControllerButtons.BTN btn1Default, AVRControllerButtons.BTN btn2Default )
		{
			// 初期値
			DblInterval = 0.5f;

			// 空白があれば除去しておく
			btnCombination = btnCombination.Replace( " ", "" );
			// +で分割
			string[] btnStr = btnCombination.Split( new char[]{'+'}, StringSplitOptions.RemoveEmptyEntries );

			if( btnStr.Length == 1 )
			{
				// 一応1ボタンも対応
				m_btn1 = _btnParse( btnStr[0], btn1Default );
				m_isTrg = _isPress1Btn;
			}
			else
			if ( btnStr.Length >= 2 )
			{
				m_btn1 = _btnParse( btnStr[0], btn1Default );
				m_btn2 = _btnParse( btnStr[1], btn2Default );
				// 同じボタンならダブルクリック判定を行う
				if ( m_btn1 == m_btn2 )	{ m_isTrg = _isPressDbl; }
				else					{ m_isTrg = _isPress2Btn; }
			}

			// 失敗時はデフォルト設定にしておく
			if ( m_isTrg == null )
			{
				m_btn1 = btn1Default;
				m_btn2 = btn2Default;
				// 同じボタンならダブルクリック判定を行う
				if ( m_btn1 == m_btn2 )	{ m_isTrg = _isPressDbl; }
				else					{ m_isTrg = _isPress2Btn; }
			}
		}

		public bool IsPress( AVRController controller )
		{
			if ( m_isTrg != null)
			{
				if ( controller != null &&
					 controller.VRControllerButtons != null )
				{
					return m_isTrg( controller );
				}
			}
			return false;
		}

		// ボタン文字列をenumに変換
		private static AVRControllerButtons.BTN _btnParse( string str, AVRControllerButtons.BTN deflt )
		{
			try	{ return (AVRControllerButtons.BTN)Enum.Parse( typeof( AVRControllerButtons.BTN ), str, true ); }
			catch {}
			return deflt;
		}

		// ボタン1が押されたか
		private bool _isPress1Btn( AVRController controller )
		{
			// ボタン1が押されたら
			if ( controller.VRControllerButtons.GetPressDown( m_btn1 ) )
			{
				return true;
			}
			return false;
		}

		// ボタン1を押しながらボタン2が押されたか
		private bool _isPress2Btn( AVRController controller )
		{
			// ボタン1を押しながら
			if ( controller.VRControllerButtons.GetPress( m_btn1 ) )
			{
				// ボタン2が押されたら
				if ( controller.VRControllerButtons.GetPressDown( m_btn2 ) )
				{
					return true;
				}
			}
			return false;
		}

		// ボタン1がダブルクリックされたか
		private bool _isPressDbl( AVRController controller )
		{
			if ( m_pressStart == 0.0f )
			{
				// ボタン1が押されたら、その時間を記憶
				if ( controller.VRControllerButtons.GetPressDown( m_btn1 ) )
				{
					m_pressStart = Time.time;
				}
			}
			else
			{
				// 時間内に再度ボタン1が押されているか調べる
				float passTime = Time.time - m_pressStart;
				if ( passTime < DblInterval )
				{
					if ( controller.VRControllerButtons.GetPressDown( m_btn1 ) )
					{
						m_pressStart = 0f;
						return true;
					}
				}
				else
				{
					m_pressStart = 0f;
				}
			}
			return false;
		}
	}
}
