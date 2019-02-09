using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace CM3D2.MaidOyako.Plugin
{
	public class Data
	{
		// 親子付けを有効にするシーン名を設定する
		public List<string> sceneNameList = new List<string>()
		{
			"SceneYotogi",					// 夜伽
			"SceneYotogiWithChubLip",		// Chu-B Lipの夜伽
		//	"ScenePhotoMode",				// 撮影モード
		//	"SceneFreeModeSelect",			// 回想モードVIP
		//	"SceneADV",						// VIP
		};
	}
	class Config
	{
		// 参照用の実体
		private static Data m_config;
		
		// コンフィグデータへのアクセス
		public static Data Instance
		{
			get
			{
				if ( m_config == null )
				{
					try
					{
						// XMLから読み込み
						StreamReader sr = new StreamReader( MaidOyako.ConfigXmlPath, new UTF8Encoding( false ) );
						XmlSerializer serializer = new XmlSerializer( typeof( Data ) );
						m_config = (Data)serializer.Deserialize( sr );
						sr.Close();
					}
					catch
					{
						m_config = new Data();
						Save();
					}
				}

				return m_config;
			}
		}

		// 設定データのセーブ
		public static void Save()
		{
			// XMLへ書き込み
			StreamWriter sw = new StreamWriter( MaidOyako.ConfigXmlPath, false, new System.Text.UTF8Encoding( false ) );
			XmlSerializer serializer = new XmlSerializer( typeof( Data ) );
			serializer.Serialize( sw, m_config );
			sw.Close();
		}
	}
}
